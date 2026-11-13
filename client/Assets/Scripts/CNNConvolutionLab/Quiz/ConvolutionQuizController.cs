using System.Collections;

namespace CNNConvolution
{
    // The output-grid controller sends a location; this lesson-owned object judges it.
    internal sealed class ConvolutionQuizController
    {
        private const float CandidateHighlightDuration = 0.45f;
        private readonly OutputSelectionController _selection;
        private readonly EquationDisplayController _equations;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        private int _expectedIndex;
        private bool _answered;
        public bool Waiting { get; private set; }

        public ConvolutionQuizController(ResultGridController result, EquationDisplayController equations,
            LessonUIController ui, LessonNarrationController narration, LessonPlayback playback)
        {
            _selection = new OutputSelectionController(result);
            _equations = equations;
            _ui = ui;
            _narration = narration;
            _playback = playback;
        }

        public IEnumerator Run(int expectedIndex)
        {
            _expectedIndex = expectedIndex;
            _ui.Caption = "위치 퀴즈 · 이번 출력값이 들어갈 빈칸을 골라 주세요.";
            _narration.Play(NarrationCue.Quiz);
            _equations.Clear();
            _answered = false;
            Waiting = true;
            _selection.Selected += SubmitAnswer;
            _selection.SetEnabled(true);
            float highlightTime = 0;
            while (!_answered || _playback.IsPaused)
            {
                highlightTime += _playback.DeltaTime;
                if (highlightTime >= CandidateHighlightDuration)
                {
                    highlightTime %= CandidateHighlightDuration;
                    _selection.AdvanceHighlight();
                }
                yield return null;
            }
            _ui.Caption = $"정답 · {_expectedIndex / 3 + 1}행 {_expectedIndex % 3 + 1}열이에요.";
            _narration.Play(NarrationCue.QuizCorrect);
            yield return _playback.Wait(0.5f);
            yield return _narration.WaitUntilDone();
            Cancel();
        }

        public void SetPaused(bool paused) => _selection.SetPaused(paused);

        public void SubmitAnswer(int index)
        {
            if (!Waiting || _answered || _playback.IsPaused) { return; }
            if (index != _expectedIndex)
            {
                _ui.Caption = $"{index / 3 + 1}행 {index % 3 + 1}열은 아니에요. 필터가 오른쪽으로 한 칸 움직였어요.";
                _narration.Play(NarrationCue.QuizWrong);
                return;
            }
            _answered = true;
            Waiting = false;
            _selection.SetEnabled(false);
        }

        public void Cancel()
        {
            Waiting = false;
            _selection.Selected -= SubmitAnswer;
            _selection.SetEnabled(false);
        }
    }
}
