using System.Collections;

namespace CNNConvolution
{
    // The same short guided exercise is used first for R, then for G.
    internal sealed class RedCalculationController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        private readonly CalculationAnimationController _animation;
        public RedCalculationController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
            _animation = new CalculationAnimationController(spatial, calculation);
        }
        public IEnumerator Run(int channel = 0)
        {
            string name = channel == 0 ? "R" : "G";
            _ui.Title = name + " 채널 · 입력 준비";
            _ui.Explanation = name + " 층을 Grip으로 잡아 앞으로 꺼내 주세요.";
            _ui.Equation = "";
            _ui.ChannelSums = "";
            _ui.Guide = "연습용 값 0~3\n\n같은 위치끼리 곱해요.";
            _ui.Feedback = "";
            yield return _spatial.PullColour(channel);
            yield return _spatial.Focus(channel);
            _ui.Title = name + " 채널 · 필터 배치";
            _ui.Explanation = "필터 손잡이를 잡아 표시된 위치에 놓아 주세요.";
            yield return _spatial.WaitForKernel();
            _ui.Title = name + " 채널 · 곱하고 더하기";
            _ui.Explanation = "같은 위치끼리 곱하고, 오른쪽 계산판에 기록해요.";
            _ui.Guide = "";
            int accumulated = 0;
            string notebook = "";
            for (int i = 0; i < 9; i++)
            {
                // Keep only the current row in the compact notebook.
                if (i % 3 == 0) { notebook = ""; }
                _ui.Equation = "";
                accumulated += _calculation.Product(channel, i);
                yield return _animation.MultiplyAndAccumulate(channel, i, accumulated);
                string term = $"{_calculation.Input(channel, i)} × {_calculation.Weight(channel, i)} = {_calculation.Product(channel, i)}";
                notebook += (i % 3 == 0 ? "" : "\n") + term;
                _ui.Guide = notebook;
                _ui.Equation = term;
                _ui.ChannelSums = name + " 지금까지의 합: " + accumulated;
                if (i % 3 == 2)
                {
                    _ui.Feedback = "0을 곱한 칸은 합에 더해도 그대로예요.";
                    yield return _ui.WaitForNext(i == 8 ? name + " 합 확인" : "다음 줄 계산");
                }
            }
            _spatial.HighlightRow(-1);
            _ui.Title = name + " 계산 완료";
            _ui.Explanation = "가중치가 1인 세 칸만 더하면 돼요.";
            _ui.Equation = $"{_calculation.Input(channel, 0)} + {_calculation.Input(channel, 4)} + {_calculation.Input(channel, 8)} = {accumulated}";
            _ui.Feedback = channel == 0 ? "G도 같은 방법으로 연습해요." : "이제 B를 풀 준비가 됐어요.";
            yield return _ui.WaitForNext(channel == 0 ? "G 시작" : "학습 정리");
        }
    }
}
