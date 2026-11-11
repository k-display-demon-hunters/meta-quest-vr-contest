using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ChannelSumAnimationController
    {
        private readonly ChannelSumDisplayController _display;
        private readonly LessonPlayback _playback;
        public ChannelSumAnimationController(ChannelSumDisplayController display, LessonPlayback playback)
        {
            _display = display;
            _playback = playback;
        }
        public IEnumerator Run(int channel, int[] products, int sum)
        {
            Vector3 centre = _display.SumGatherPosition;
            Vector3[] starts = new Vector3[9];
            for (int index = 0; index < 9; index++) { starts[index] = _display.CopyStart(index); }
            _display.PrepareChannelSum(channel, products);
            float duration = 0.65f / 0.75f;
            for (float time = 0; time < duration; time += _playback.DeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / duration);
                for (int index = 0; index < 9; index++) { _display.MoveSumCopy(index, Vector3.Lerp(starts[index], centre, t)); }
                yield return null;
            }
            _display.ShowGatheredSum(sum);
            yield return _playback.Wait(0.45f);
            yield return Fly(centre, _display.AnswerPosition, 0.6f / 0.75f);
            _display.FinishChannelSum(sum);
        }
        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += _playback.DeltaTime)
            {
                _display.MoveProduct(Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration)));
                yield return null;
            }
            _display.MoveProduct(end);
        }
    }
}
