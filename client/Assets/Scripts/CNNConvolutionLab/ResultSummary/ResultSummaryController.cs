using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ResultSummaryController
    {
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        private bool _confirmed;
        private bool _ready;

        public event Action FinishRequested;

        public ResultSummaryController(LessonUIController ui, LessonNarrationController narration, LessonPlayback playback)
        {
            _ui = ui;
            _narration = narration;
            _playback = playback;
        }

        public IEnumerator WaitForConfirmation()
        {
            _confirmed = false;
            _ready = false;
            _ui.NextRequested += HandleConfirm;
            _ui.FinishRequested += HandleFinish;
            _ui.Caption = "아홉 자리의 계산이 끝났어요. ‘결과 확인’을 눌러 주세요.";
            _narration.Play(NarrationCue.AllDone);
            _ui.ShowNext("결과 확인");
            yield return new WaitUntil(() => _confirmed && !_playback.IsPaused);
            _ui.NextRequested -= HandleConfirm;
            _ui.HideNext();
        }

        // The closing scene's caption stays; its last narration already asks for the finish button.
        public void ShowFinish()
        {
            _ui.ShowFinish();
            _ready = true;
        }

        public void Cancel()
        {
            _confirmed = false;
            _ready = false;
            _ui.NextRequested -= HandleConfirm;
            _ui.FinishRequested -= HandleFinish;
            _ui.HideNext();
        }

        private void HandleConfirm()
        {
            _confirmed = true;
        }

        private void HandleFinish()
        {
            if (!_ready) { return; }
            _ready = false;
            _ui.DisableFinish();
            _ui.Caption = "체험을 마쳤어요. 다음 방으로 이동할 수 있어요.";
            _narration.Play(NarrationCue.Finished);
            FinishRequested?.Invoke();
        }
    }
}
