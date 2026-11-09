using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Entry point for both the detailed first window and subsequent compact windows.
    internal sealed class CalculationLessonController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;

        public CalculationLessonController(RgbVolumeController spatial, LessonUIController ui)
        {
            _spatial = spatial;
            _ui = ui;
        }

        public IEnumerator RunFirstWindow(ConvolutionCalculation calculation)
        {
            RedCalculationController firstChannel = new RedCalculationController(_spatial, _ui, calculation);
            yield return firstChannel.Run();
            yield return ShowCompactChannel(calculation, 1, true);
            yield return ShowCompactChannel(calculation, 2, true);
            _ui.Title = "세 채널의 합이 출력값 하나가 돼요";
            _ui.Explanation = "오른쪽 판은 지금부터 출력값이 채워지는 지도예요.";
            _spatial.BeginOutputMap();
            yield return RecordOutput(calculation);
        }

        public IEnumerator RunWindow(ConvolutionCalculation calculation)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                yield return ShowCompactChannel(calculation, channel, false);
            }
            yield return RecordOutput(calculation);
        }

        public IEnumerator MoveToWindow(int row, int column, bool showGuidance)
        {
            _ui.ResetChannelProgress();
            _spatial.ClearEquations();
            if (showGuidance)
            {
                _ui.Title = "필터를 한 칸 옮겨요";
                _ui.Explanation = "새 위치에서도 R · G · B를 각각 계산해요.";
            }
            yield return _spatial.MoveWindow(row, column);
        }

        private IEnumerator ShowCompactChannel(ConvolutionCalculation calculation, int channel, bool showProducts)
        {
            _ui.BeginChannel(channel);
            _ui.Title = $"{"RGB"[channel]} · 출력 {calculation.Row + 1}행 {calculation.Column + 1}열";
            _ui.Explanation = "현재 위치의 아홉 곱을 모두 더해요.";
            _ui.Guide = "R · G · B의 계산은 같은 위치에서 이어져요.";
            _ui.Feedback = "";
            _spatial.ShowSlidingChannel(channel, showProducts);
            yield return new WaitForSecondsRealtime(0.25f);
            if (showProducts) { _spatial.ShowProducts(channel, 9); }
            _spatial.ShowChannelSum(channel, false);
            _ui.CompleteChannel(channel, calculation.Sum(channel));
            yield return new WaitForSecondsRealtime(0.8f);
        }

        private IEnumerator RecordOutput(ConvolutionCalculation calculation)
        {
            _ui.Title = $"출력 {calculation.Row + 1}행 {calculation.Column + 1}열에 기록해요";
            _ui.Explanation = "R 합 + G 합 + B 합 = 출력값 하나";
            _spatial.ShowRgbSum();
            yield return new WaitForSecondsRealtime(0.7f);
            yield return _spatial.RecordWindowOutput();
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }
}
