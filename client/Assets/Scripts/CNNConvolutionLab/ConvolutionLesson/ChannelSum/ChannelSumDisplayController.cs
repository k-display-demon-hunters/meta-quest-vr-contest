using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ChannelSumDisplayController
    {
        private readonly FlyingNumberController[] _copies = new FlyingNumberController[9];
        private readonly FlyingNumberController _number;
        private readonly ResultGridController _result;
        private readonly EquationDisplayController _equations;

        public ChannelSumDisplayController(WorkbenchPrimitiveFactory factory, FlyingNumberController number,
            ResultGridController result, EquationDisplayController equations)
        {
            _number = number;
            _result = result;
            _equations = equations;
            for (int index = 0; index < _copies.Length; index++)
            {
                _copies[index] = new FlyingNumberController(factory, "Product sum copy " + index, 0.17f);
            }
        }

        public Vector3 SumGatherPosition => CopyStart(4);
        public Vector3 AnswerPosition => _equations.AnswerPosition;
        public Vector3 CopyStart(int index) => _result.CellPosition(index) + Vector3.back * 0.025f;
        public void MoveSumCopy(int index, Vector3 position) => _copies[index].Move(position);
        public void MoveProduct(Vector3 position) => _number.Move(position);

        public void PrepareChannelSum(int channel, int[] products)
        {
            for (int index = 0; index < _copies.Length; index++)
            {
                _copies[index].Move(CopyStart(index));
                _copies[index].Show(products[index].ToString(), WorkbenchColors.Channel(channel), 52);
                _result.SetValueColor(index, new Color(0.43f, 0.48f, 0.52f));
            }
        }

        public void ShowGatheredSum(int sum)
        {
            foreach (FlyingNumberController copy in _copies) { copy.Hide(); }
            _number.Move(SumGatherPosition);
            _number.Show(sum.ToString(), WorkbenchColors.CalculationAccent, 68);
            _result.SetValueVisible(4, false);
        }

        public void FinishChannelSum(int sum)
        {
            Clear();
            _equations.ShowSumValue(sum);
        }

        public void Clear()
        {
            foreach (FlyingNumberController copy in _copies) { copy.Hide(); }
            _number.Hide();
            _result.SetValueVisible(4, true);
            for (int index = 0; index < _copies.Length; index++) { _result.SetValueColor(index, Color.white); }
        }
    }
}
