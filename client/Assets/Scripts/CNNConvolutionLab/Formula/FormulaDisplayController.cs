using UnityEngine;

namespace CNNConvolution
{
    // Floating formula for one filter position: nine "value × weight" terms laid out like the 3×3
    // filter, followed by their sum.
    internal sealed class FormulaDisplayController
    {
        private const float ColumnPitch = 0.40f;
        private const float RowPitch = 0.20f;
        private readonly WorkbenchPrimitiveFactory _factory;
        private readonly WorkbenchKernelController _kernel;
        private readonly Transform _anchor;
        private readonly FlyingNumberController[] _terms = new FlyingNumberController[9];
        // The sum floats on its own so it can stay in the air while the panel behind it changes.
        private readonly FlyingNumberController _sum;
        private readonly Transform _cellHighlight;

        public FormulaDisplayController(WorkbenchPrimitiveFactory factory, WorkbenchKernelController kernel, Transform anchor)
        {
            _factory = factory;
            _kernel = kernel;
            _anchor = anchor;
            for (int index = 0; index < _terms.Length; index++)
            {
                _terms[index] = new FlyingNumberController(factory, "Formula term " + index, 0.38f);
            }
            _sum = new FlyingNumberController(factory, "Formula sum", 1.0f);
            // The frame is drawn in the filter's own units, around the cell being multiplied.
            _cellHighlight = factory.Root("Formula cell");
            factory.Frame(_cellHighlight, 0.178f, 0.158f, 0.02f, WorkbenchColors.CalculationAccent, 0.016f);
            _cellHighlight.gameObject.SetActive(false);
        }

        public Vector3 TermStart(int index) => _kernel.CellPosition(index);

        // Terms keep the filter's 3×3 arrangement so each one sits where its cell is.
        public Vector3 TermSlot(int index) => PanelPoint((index % 3 - 1) * ColumnPitch, (1 - index / 3) * RowPitch + 0.06f);

        public Vector3 SumSlot => PanelPoint(0f, -0.42f);

        // The term appears over the filter cell it comes from.
        public void ShowTerm(int index, int value, string weight)
        {
            _kernel.AttachHighlight(_cellHighlight, index);
            _terms[index].Move(TermStart(index));
            _terms[index].Show((index == 0 ? "" : "+ ") + value + "×" + weight, WorkbenchColors.Channel(0), 56);
        }

        public void MoveTerm(int index, Vector3 position) => _terms[index].Move(position);

        public void ShowSum(float sum)
        {
            _cellHighlight.gameObject.SetActive(false);
            _sum.Move(SumSlot);
            _sum.Show("= " + sum.ToString("0.0"), WorkbenchColors.CalculationAccent, 92);
        }

        public void MoveSum(Vector3 position) => _sum.Move(position);

        public void HideTerms()
        {
            foreach (FlyingNumberController term in _terms) { term.Hide(); }
        }

        public void Clear()
        {
            HideTerms();
            _sum.Hide();
            _cellHighlight.gameObject.SetActive(false);
        }

        private Vector3 PanelPoint(float x, float y) => _factory.LocalPoint(_anchor.TransformPoint(new Vector3(x, y, -0.03f)));
    }
}
