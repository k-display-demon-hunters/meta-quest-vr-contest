using System;
using System.Collections;

namespace CNNConvolution
{
    // After the feature map is complete: sums up what convolution is, what the feature map is and where
    // it goes next, then offers the finish button and reports the learner's finish request once.
    internal sealed class ResultSummaryController
    {
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private bool _ready;

        public event Action FinishRequested;

        public ResultSummaryController(LessonUIController ui, LessonNarrationController narration)
        {
            _ui = ui;
            _narration = narration;
        }

        public IEnumerator Run()
        {
            _ui.FinishRequested += HandleFinish;
            _ui.Caption = "합성곱: 필터를 옮기며, 겹친 칸끼리 곱해 더한 값을 자리마다 구합니다.";
            _narration.Play(NarrationCue.Summary);
            yield return _narration.WaitUntilDone();
            _ui.Caption = "특징 맵: 필터가 찾는 특징이 그림의 어디에 있는지 보여 주는 지도 한 장입니다.";
            _narration.Play(NarrationCue.FeatureMap);
            yield return _narration.WaitUntilDone();
            _ui.Caption = "특징 맵은 다음 단계의 입력이 됩니다. ‘체험 마치기’를 눌러 주세요.";
            _narration.Play(NarrationCue.NextStage);
            _ui.ShowFinish();
            _ready = true;
        }

        public void Cancel()
        {
            _ready = false;
            _ui.FinishRequested -= HandleFinish;
        }

        private void HandleFinish()
        {
            if (!_ready) { return; }
            _ready = false;
            _ui.DisableFinish();
            _ui.Caption = "체험을 마쳤습니다. 다음 방으로 이동할 수 있습니다.";
            _narration.Play(NarrationCue.Finished);
            FinishRequested?.Invoke();
        }
    }
}
