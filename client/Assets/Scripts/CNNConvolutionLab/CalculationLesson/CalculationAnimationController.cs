using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class CalculationAnimationController
    {
        private readonly RgbVolumeController _spatial;
        private readonly ConvolutionCalculation _calculation;
        public CalculationAnimationController(RgbVolumeController spatial, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _calculation = calculation;
        }
        public IEnumerator MultiplyAndAccumulate(int channel, int index, int accumulated)
        {
            int product = _calculation.Product(channel, index);
            _spatial.BeginProductDisplay(channel, index);
            Vector3 start = _spatial.InputCellPosition(channel, index);
            Vector3 weightStart = _spatial.KernelCellPosition(index);
            Vector3 meeting = new Vector3(0.1f, 0.42f, -0.65f);
            _spatial.HighlightRow(index / 3);
            for (float time = 0; time < 0.7f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 0.7f);
                _spatial.MoveOperands(Vector3.Lerp(start, meeting + Vector3.left * 0.13f, t),
                    Vector3.Lerp(weightStart, meeting + Vector3.right * 0.13f, t));
                yield return null;
            }
            _spatial.MoveProduct(meeting);
            _spatial.ShowProductEquation(channel, index);
            yield return new WaitForSecondsRealtime(0.4f);
            Vector3 result = _spatial.ResultCellPosition(index);
            yield return Fly(meeting, result, 0.45f);
            _spatial.CommitProduct(index, product);
            yield return Fly(result, _spatial.AnswerPosition, 0.4f);
            _spatial.CompleteProduct(accumulated);
        }
        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _spatial.MoveProduct(Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration)));
                yield return null;
            }
            _spatial.MoveProduct(end);
        }
    }
}
