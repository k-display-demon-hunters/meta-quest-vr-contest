using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ResultSummaryController
    {
        private readonly LessonUIController _ui;
        private readonly LessonPlayback _playback;
        private bool _confirmed;
        private bool _ready;

        public event Action FinishRequested;
        public event Action Confirmed;

        public ResultSummaryController(LessonUIController ui, LessonPlayback playback)
        {
            _ui = ui;
            _playback = playback;
        }

        public IEnumerator Run(int[] outputs, int[] firstSums)
        {
            _confirmed = false;
            _ready = false;
            _ui.NextRequested += HandleConfirm;
            _ui.FinishRequested += HandleFinish;
            _ui.Title = "아홉 위치의 계산이 끝났어요";
            _ui.Explanation = "입력판 위를 이동하며 출력값 9개를 만들었어요.";
            _ui.ShowNext("결과 확인");
            yield return new WaitUntil(() => _confirmed && !_playback.IsPaused);
            _ui.NextRequested -= HandleConfirm;
            _ui.HideNext();
            Confirmed?.Invoke();
            _ui.ShowOutputSummary(outputs, firstSums);
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
            FinishRequested?.Invoke();
        }
    }
}
