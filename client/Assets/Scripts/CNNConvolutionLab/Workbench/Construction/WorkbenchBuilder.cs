using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    public sealed class WorkbenchBuilder : MonoBehaviour
    {
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

        internal (WorkbenchPrimitiveFactory Factory, InputGridController Input, WorkbenchKernelController Kernel,
            ResultGridController Products, ResultGridController Result, EquationDisplayController Equations, FlyingNumberController Number) Build(Font font)
        {
            _sideNumbers = new SideNumberController();
            _factory = new WorkbenchPrimitiveFactory(transform, font, _sideNumbers);
            _backdrop = new WorkbenchHologramBackdrop(transform, _hologramBackdropMaterial);
            InputGridController input = new InputGridController(_factory, _inputAnchor);
            _kernel = new WorkbenchKernelController(_factory, transform, _kernelHome, _kernelTarget, _placementPreviewMaterial, _placementGhostPeriod);
            ResultGridController products = new ResultGridController(_factory, _productsAnchor, "중간 계산 · 아홉 곱", hasEquationArea: true);
            ResultGridController result = new ResultGridController(_factory, _resultAnchor, "최종 출력 · 3×3", hasEquationArea: false);
            EquationDisplayController equations = new EquationDisplayController(_factory, _sumAnchor, _equationAnchor, _sumEquationAnchor);
            FlyingNumberController number = new FlyingNumberController(_factory, "Animated calculation copy", 0.9f);
            return (_factory, input, _kernel, products, result, equations, number);
        }

        internal Transform PlacementTarget => _kernelTarget;
        internal Text PlacementHint => _placementHint;

#if UNITY_EDITOR
        [ContextMenu("Apply Workbench Panel Layout")]
        private void ApplyPanelLayout()
        {
            Transform hint = _placementHint.rectTransform;
            Transform[] anchors =
            {
                _kernelHome, _inputAnchor, _kernelTarget, _productsAnchor, _resultAnchor,
                _equationAnchor, _sumAnchor, _sumEquationAnchor, hint
            };
            UnityEditor.Undo.RecordObjects(anchors, "Apply Convolution Panel Layout");

            Quaternion leftWing = Quaternion.Euler(0f, -40f, 0f);
            _kernelHome.SetLocalPositionAndRotation(new Vector3(-2.91f, -0.20f, -1.30f), leftWing);
            _inputAnchor.SetLocalPositionAndRotation(new Vector3(-1.79f, 0.04f, -0.27f), leftWing);
            _productsAnchor.SetLocalPositionAndRotation(new Vector3(-0.67f, 0.04f, 0.10f), Quaternion.identity);
            _resultAnchor.SetLocalPositionAndRotation(new Vector3(0.67f, 0.04f, 0.10f), Quaternion.identity);

            PlaceRelative(_kernelTarget, _inputAnchor, InputGridController.WindowOffset(0, 0));
            PlaceRelative(_equationAnchor, _productsAnchor, new Vector3(0f, -0.39f, -0.035f));
            PlaceRelative(_sumAnchor, _productsAnchor, new Vector3(0f, -0.55f, -0.035f));
            PlaceRelative(_sumEquationAnchor, _productsAnchor, new Vector3(0f, -0.71f, -0.035f));
            PlaceRelative(hint, _kernelHome, new Vector3(0f, -0.49f, -0.06f));

            foreach (Transform anchor in anchors)
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
            }
        }

        private static void PlaceRelative(Transform anchor, Transform panel, Vector3 offset) =>
            anchor.SetPositionAndRotation(panel.TransformPoint(offset), panel.rotation);
#endif

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
