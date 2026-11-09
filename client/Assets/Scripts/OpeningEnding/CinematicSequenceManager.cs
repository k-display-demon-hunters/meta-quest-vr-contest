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
        private bool _ready, _complete, _loading;
        public float Elapsed { get; private set; }
        public bool IsComplete => _complete;
        public bool IsEnding => _ending;
        public const float Duration = 20f;

        private void OnEnable()
        { _input.SkipRequested += Skip; _presentation.HapticRequested += _input.Pulse; }
        private void Start()
        {
            _rig = FindFirstObjectByType<XROrigin>();
            _input.Bind(_rig);
            if (!_ending) { _model.Clear(); }
            _presentation.Bind(_content, _model, _ending, _rig.Camera.transform);
            _ready = true;
            _presentation.Evaluate(0, 0);
        }
        private void Update()
        {
            if (!_ready || _loading) { return; }
            if (_complete) { _presentation.SetCompletedHold(_input.HoldProgress); return; }
            Elapsed = Mathf.Min(Duration, Elapsed + Time.deltaTime);
            _presentation.Evaluate(Elapsed, _input.HoldProgress);
            if (Elapsed >= Duration) { Complete(); }
        }
        private void Skip()
        {
            if (!_ready || _loading) { return; }
            if (_complete) { StartCoroutine(Leave()); }
            else { Elapsed = Duration; Complete(); }
        }
        private void Complete()
        {
            _complete = true;
            _presentation.Complete(_ending);
            if (!_ending) { StartCoroutine(Leave()); }
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
            // Keep the rig visuals hidden until this scene actually unloads.
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
            _input.Release();
        }
    }
}
