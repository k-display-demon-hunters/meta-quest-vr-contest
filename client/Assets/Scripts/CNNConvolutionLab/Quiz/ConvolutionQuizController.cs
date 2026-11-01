using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ConvolutionQuizController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        private readonly int[] _options = new int[3];
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
            _ui.Title = "B 층을 앞으로 꺼내세요";
            _ui.Explanation = "Grip으로 잡아 옮기고 놓으세요.";
            yield return _spatial.PullColour(2);
            yield return _spatial.Focus(2);
            _ui.Title = "B 가중치를 맞춰 보세요";
            _ui.Explanation = "가중치를 Grip으로 잡아\n빛나는 테두리에 놓으세요.";
            yield return _spatial.WaitForKernel();
            _ui.Title = "B의 계산 결과는 얼마일까요?";
            _ui.Explanation = "같은 위치끼리 곱하고 모두 더하세요.";
            _ui.Equation = "B의 곱 9개를 더하면?";
            _ui.Guide = "가중치가 1인\n세 칸에 주목하세요.\n\n나머지는 × 0이에요.";
            _ui.Feedback = "숫자를 Grip으로 잡아 답 칸에 놓으세요.\n숫자 버튼으로 골라도 돼요.";
            int answer = _calculation.Sum(2);
            int correctIndex = Random.Range(0, 3);
            int offset = 1;
            for (int i = 0; i < 3; i++) { _options[i] = i == correctIndex ? answer : answer + offset++; }
            _answered = false;
            Waiting = true;
            _ui.SetChoices(_options);
            _ui.ShowAnswers(true);
            _spatial.ShowAnswers(_options, Choose);
            yield return new WaitUntil(() => _answered);
            Waiting = false;
            _ui.ShowAnswers(false);
            _spatial.HideAnswers();
            _spatial.ShowProducts(2, 9);
            _ui.Equation = $"{_calculation.Input(2, 0)} + {_calculation.Input(2, 4)} + {_calculation.Input(2, 8)} = {answer}";
            _ui.Guide = "잘했어요!\n\nR · G에서 배운 방법을\nB에도 적용했어요.";
            yield return _ui.WaitForNext("결과 확인");
        }
        public void Choose(int option)
        {
            if (!Waiting || _answered) { return; }
            if (_options[option] != _calculation.Sum(2))
            {
                _ui.Feedback = "한 번 더 해봐요.\n가중치가 1인 세 칸의 숫자를 더하세요.";
                return;
            }
            _answered = true;
            Waiting = false;
            _ui.Feedback = "맞아요! 곱한 값을 모두 더했어요.";
            _ui.SetAnswersEnabled(false);
            _spatial.ShowAnswer(_calculation.Sum(2));
        }
        public void Cancel() { Waiting = false; _spatial.HideAnswers(); }
    }
}
