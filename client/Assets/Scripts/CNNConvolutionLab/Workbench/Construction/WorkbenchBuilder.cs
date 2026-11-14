using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    public sealed class WorkbenchBuilder : MonoBehaviour
    {
        [Header("Panel groups")]
        [SerializeField] private Transform _inputGroup;
        [SerializeField] private Transform _formulaGroup;
        [SerializeField] private Transform _outputGroup;

        [Header("Focus slots")]
        [SerializeField] private Transform _inputCenterSlot;
        [SerializeField] private Transform _inputSideSlot;
        [SerializeField] private Transform _gridSideSlot;

        [Header("Panel anchors")]
        [SerializeField] private Transform _inputAnchor;
        [SerializeField] private Transform _formulaAnchor;
        [SerializeField] private Transform _kernelHome;
        [SerializeField] private Transform _kernelTarget;
        [SerializeField] private Transform _resultAnchor;
        [SerializeField] private Text _placementHint;
        [SerializeField] private Material _placementPreviewMaterial;
        [SerializeField, Min(0.1f)] private float _placementGhostPeriod = 1f;

        private WorkbenchPrimitiveFactory _factory;
        private PictureDisplayController _picture;
        private WorkbenchKernelController _kernel;
        private FeatureMapDisplayController _map;

        internal (WorkbenchPrimitiveFactory Factory, FocusLayoutController Focus, PictureDisplayController Picture,
            WorkbenchKernelController Kernel, FeatureMapDisplayController Map) Build(Font font, LessonPlayback playback)
        {
            _factory = new WorkbenchPrimitiveFactory(transform, font);
            FocusLayoutController focus = new FocusLayoutController(_inputGroup, _formulaGroup, _outputGroup,
                _inputCenterSlot, _inputSideSlot, _gridSideSlot, playback);
            _picture = new PictureDisplayController(_factory, _inputAnchor);
            _kernel = new WorkbenchKernelController(_factory, transform, _inputGroup, _kernelHome, _kernelTarget,
                PictureDisplayController.Pitch * 3f, _placementPreviewMaterial, _placementGhostPeriod);
            _map = new FeatureMapDisplayController(_factory, _resultAnchor);
            return (_factory, focus, _picture, _kernel, _map);
        }

        internal Transform PlacementTarget => _kernelTarget;
        internal Text PlacementHint => _placementHint;
        internal Transform FormulaAnchor => _formulaAnchor;

        private void OnDisable() => _kernel?.ReleaseInteractions();

        private void OnDestroy()
        {
            _kernel?.Dispose();
            _picture?.Dispose();
            _map?.Dispose();
            _factory?.Dispose();
        }
    }
}
