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
            _animation = new CalculationAnimationController(spatial, ui, calculation);
        }
        public IEnumerator Run(int channel = 0)
        {
            string name = channel == 0 ? "R" : "G";
            _ui.BeginChannel(channel);
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
            _spatial.BeginGuidedCalculation();
            _ui.ChannelSums = name + " 지금까지의 합: 0";
            string notebook = "";
            for (int i = 0; i < 9; i++)
            {
                // Keep only the current row in the compact notebook.
                if (i % 3 == 0) { notebook = ""; _ui.Guide = ""; }
                _ui.Title = name + " 채널 · " + (i + 1) + " / 9칸";
                _ui.Explanation = $"{i / 3 + 1}행 {i % 3 + 1}열 · 같은 위치의 입력과 가중치를 곱해요.";
                _ui.Equation = "";
                int product = _calculation.Product(channel, i);
                _ui.Feedback = _calculation.Weight(channel, i) == 0
                    ? "0을 곱하므로 결과가 0이 돼요."
                    : "1을 곱하므로 입력값이 유지돼요.";
                yield return _animation.MultiplyAndAccumulate(channel, i, accumulated);
                accumulated += product;
                string term = $"{_calculation.Input(channel, i)} × {_calculation.Weight(channel, i)} = {_calculation.Product(channel, i)}";
                notebook += (i % 3 == 0 ? "" : "\n") + term;
                _ui.Guide = notebook;
                if (i % 3 == 2)
                {
                    yield return _ui.WaitForNext(i == 8 ? name + " 합 확인" : "다음 줄 계산");
                }
            }
            _ui.CompleteChannel(channel, accumulated);
            _ui.Title = name + " 계산 완료";
            _ui.Explanation = "가중치가 1인 세 칸만 더하면 돼요.";
            _ui.Equation = $"{_calculation.Input(channel, 0)} + {_calculation.Input(channel, 4)} + {_calculation.Input(channel, 8)} = {accumulated}";
            _ui.Feedback = channel == 0 ? "G도 같은 방법으로 연습해요." : "이제 B를 풀 준비가 됐어요.";
            yield return _ui.WaitForNext(channel == 0 ? "G 시작" : "학습 정리");
        }
    }
}
