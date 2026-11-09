using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class OutputRecordingAnimationController
    {
        private readonly RgbVolumeController _spatial;
        public OutputRecordingAnimationController(RgbVolumeController spatial) { _spatial = spatial; }
        public IEnumerator Run(int index, int value)
        {
            _spatial.PrepareOutputRecord(index, value);
            yield return Fly(_spatial.OutputSourcePosition, _spatial.ResultCellPosition(index), 0.55f);
            _spatial.FinishOutputRecord(index, value);
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
