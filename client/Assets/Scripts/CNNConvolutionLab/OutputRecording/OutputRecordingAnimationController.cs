using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class OutputRecordingAnimationController
    {
        private readonly WorkbenchKernelController _kernel;
        private readonly ResultGridController _result;
        private readonly FlyingNumberController _number;
        private readonly LessonPlayback _playback;
        private readonly EquationDisplayController _equations;
        private readonly FlyingNumberController[] _channelSums = new FlyingNumberController[3];

        public OutputRecordingAnimationController(WorkbenchPrimitiveFactory factory, WorkbenchKernelController kernel, ResultGridController result,
            EquationDisplayController equations, FlyingNumberController number, LessonPlayback playback)
        {
            _kernel = kernel;
            _result = result;
            _number = number;
            _playback = playback;
            _equations = equations;
            for (int channel = 0; channel < 3; channel++)
            {
                _channelSums[channel] = new FlyingNumberController(factory, "RGB channel sum " + channel, 0.45f);
            }
        }
        public IEnumerator Run(int index, int[] sums)
        {
            _equations.SetAnswerVisible(false);
            Vector3 start = _equations.AnswerPosition + Vector3.back * 0.08f;
            Vector3[] sources = new Vector3[3];
            int value = 0;
            for (int channel = 0; channel < 3; channel++)
            {
                sources[channel] = _kernel.ChannelPosition(channel) + Vector3.back * 0.14f;
                _channelSums[channel].Move(sources[channel]);
                _channelSums[channel].Show($"{"RGB"[channel]} {sums[channel]}", WorkbenchColors.Channel(channel), 48);
                value += sums[channel];
            }
            yield return _playback.Wait(0.6f);
            const float gatherDuration = 0.7f;
            for (float time = 0; time < gatherDuration; time += _playback.DeltaTime)
            {
                for (int channel = 0; channel < 3; channel++)
                {
                    Vector3 destination = start + Vector3.right * ((channel - 1) * 0.27f);
                    _channelSums[channel].Move(Vector3.Lerp(sources[channel], destination, Mathf.SmoothStep(0, 1, time / gatherDuration)));
                }
                yield return null;
            }
            for (int channel = 0; channel < 3; channel++)
            {
                _channelSums[channel].Move(start + Vector3.right * ((channel - 1) * 0.27f));
            }
            yield return _playback.Wait(0.35f);
            foreach (FlyingNumberController sum in _channelSums) { sum.Hide(); }
            _number.Move(start);
            _number.Show(value.ToString(), WorkbenchColors.CalculationAccent, 64);
            _result.Highlight(index);
            yield return Fly(start, _result.CellPosition(index), 0.55f);
            _result.Record(index, value);
            _number.Hide();
            _equations.SetAnswerVisible(true);
            _result.HideHighlight();
        }
        public void Cancel()
        {
            foreach (FlyingNumberController sum in _channelSums) { sum.Hide(); }
            _number.Hide();
            _equations.SetAnswerVisible(true);
            _result.HideHighlight();
        }
        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += _playback.DeltaTime)
            {
                _number.Move(Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration)));
                yield return null;
            }
            _number.Move(end);
        }
    }
}
