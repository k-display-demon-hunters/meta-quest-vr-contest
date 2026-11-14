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

        [Header("Panel anchors")]
        [SerializeField] private Transform _inputAnchor;
        [SerializeField] private RawImage _inputImage;
        [SerializeField] private Transform _formulaAnchor;
        [SerializeField] private Transform _kernelTitle;
        [SerializeField] private Transform _kernelHome;
        [SerializeField] private Transform _kernelTarget;
        [SerializeField] private Transform _resultAnchor;
        [SerializeField] private RawImage _resultImage;
        [SerializeField] private Text _resultReadout;
        [SerializeField] private Text _placementHint;
        [SerializeField] private Material _placementPreviewMaterial;
        [SerializeField, Min(0.1f)] private float _placementGhostPeriod = 1f;

        private WorkbenchPrimitiveFactory _factory;
        private PictureDisplayController _picture;
        private WorkbenchKernelController _kernel;
        private FeatureMapDisplayController _map;

        internal (WorkbenchPrimitiveFactory Factory, FocusLayoutController Focus, PictureDisplayController Picture,
            WorkbenchKernelController Kernel, FeatureMapDisplayController Map) Build(Font font)
        {
            _factory = new WorkbenchPrimitiveFactory(transform, font);
            FocusLayoutController focus = new FocusLayoutController(_formulaGroup, _outputGroup);
            _picture = new PictureDisplayController(_inputAnchor, _inputImage);
            _kernel = new WorkbenchKernelController(_factory, transform, _inputGroup, _kernelHome, _kernelTarget, _kernelTitle,
                PictureDisplayController.Pitch * 3f, _placementPreviewMaterial, _placementGhostPeriod);
            _map = new FeatureMapDisplayController(_factory, _resultAnchor, _resultImage, _resultReadout);
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
