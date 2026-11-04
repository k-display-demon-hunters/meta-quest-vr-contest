using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class CalculationAnimationController
    {
        private const float PairHold = 0.25f;
        private const float OperandTravel = 0.45f;
        private const float EquationHold = 0.7f;
        private const float ProductTravel = 0.4f;
        private const float RecordHold = 0.22f;
        private const float SumTravel = 0.35f;
        private const float SumHold = 0.65f;
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        public CalculationAnimationController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
        }
        public IEnumerator MultiplyAndAccumulate(int channel, int index, int previousSum)
        {
            int product = _calculation.Product(channel, index);
            _spatial.BeginProductDisplay(channel, index);
            yield return new WaitForSecondsRealtime(PairHold);
            Vector3 start = _spatial.InputCellPosition(channel, index);
            Vector3 weightStart = _spatial.KernelCellPosition(index);
            Vector3 meeting = _spatial.EquationPosition;
            _spatial.ShowOperandCopies(channel, index);
            Vector3 inputEnd = meeting + Vector3.left * 0.14f;
            Vector3 weightEnd = meeting + Vector3.right * 0.14f;
            for (float time = 0; time < OperandTravel; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / OperandTravel);
                _spatial.MoveOperands(Vector3.Lerp(start, inputEnd, t), Vector3.Lerp(weightStart, weightEnd, t));
                yield return null;
            }
            _spatial.MoveOperands(inputEnd, weightEnd);
            _spatial.MoveProduct(meeting);
            _spatial.ShowProductEquation(channel, index);
            _ui.Equation = $"{_calculation.Input(channel, index)} × {_calculation.Weight(channel, index)} = {product}";
            yield return new WaitForSecondsRealtime(EquationHold);
            Vector3 result = _spatial.ResultCellPosition(index);
            _spatial.ShowProductValue(index, product);
            yield return Fly(meeting, result, ProductTravel);
            _spatial.CommitProduct(index, product);
            _ui.Feedback = $"곱한 값 {product}을 결과 칸에 기록해요.";
            yield return new WaitForSecondsRealtime(RecordHold);
            _spatial.BeginAccumulation(index, product);
            yield return Fly(result, _spatial.AnswerPosition, SumTravel);
            _spatial.ShowAccumulation(previousSum, product);
            string channelName = channel == 0 ? "R" : "G";
            _ui.ChannelSums = channelName + " 지금까지의 합: " + (previousSum + product);
            _ui.Feedback = product == 0
                ? $"0도 계산판에 기록했어요.\n합은 {previousSum} 그대로예요."
                : $"{previousSum} + {product} = {previousSum + product}\n곱한 값을 누적 합에 더했어요.";
            yield return new WaitForSecondsRealtime(SumHold);
            _spatial.CompleteProduct(previousSum + product);
        }
        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / duration);
                _spatial.MoveProduct(Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(Mathf.PI * t) * 0.04f));
                yield return null;
            }
            _spatial.MoveProduct(end);
        }
    }
}
