using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    public sealed class WorkbenchBuilder : MonoBehaviour
    {
        [Header("Panel groups")]
        [SerializeField] private Transform _inputGroup;
        [SerializeField] private Transform _productsGroup;
        [SerializeField] private Transform _outputGroup;
        [SerializeField] private Transform _equationGroup;
        [SerializeField] private Transform _meaningGroup;

        [Header("Focus slots")]
        [SerializeField] private Transform _inputCenterSlot;
        [SerializeField] private Transform _inputSideSlot;
        [SerializeField] private Transform _gridCenterSlot;
        [SerializeField] private Transform _gridSideSlot;
        [SerializeField] private Transform _equationCenterSlot;
        [SerializeField] private Transform _equationSideSlot;
        [SerializeField] private Transform _meaningSlot;

        [Header("Panel anchors")]
        [SerializeField] private Transform _inputAnchor;
        [SerializeField] private Transform _productsAnchor;
        [SerializeField] private Transform _kernelHome;
        [SerializeField] private Transform _kernelTarget;
        [SerializeField] private Transform _resultAnchor;
        [SerializeField] private Transform _sumAnchor;
        [SerializeField] private Transform _equationAnchor;
        [SerializeField] private Transform _sumEquationAnchor;
        [SerializeField] private Text _placementHint;
        [SerializeField] private Material _placementPreviewMaterial;
        [SerializeField, Min(0.1f)] private float _placementGhostPeriod = 1f;

        [SerializeField] private Material _hologramBackdropMaterial;

        private WorkbenchHologramBackdrop _backdrop;
        private SideNumberController _sideNumbers;
        private WorkbenchPrimitiveFactory _factory;
        private WorkbenchKernelController _kernel;

        internal (WorkbenchPrimitiveFactory Factory, FocusLayoutController Focus, InputGridController Input,
            WorkbenchKernelController Kernel, ResultGridController Products, ResultGridController Result,
            EquationDisplayController Equations, FlyingNumberController Number) Build(Font font, LessonPlayback playback)
        {
            _sideNumbers = new SideNumberController();
            _factory = new WorkbenchPrimitiveFactory(transform, font, _sideNumbers);
            _backdrop = new WorkbenchHologramBackdrop(transform, _hologramBackdropMaterial);
            FocusLayoutController focus = new FocusLayoutController(_inputGroup, _productsGroup, _outputGroup, _equationGroup,
                _meaningGroup, _inputCenterSlot, _inputSideSlot, _gridCenterSlot, _gridSideSlot, _equationCenterSlot,
                _equationSideSlot, _meaningSlot, playback);
            InputGridController input = new InputGridController(_factory, _inputAnchor);
            _kernel = new WorkbenchKernelController(_factory, transform, _inputGroup, _kernelHome, _kernelTarget,
                _placementPreviewMaterial, _placementGhostPeriod);
            ResultGridController products = new ResultGridController(_factory, _productsAnchor, "중간 계산 · 아홉 곱", hasEquationArea: true);
            ResultGridController result = new ResultGridController(_factory, _resultAnchor, "최종 출력 · 3×3", hasEquationArea: false);
            EquationDisplayController equations = new EquationDisplayController(_factory, _sumAnchor, _equationAnchor, _sumEquationAnchor);
            FlyingNumberController number = new FlyingNumberController(_factory, "Animated calculation copy", 0.9f);
            return (_factory, focus, input, _kernel, products, result, equations, number);
        }

        internal Transform PlacementTarget => _kernelTarget;
        internal Text PlacementHint => _placementHint;
        internal Transform MeaningAnchor => _meaningGroup;

        private void LateUpdate()
        {
            _kernel?.RefreshLayerSpacing();
            _sideNumbers?.Refresh();
        }

        private void OnDisable() => _kernel?.ReleaseInteractions();

        private void OnDestroy()
        {
            _backdrop?.Dispose();
            _kernel?.Dispose();
            _factory?.Dispose();
        }
    }
}
