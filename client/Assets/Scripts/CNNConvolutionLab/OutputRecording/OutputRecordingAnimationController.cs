using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class OutputRecordingAnimationController
    {
        private readonly WorkbenchKernelController _kernel;
        private readonly ResultGridController _result;
        private readonly FlyingNumberController _number;

        public OutputRecordingAnimationController(WorkbenchKernelController kernel, ResultGridController result, FlyingNumberController number)
        {
            _kernel = kernel;
            _result = result;
            _number = number;
        }
        public IEnumerator Run(int index, int value)
        {
            Vector3 start = _kernel.LocalPosition + Vector3.back * 0.06f;
            _number.Move(start);
            _number.Show(value.ToString(), WorkbenchColors.CalculationAccent, 64);
            _result.Highlight(index);
            yield return Fly(start, _result.CellPosition(index), 0.55f);
            _result.Record(index, value);
            _number.Hide();
            _result.HideHighlight();
        }
        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _number.Move(Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration)));
                yield return null;
            }
            _number.Move(end);
        }
    }
}
