using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class CalculationAnimationController
    {
        private const float PairHold = 0.18f;
        private const float OperandTravel = 0.35f;
        private const float EquationHold = 0.65f;
        private const float ProductTravel = 0.35f;
        private const float RecordHold = 0.15f;
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        public CalculationAnimationController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
        }

        public IEnumerator MultiplyAndRecord(int channel, int index)
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
            _spatial.ShowProductEquation(channel, index);
            yield return new WaitForSecondsRealtime(EquationHold);
            _spatial.MoveProduct(meeting);
            _spatial.ShowProductValue(index, product);
            yield return Fly(meeting, _spatial.ResultCellPosition(index), ProductTravel);
            _spatial.CommitProduct(index, product);
            _ui.Feedback = $"곱한 값 {product}을 같은 위치에 기록했어요.";
            yield return new WaitForSecondsRealtime(RecordHold);
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
