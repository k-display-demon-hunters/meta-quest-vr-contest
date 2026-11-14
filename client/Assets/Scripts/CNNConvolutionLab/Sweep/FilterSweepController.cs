using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Sweep stage: the learner drags the filter over the picture and every position it passes paints one
    // cell of the feature map. Once enough has been tried by hand, the remaining cells fill in by themselves.
    internal sealed class FilterSweepController
    {
        private const int ManualCells = 60;
        private const float AutoFillCellsPerSecond = 80f;
        private readonly WorkbenchKernelController _kernel;
        private readonly PictureDisplayController _picture;
        private readonly FeatureMapDisplayController _map;
        private readonly PictureConvolution _convolution;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        private readonly bool[] _recorded = new bool[PictureConvolution.MapColumns * PictureConvolution.MapRows];
        // Current filter position: x = column, y = row.
        private Vector2Int _window;
        private int _count;
        private bool _active;
        private bool _autoRequested;

        public FilterSweepController(WorkbenchKernelController kernel, PictureDisplayController picture,
            FeatureMapDisplayController map, PictureConvolution convolution, LessonUIController ui,
            LessonNarrationController narration, LessonPlayback playback)
        {
            _kernel = kernel;
            _picture = picture;
            _map = map;
            _convolution = convolution;
            _ui = ui;
            _narration = narration;
            _playback = playback;
        }

        public void Prepare()
        {
            Array.Clear(_recorded, 0, _recorded.Length);
            _count = 0;
            _map.Clear();
        }

        // Writes the output of one filter position into the same cell of the feature map.
        public void Record(int row, int column)
        {
            _window = new Vector2Int(column, row);
            float output = _convolution.Output(row, column);
            int index = row * PictureConvolution.MapColumns + column;
            if (!_recorded[index])
            {
                _recorded[index] = true;
                _count++;
                _map.Record(row, column, output);
            }
            _map.ShowCurrent(row, column, output);
        }

        public IEnumerator Run()
        {
            _kernel.Released += HandleReleased;
            _ui.SkipRequested += HandleAutoRequested;
            _active = true;
            _autoRequested = false;
            _kernel.SetConstrainedMove(true);
            _kernel.SetGrabEnabled(true);
            _ui.Caption = "필터를 잡고 그림 위를 움직여 주세요.";
            _narration.Play(NarrationCue.SweepStart);
            _ui.ShowSkip(true, "자동으로 훑기");
            // The sweep stays open until the instructions have been heard, however fast the learner is.
            while (!_autoRequested && (_count < ManualCells || _narration.IsBusy))
            {
                if (!_playback.IsPaused && _kernel.IsSelected) { SweepTo(_picture.NearestWindow(_kernel.RequestedPosition)); }
                yield return null;
            }
            EndManualSweep();

            _ui.Caption = "남은 자리는 같은 방법으로 한 번에 채웁니다.";
            _narration.Play(NarrationCue.AutoFill);
            float budget = 0f;
            for (int index = 0; index < _recorded.Length; index++)
            {
                if (_recorded[index]) { continue; }
                while (budget < 1f)
                {
                    yield return null;
                    budget += _playback.DeltaTime * AutoFillCellsPerSecond;
                }
                budget -= 1f;
                int row = index / PictureConvolution.MapColumns;
                int column = index % PictureConvolution.MapColumns;
                _kernel.SetPose(_picture.WindowPose(row, column));
                Record(row, column);
            }
            yield return _narration.WaitUntilDone();
            _map.HideCurrent();
        }

        public void SetPaused(bool paused)
        {
            if (!_active) { return; }
            if (paused) { _kernel.CancelGrab(); }
            _kernel.SetGrabEnabled(!paused);
        }

        public void Cancel()
        {
            EndManualSweep();
            _map.HideCurrent();
        }

        // The filter snaps from cell to cell; cells a fast hand skips over are visited on the way.
        private void SweepTo(Vector2Int target)
        {
            Vector2Int from = _window;
            int steps = Mathf.Max(Mathf.Abs(target.x - from.x), Mathf.Abs(target.y - from.y));
            if (steps == 0) { return; }
            for (int step = 1; step <= steps; step++)
            {
                Vector2 point = Vector2.Lerp(from, target, (float)step / steps);
                Record(Mathf.RoundToInt(point.y), Mathf.RoundToInt(point.x));
            }
            _kernel.SetPose(_picture.WindowPose(target.y, target.x));
        }

        private void EndManualSweep()
        {
            _active = false;
            _kernel.Released -= HandleReleased;
            _ui.SkipRequested -= HandleAutoRequested;
            _kernel.CancelGrab();
            _kernel.SetGrabEnabled(false);
            _kernel.SetConstrainedMove(false);
            _ui.ShowSkip(false);
        }

        private void HandleReleased() => _kernel.RestoreParent();

        private void HandleAutoRequested()
        {
            if (_active && !_playback.IsPaused) { _autoRequested = true; }
        }
    }
}
