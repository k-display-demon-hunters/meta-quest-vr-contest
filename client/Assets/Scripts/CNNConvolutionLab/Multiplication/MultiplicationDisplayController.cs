using UnityEngine;

namespace CNNConvolution
{
    internal sealed class MultiplicationDisplayController
    {
        private readonly WorkbenchPrimitiveFactory _factory;
        private readonly InputGridController _input;
        private readonly WorkbenchKernelController _kernel;
        private readonly ResultGridController _result;
        private readonly EquationDisplayController _equations;
        private readonly FlyingNumberController _number;
        private readonly FlyingNumberController _weight;
        private readonly Transform _inputHighlight;
        private readonly Transform _weightHighlight;
        private readonly LineRenderer _path;

        public MultiplicationDisplayController(WorkbenchPrimitiveFactory factory, InputGridController input,
            WorkbenchKernelController kernel, ResultGridController result, EquationDisplayController equations, FlyingNumberController number)
        {
            _factory = factory;
            _input = input;
            _kernel = kernel;
            _result = result;
            _equations = equations;
            _number = number;
            _weight = new FlyingNumberController(factory, "Animated weight copy", 0.3f);
            _inputHighlight = factory.Highlight("Active input cell", 0.178f, 0.158f);
            _weightHighlight = factory.Highlight("Active weight cell", 0.049f, 0.049f);
            _path = factory.CreatePath("Active multiplication correspondence", WorkbenchColors.CalculationAccent, 0.006f);
        }

        public Vector3 InputCellPosition(int channel, int index) => _input.CellPosition(channel, index);
        public Vector3 KernelCellPosition(int index) => _kernel.CellPosition(index);
        public Vector3 ResultCellPosition(int index) => _result.CellPosition(index);
        public Vector3 EquationPosition => _equations.EquationPosition;

        public void BeginProductDisplay(int channel, int inputIndex, int index)
        {
            Clear();
            _equations.ClearProduct();
            _input.AttachHighlight(_inputHighlight, channel, inputIndex);
            _kernel.AttachHighlight(_weightHighlight, index);
            ShowPath(_factory.WorldPoint(InputCellPosition(channel, inputIndex)), _factory.WorldPoint(KernelCellPosition(index)));
        }

        public void ShowOperandCopies(int channel, int inputIndex, int index, int input, int weight)
        {
            MoveOperands(InputCellPosition(channel, inputIndex), KernelCellPosition(index));
            _number.Show(input.ToString(), WorkbenchColors.Channel(channel), 52);
            _weight.Show("× " + weight, WorkbenchColors.CalculationAccent, 52);
            _path.enabled = false;
        }

        public void MoveOperands(Vector3 input, Vector3 weight)
        {
            _number.Move(input);
            _weight.Move(weight);
        }

        public void MoveProduct(Vector3 position) => _number.Move(position);

        public void ShowProductEquation(int channel, int input, int weight, int product)
        {
            _weight.Hide();
            _number.Hide();
            _equations.ShowProduct(channel, input, weight, product);
        }

        public void ShowProductValue(int channel, int index, int product)
        {
            _number.Show(product.ToString(), WorkbenchColors.Channel(channel), 52);
            _result.Highlight(index);
            ShowPath(_number.WorldPosition, _factory.WorldPoint(ResultCellPosition(index)));
        }

        public void CommitProduct(int index, int product)
        {
            _result.Record(index, product);
            _number.Hide();
            _path.enabled = false;
            _inputHighlight.gameObject.SetActive(false);
            _weightHighlight.gameObject.SetActive(false);
        }

        public void Clear()
        {
            _path.enabled = false;
            _number.Hide();
            _weight.Hide();
            _inputHighlight.gameObject.SetActive(false);
            _weightHighlight.gameObject.SetActive(false);
            _result.HideHighlight();
        }

        private void ShowPath(Vector3 from, Vector3 to)
        {
            _path.SetPosition(0, from);
            _path.SetPosition(1, to);
            _path.enabled = true;
        }
    }
}
