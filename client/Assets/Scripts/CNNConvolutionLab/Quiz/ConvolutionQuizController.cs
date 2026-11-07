using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // The output-grid controller sends a location; this lesson-owned object judges it.
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
            _ui.Title = "이번 출력값은 어느 칸에 들어갈까요?";
            _ui.Explanation = "오른쪽 출력판에서 대응하는 칸을 선택하세요.";
            _ui.Guide = "필터가 오른쪽으로 한 칸 움직였어요.";
            _ui.Feedback = "첫 출력값은 그대로 남아 있어요.";
            _spatial.ClearEquations();
            _answered = false;
            Waiting = true;
            _spatial.OutputSelected += SubmitAnswer;
            _spatial.BeginPositionQuiz();
            yield return new WaitUntil(() => _answered);
            _ui.Title = "맞아요 · 1행 2열이에요";
            _ui.Explanation = "이제 이 위치의 R · G · B를 계산해요.";
            _ui.Feedback = "";
            yield return new WaitForSecondsRealtime(0.5f);
        }

        public void SubmitAnswer(int index)
        {
            if (!Waiting || _answered) { return; }
            if (index != _calculation.Row * 3 + _calculation.Column)
            {
                _ui.Feedback = "필터가 한 칸 움직인 방향을 따라가 보세요.";
                return;
            }
            _answered = true;
            Waiting = false;
            _spatial.EndPositionQuiz();
        }

        public void Cancel()
        {
            Waiting = false;
            _spatial.OutputSelected -= SubmitAnswer;
            _spatial.EndPositionQuiz();
        }
    }
}
