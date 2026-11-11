using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UI;

namespace Cinematic
{
    public sealed class CinematicSequenceManager : MonoBehaviour
    {
        [SerializeField] private bool _ending;
        [SerializeField] private CinematicText _content;
        [SerializeField] private CinematicModelResult _model;
        [SerializeField] private CinematicPresentationManager _presentation;
        [SerializeField] private CinematicInputController _input;
        [SerializeField] private string _destination = "FactoryEnvironment";
        private XROrigin _rig;
        private bool _ready, _started, _complete, _loading;
        public float Elapsed { get; private set; }
        public bool IsComplete => _complete;
        public bool IsEnding => _ending;
        public const float Duration = 20f;
        private float PlaybackDuration => _ending ? Duration : OpeningTiming.Duration;

        private void OnEnable()
        {
            _input.SkipRequested += Skip;
            _presentation.HapticRequested += _input.Pulse;
            _presentation.StartRequested += BeginOpening;
            _presentation.RestartRequested += Restart;
        }
        private void Start()
        {
            _rig = FindFirstObjectByType<XROrigin>();
            _input.Bind(_rig, !_ending);
            _presentation.Bind(_content, _model, _ending, _rig.Camera);
            _ready = true;
            _started = _ending;
            if (_ending) { _presentation.Evaluate(0, 0); }
        }

        private void BeginOpening()
        {
            if (!_ready || _started) { return; }
            _model.Clear();
            _started = true;
            _input.BeginPlayback();
            _presentation.BeginOpening();
            _presentation.Evaluate(0, 0);
        }
        private void Update()
        {
            if (!_ready || !_started || _loading) { return; }
            if (_complete) { _presentation.SetCompletedHold(_input.HoldProgress); return; }
            Elapsed = Mathf.Min(PlaybackDuration, Elapsed + Time.deltaTime);
            _presentation.Evaluate(Elapsed, _input.HoldProgress);
            if (Elapsed >= PlaybackDuration) { Complete(); }
        }
        private void Skip()
        {
            if (!_ready || !_started || _loading) { return; }
            if (_complete) { Restart(); }
            else { Elapsed = PlaybackDuration; Complete(); }
        }
        private void Restart()
        {
            if (!_complete || _loading) { return; }
            StartCoroutine(Leave());
        }
        private void Complete()
        {
            _complete = true;
            _presentation.Complete();
            if (_ending) { _input.ShowEndingControls(); }
            else { StartCoroutine(Leave()); }
        }
        private IEnumerator Leave()
        {
            _loading = true;
            yield return null;
            if (!SceneTransitionController.CanLoad(_destination))
            { _loading = false; _presentation.ShowFailure(); yield break; }
            GameObject overlay = new GameObject("Archive transition", typeof(RectTransform));
            overlay.transform.SetParent(_rig.transform, false);
            SceneTransitionController transition = overlay.AddComponent<SceneTransitionController>();
            transition.Initialize(_rig.Camera);
            transition.Load(_destination);
            while (transition != null && transition.IsLoading) { yield return null; }
            if (transition != null)
            {
                Destroy(transition.gameObject);
                _loading = false; _presentation.ShowFailure();
            }
        }
        private void OnDisable()
        {
            _input.SkipRequested -= Skip; _presentation.HapticRequested -= _input.Pulse;
            _presentation.StartRequested -= BeginOpening;
            _presentation.RestartRequested -= Restart;
            _input.Release();
        }
    }
}
