using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicSequenceManager : MonoBehaviour
    {
        [SerializeField] private CinematicTransitionController _transitionPrefab;

        private enum State
        {
            Initializing,
            WaitingForStart,
            Playing,
            Completed,
            Transitioning,
            TransitionFailed
        }

        [SerializeField] private bool _ending;
        [SerializeField] private CinematicText _content;
        [SerializeField] private CinematicModelResult _model;
        [SerializeField] private CinematicPresentationManager _presentation;
        [SerializeField] private CinematicInteractionManager _interaction;
        [SerializeField] private string _destination = "FactoryEnvironment";

        private XROrigin _rig;
        private CinematicTransitionController _transition;
        private State _state;

        private void OnEnable()
        {
            _interaction.OnHoldCompleted += HandleHold;
            _interaction.OnButtonRequested += HandleButton;
            _presentation.OnPlaybackCompleted += HandlePlaybackCompleted;
            _presentation.OnHapticRequested += _interaction.Pulse;
        }

        private void OnDisable()
        {
            _interaction.OnHoldCompleted -= HandleHold;
            _interaction.OnButtonRequested -= HandleButton;
            _presentation.OnPlaybackCompleted -= HandlePlaybackCompleted;
            _presentation.OnHapticRequested -= _interaction.Pulse;
            Log($"Disabled state={_state} elapsed={_presentation.Elapsed:F2}s");
        }

        private IEnumerator Start()
        {
            Log($"Initializing destination={_destination}");
            _rig = FindFirstObjectByType<XROrigin>();
            _interaction.Bind(_rig, _ending);
            _presentation.Bind(_content, _model, _ending, _rig.Camera, GetInstanceID());
            if (!_ending)
            {
                SetState(State.WaitingForStart);
                yield break;
            }

            // Direct scene play has no incoming transition. Factory entry waits for its fade-in.
            CinematicTransitionController arrival = _rig.GetComponentInChildren<CinematicTransitionController>();
            while (arrival != null && arrival.IsLoading) { yield return null; }
            _interaction.BeginEnding();
            SetState(State.Playing);
            _presentation.Begin();
        }

        private void Update()
        {
            if (_state == State.WaitingForStart) { _presentation.AlignOpeningToViewer(); }
            if (_state == State.Playing) { _presentation.Tick(Time.deltaTime); }
            if (_state == State.Playing || _state == State.Completed || _state == State.TransitionFailed)
            {
                _interaction.UpdateHoldProgress();
            }
        }

        private void HandleButton()
        {
            if (_state == State.WaitingForStart)
            {
                _model.Clear();
                SetState(State.Playing);
                _interaction.BeginOpening();
                _presentation.Begin();
                _transition = CinematicTransitionController.Prepare(_transitionPrefab, _rig.Camera, _rig.transform, _destination);
            }
            else if (_state == State.Completed || _state == State.TransitionFailed)
            {
                StartTransition();
            }
        }

        private void HandleHold()
        {
            if (_state == State.Playing) { CompletePlayback("Skip"); }
            else if (_state == State.Completed || _state == State.TransitionFailed) { StartTransition(); }
        }

        private void HandlePlaybackCompleted() => CompletePlayback("Natural");

        private void CompletePlayback(string reason)
        {
            Log($"PlaybackCompleted reason={reason} elapsed={_presentation.Elapsed:F2}s");
            _presentation.Finish();
            _interaction.ClearHoldProgress();
            if (_ending)
            {
                SetState(State.Completed);
                _interaction.ShowEndingControls();
            }
            else
            {
                StartTransition();
            }
        }

        private void StartTransition()
        {
            SetState(State.Transitioning);
            StartCoroutine(Leave());
        }

        private IEnumerator Leave()
        {
            yield return null;
            if (_transition == null)
            {
                _transition = CinematicTransitionController.Begin(_transitionPrefab, _rig.Camera, _rig.transform, _destination);
            }
            else
            {
                _transition.BeginTransition();
            }
            CinematicTransitionController transition = _transition;
            while (transition != null && transition.IsLoading) { yield return null; }
            if (transition != null && !transition.Succeeded)
            {
                Destroy(transition.gameObject);
                _transition = null;
                ShowFailure();
            }
        }

        private void ShowFailure()
        {
            SetState(State.TransitionFailed);
            _interaction.ShowFailure(_content.LoadFailure);
        }

        private void SetState(State state)
        {
            Log($"State {_state} -> {state}");
            _state = state;
        }

        private void Log(string message)
        {
            if (!_ending) { Debug.Log($"[Opening][run={GetInstanceID()}] {message}", this); }
        }
    }
}
