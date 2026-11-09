using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ChannelSumAnimationController
    {
        private readonly RgbVolumeController _spatial;
        public ChannelSumAnimationController(RgbVolumeController spatial) { _spatial = spatial; }
        public IEnumerator Run(int channel, int[] products, int sum)
        {
            Vector3 centre = _spatial.SumGatherPosition;
            Vector3[] starts = new Vector3[9];
            for (int index = 0; index < 9; index++) { starts[index] = _spatial.ResultCellPosition(index) + Vector3.back * 0.025f; }
            _spatial.PrepareChannelSum(channel, products);
            float duration = 0.65f / 0.75f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / duration);
                for (int index = 0; index < 9; index++) { _spatial.MoveSumCopy(index, Vector3.Lerp(starts[index], centre, t)); }
                yield return null;
            }
            _spatial.ShowGatheredSum(sum);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return Fly(centre, _spatial.AnswerPosition, 0.6f / 0.75f);
            _spatial.FinishChannelSum(sum);
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
