using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class EndingVisualController : MonoBehaviour
    {
        [SerializeField] private Transform _world, _portal, _timeline, _digit, _factory;
        [SerializeField] private Transform[] _boxes, _pixels, _eraProps;
        [SerializeField] private Transform _network, _modelFrame, _standingMarker, _holdHint;
        [SerializeField] private Material _recognitionMaterial;
        [SerializeField] private Text _year;
        [SerializeField] private Transform[] _networkLayers;
        [SerializeField] private MeshRenderer[] _layerCards;
        [SerializeField] private Text _modelHeading, _testAccuracy;
        [SerializeField] private Text[] _layerLabels;
        [SerializeField] private EndingArchiveTrail.Panel[] _historyPanels;
        [SerializeField] private EndingYearRoll _yearRoll;

        private EndingRecognitionController _archiveFrame;
        private EndingModelController _model;
        private EndingQuietController _quietController;
        private EndingArchiveTrail _trail;
        internal const float RecognitionLockTime = EndingRecognitionController.LockAt;
        internal const float FocusLockDuration = EndingRecognitionController.LockDuration;
        internal Vector3 FocusPosition => _boxes[0].position;

        internal void Bind(CinematicModelResult result, Camera camera)
        {
            _quietController = new EndingQuietController(camera, _standingMarker, _holdHint);
            Transform head = camera.transform;
            // Move the stage around the viewer once; never move the camera or chase head rotation.
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            _world.position = new Vector3(head.position.x, head.position.y - 1.6f, head.position.z);
            _world.rotation = Quaternion.LookRotation(forward.sqrMagnitude > .01f ? forward : Vector3.forward);
            _archiveFrame = new EndingRecognitionController(_boxes[0], _recognitionMaterial, _portal);
            _modelFrame.gameObject.SetActive(false);

            _yearRoll.Bind(_year);
            _model = new EndingModelController(_world, _network, _networkLayers, _layerCards,
                _modelHeading, _testAccuracy, _layerLabels);
            _model.Bind(result);
        }

        internal void PrepareTrail(int panelCount)
        {
            _trail = new EndingArchiveTrail(_portal, _historyPanels, panelCount);
        }
        internal void SetTrailPhoto(int index, Texture2D still, Rect crop, string year) =>
            _trail.SetPhoto(index, still, crop, year);
        internal void EvaluateTrail(float time, float[] starts, float outroStart, float quietStart) =>
            _trail.Evaluate(time, starts, outroStart, quietStart);
        private void OnDestroy() => _trail?.Dispose();

        internal void SetMontageVisible(bool visible)
        {
            _portal.gameObject.SetActive(visible);
        }

        internal Rect FrameArchive(EndingSequenceData.MontageEntry entry, out Rect visibleFocus) =>
            EndingPhotoFraming.Calculate(entry, _portal.localScale.x / _portal.localScale.y, out visibleFocus);

        internal void HideArchiveFocus() => _archiveFrame.Hide();

        internal void ShowYear(string previous, string year, float elapsed, float alpha) =>
            _yearRoll.Show(previous, year, elapsed, alpha);

        internal void HideYear() => _year.gameObject.SetActive(false);

        internal EndingFocusTarget ArchiveTarget(Rect focus, float zoom)
        {
            Vector2 offset = (focus.center - Vector2.one * .5f) * zoom;
            Vector3 world = _portal.TransformPoint(new Vector3(offset.x, offset.y, -.045f));
            Vector2 size = new Vector2(focus.width * _portal.lossyScale.x, focus.height * _portal.lossyScale.y) * zoom;
            return new EndingFocusTarget(_world.InverseTransformPoint(world), size);
        }

        internal EndingFocusTarget ModelTarget => _model.Target;

        internal void ShowModelFocus(float elapsed) => _archiveFrame.ShowModelFocus(ModelTarget, elapsed);
        internal void ShowReleasedFocus(EndingFocusTarget target, float progress) =>
            _archiveFrame.ShowReleasedFocus(target, progress);
        internal void ShowFlyingFocus(EndingFocusTarget source, EndingFocusTarget target, float progress) =>
            _archiveFrame.ShowFlyingFocus(source, target, progress);
        internal void ShowLockedFocus(EndingFocusTarget target, float elapsed) =>
            _archiveFrame.ShowLockedFocus(target, elapsed);

        internal void ShowModel(bool visible, float elapsed) => _model.ShowModel(visible, elapsed);
        internal void SetQuiet(bool quiet) => _quietController.SetQuiet(quiet);
        private void OnDisable() => _quietController?.Restore();

        internal void Finish()
        {
            SetQuiet(false);
            _trail?.HidePhotos();
            _model.ShowModel(false, 0f);
            foreach (Transform target in new[] { _portal, _timeline, _digit, _factory })
            { target.gameObject.SetActive(false); }
            _year.gameObject.SetActive(false);
            foreach (Transform target in _boxes) { target.gameObject.SetActive(false); }
            foreach (Transform target in _pixels) { target.gameObject.SetActive(false); }
            foreach (Transform target in _eraProps) { target.gameObject.SetActive(false); }
        }

    }
}
