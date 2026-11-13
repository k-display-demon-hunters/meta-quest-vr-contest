using System;
using System.Collections;

namespace CNNConvolution
{
    // A review checkpoint owns only its buttons, never the lesson's calculation or position.
    internal sealed class LessonStepController
    {
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        private bool _waiting;
        private bool _advance;
        private bool _replay;

        public LessonStepController(LessonUIController ui, LessonNarrationController narration, LessonPlayback playback)
        {
            _ui = ui;
            _narration = narration;
            _playback = playback;
        }

        public IEnumerator Run(Func<IEnumerator> presentation, string nextLabel)
        {
            _ui.NextRequested += Advance;
            _ui.ReplayRequested += Replay;
            do
            {
                _advance = false;
                _replay = false;
                _waiting = false;
                _ui.HideNext();
                _ui.ShowReplay(false);
                yield return presentation();
                _ui.Caption = "‘다음’으로 계속하거나 ‘다시 보기’를 눌러 주세요.";
                _narration.Play(NarrationCue.Checkpoint);
                _ui.ShowNext(nextLabel);
                _ui.ShowReplay(true);
                // Subscribe before playback, but accept input only at this checkpoint.
                _waiting = true;
                while ((!_advance && !_replay) || _playback.IsPaused) { yield return null; }
            }
            while (_replay);
            Cancel();
        }

        private void Advance()
        {
            if (_waiting && !_playback.IsPaused) { _advance = true; _waiting = false; }
        }

        private void Replay()
        {
            if (_waiting && !_playback.IsPaused) { _replay = true; _waiting = false; }
        }

        public void Cancel()
        {
            _waiting = false;
            _ui.NextRequested -= Advance;
            _ui.ReplayRequested -= Replay;
            _ui.HideNext();
            _ui.ShowReplay(false);
        }
    }
}
