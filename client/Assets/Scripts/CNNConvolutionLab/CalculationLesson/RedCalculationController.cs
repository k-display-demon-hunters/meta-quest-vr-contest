using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // First R records all nine products before the single channel-sum animation.
    internal sealed class RedCalculationController
    {
        private const float NumberPlaybackSpeed = 0.75f;
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        private readonly CalculationAnimationController _animation;
        public RedCalculationController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
            _animation = new CalculationAnimationController(spatial, ui, calculation);
        }

        public IEnumerator Run(int channel = 0)
        {
            _ui.BeginChannel(channel);
            _spatial.ShowSlidingChannel(channel, true);
            for (int index = 0; index < 9; index++)
            {
                _ui.Title = $"R · {index / 3 + 1}행 {index % 3 + 1}열 · {index + 1} / 9";
                _ui.Explanation = "현재 칸의 입력값과 같은 위치의 가중치를 곱해요.";
                _ui.Guide = "원래 숫자는 제자리에 있고 사본만 이동해요.";
                _ui.Feedback = _calculation.Weight(channel, index) == 0
                    ? "0을 곱한 결과도 오른쪽 판에 기록해요."
                    : "1을 곱하면 입력값이 그대로 남아요.";
                yield return _animation.MultiplyAndRecord(channel, index, NumberPlaybackSpeed);
            }
            _ui.Title = "R · 아홉 칸 계산 완료";
            _ui.Explanation = "아홉 곱을 오른쪽 판 가운데로 모아 더해요.";
            _ui.Guide = "아홉 칸의 곱을 모두 더하면 R의 합이에요.";
            _ui.Feedback = "";
            _spatial.ClearEquations();
            yield return _spatial.GatherFirstChannelSum(NumberPlaybackSpeed);
            _spatial.ShowChannelSum(channel, true);
            _ui.CompleteChannel(channel, _calculation.Sum(channel));
            yield return new WaitForSecondsRealtime(0.9f);
        }
    }
}
