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
        private readonly LessonPlayback _playback;
        private int _expectedIndex;
        private bool _answered;
        public bool Waiting { get; private set; }

        public ConvolutionQuizController(ResultGridController result, EquationDisplayController equations,
            LessonUIController ui, LessonPlayback playback)
        {
            _selection = new OutputSelectionController(result);
            _equations = equations;
            _ui = ui;
            _playback = playback;
        }

        public IEnumerator Run(int expectedIndex)
        {
            _expectedIndex = expectedIndex;
            _ui.Title = "<size=48><color=#FFD582>위치 퀴즈</color></size>  ·  출력 칸 고르기";
            _ui.Explanation = "<b>이번 출력값은 어느 칸에 들어갈까요?</b>\n오른쪽 3×3 출력판에서 한 칸을 골라 주세요.";
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
            _ui.Title = "<color=#93EDB6>정답</color>  ·  1행 2열이에요";
            _ui.Explanation = "이제 이 위치의 R · G · B를 계산해요.";
            yield return _playback.Wait(0.5f);
            Cancel();
        }

        public void SetPaused(bool paused) => _selection.SetPaused(paused);

        public void SubmitAnswer(int index)
        {
            if (!Waiting || _answered || _playback.IsPaused) { return; }
            if (index != _expectedIndex)
            {
                _ui.Title = "다시 골라 볼까요?";
                _ui.Explanation = $"선택한 곳은 {index / 3 + 1}행 {index % 3 + 1}열이에요.\n필터가 오른쪽 한 칸 이동했으니 출력도 같은 행에서 오른쪽 한 칸이에요.";
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
