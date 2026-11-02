using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ConvolutionQuizController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        private bool _answered;
        public bool Waiting { get; private set; }
        public ConvolutionQuizController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
        }
        public IEnumerator Run()
        {
            _ui.BeginChannel(2);
            _ui.Title = "B 채널 · 입력 준비";
            _ui.Explanation = "B 층을 Grip으로 잡아 앞으로 꺼내 주세요.";
            _ui.Equation = "";
            _ui.ChannelSums = "";
            _ui.Guide = "R · G에서 연습한 방법으로\n이번에는 직접 계산해요.";
            _ui.Feedback = "";
            yield return _spatial.PullColour(2);
            yield return _spatial.Focus(2);
            _ui.Title = "B 채널 · 필터 배치";
            _ui.Explanation = "필터 손잡이를 잡아 표시된 위치에 놓아 주세요.";
            yield return _spatial.WaitForKernel();
            _ui.Title = "B의 계산 결과는 얼마일까요?";
            _ui.Explanation = "같은 위치끼리 곱하고 모두 더하세요.";
            _ui.Equation = "B의 곱 9개를 더하면?";
            _ui.Guide = "가중치가 1인\n세 칸에 주목하세요.\n\n나머지는 × 0이에요.";
            _ui.Feedback = "숫자를 누른 뒤 답 확인을 눌러 주세요.";
            int answer = _calculation.Sum(2);
            _answered = false;
            Waiting = true;
            _spatial.HideAnswer();
            _ui.ShowKeypad(true);
            yield return new WaitUntil(() => _answered);
            Waiting = false;
            _spatial.ShowProducts(2, 9);
            _ui.Equation = $"{_calculation.Input(2, 0)} + {_calculation.Input(2, 4)} + {_calculation.Input(2, 8)} = {answer}";
            _ui.Guide = "잘했어요!\n\nR · G에서 배운 방법을\nB에도 적용했어요.";
            yield return _ui.WaitForNext("결과 확인");
        }
        public void SubmitAnswer(int answer)
        {
            if (!Waiting || _answered) { return; }
            if (answer != _calculation.Sum(2))
            {
                _ui.Feedback = "아직 맞지 않아요.\n가중치가 1인 세 칸을 더하고\n입력값을 수정해 보세요.";
                return;
            }
            _answered = true;
            Waiting = false;
            _ui.CompleteChannel(2, _calculation.Sum(2));
            _ui.Title = "B 계산 완료";
            _ui.Explanation = "가중치가 1인 세 칸을 더한 값이 B의 합이에요.";
            _ui.ChannelSums = "B 합: " + _calculation.Sum(2);
            _ui.Feedback = "맞아요! 곱한 값을 모두 더했어요.";
            _ui.ShowKeypad(false);
            _spatial.ShowAnswer(_calculation.Sum(2));
        }
        public void Cancel() { Waiting = false; _ui.ShowKeypad(false); }
    }
}
