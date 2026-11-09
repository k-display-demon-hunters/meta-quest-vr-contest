using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    public sealed class WorkbenchBuilder : MonoBehaviour
    {
        [SerializeField] private GameObject _workbenchVisuals;
        [SerializeField] private Transform _inputAnchor;
        [SerializeField] private Transform _kernelHome;
        [SerializeField] private Transform _kernelTarget;
        [SerializeField] private Transform _resultAnchor;
        [SerializeField] private Transform _sumAnchor;
        [SerializeField] private Transform _equationAnchor;
        [SerializeField] private Transform _sumEquationAnchor;
        [SerializeField] private Text _placementHint;

        private SideNumberController _sideNumbers;
        private WorkbenchPrimitiveFactory _factory;
        private WorkbenchKernelController _kernel;

        internal (WorkbenchPrimitiveFactory Factory, InputGridController Input, WorkbenchKernelController Kernel,
            ResultGridController Result, EquationDisplayController Equations, FlyingNumberController Number) Build(Font font)
        {
            _sideNumbers = new SideNumberController();
            _factory = new WorkbenchPrimitiveFactory(transform, font, _sideNumbers);
            InputGridController input = new InputGridController(_factory, _inputAnchor);
            _kernel = new WorkbenchKernelController(_factory, transform, _kernelHome, _kernelTarget);
            ResultGridController result = new ResultGridController(_factory, _resultAnchor);
            EquationDisplayController equations = new EquationDisplayController(_factory, _sumAnchor, _equationAnchor, _sumEquationAnchor);
            FlyingNumberController number = new FlyingNumberController(_factory, "Animated calculation copy", 0.9f);
            return (_factory, input, _kernel, result, equations, number);
        }

        internal Transform PlacementTarget => _kernelTarget;
        internal Text PlacementHint => _placementHint;

        internal void Show() => _workbenchVisuals.SetActive(true);

        private void LateUpdate() => _sideNumbers?.Refresh();

        private void OnDisable() => _kernel?.ReleaseInteractions();

        private void OnDestroy()
        {
            _kernel?.Dispose();
            _factory?.Dispose();
        }
    }
}
