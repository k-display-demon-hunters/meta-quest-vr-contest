using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class MultiplicationAnimationController
    {
        private const float PairHold = 0.18f;
        private const float OperandTravel = 0.35f;
        private const float EquationHold = 0.65f;
        private const float ProductTravel = 0.35f;
        private readonly RgbVolumeController _spatial;
        public MultiplicationAnimationController(RgbVolumeController spatial)
        {
            _spatial = spatial;
        }

        public IEnumerator MultiplyAndRecord(int channel, int index, int inputIndex, int input, int weight, int product, float playbackSpeed)
        {
            _spatial.BeginProductDisplay(channel, inputIndex, index);
            yield return new WaitForSecondsRealtime(PairHold);
            Vector3 start = _spatial.InputCellPosition(channel, inputIndex);
            Vector3 weightStart = _spatial.KernelCellPosition(index);
            Vector3 meeting = _spatial.EquationPosition;
            _spatial.ShowOperandCopies(channel, inputIndex, index, input, weight);
            Vector3 inputEnd = meeting + Vector3.left * 0.14f;
            Vector3 weightEnd = meeting + Vector3.right * 0.14f;
            float operandDuration = OperandTravel / playbackSpeed;
            for (float time = 0; time < operandDuration; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / operandDuration);
                _spatial.MoveOperands(Vector3.Lerp(start, inputEnd, t), Vector3.Lerp(weightStart, weightEnd, t));
                yield return null;
            }
            _spatial.MoveOperands(inputEnd, weightEnd);
            _spatial.ShowProductEquation(channel, input, weight, product);
            yield return new WaitForSecondsRealtime(EquationHold);
            _spatial.MoveProduct(meeting);
            _spatial.ShowProductValue(index, product);
            yield return Fly(meeting, _spatial.ResultCellPosition(index), ProductTravel / playbackSpeed);
            _spatial.CommitProduct(index, product);
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
