using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class WindowMovementController
    {
        private readonly InputGridController _input;
        private readonly WorkbenchKernelController _kernel;
        private readonly EquationDisplayController _equations;
        private readonly LessonUIController _ui;
        private readonly LessonPlayback _playback;
        private bool _manual;
        private bool _released;
        private bool _assisted;
        private Pose _manualStart;
        private Pose _manualTarget;
        public WindowMovementController(InputGridController input, WorkbenchKernelController kernel,
            EquationDisplayController equations, LessonUIController ui, LessonPlayback playback)
        {
            _input = input;
            _kernel = kernel;
            _equations = equations;
            _ui = ui;
            _playback = playback;
        }

        public IEnumerator RunManual(int row, int column)
        {
            _manual = true;
            _released = false;
            _assisted = false;
            _manualStart = _kernel.Pose;
            _manualTarget = _input.WindowPose(row, column);
            _input.ShowChannel(0);
            _kernel.ShowChannel(0);
            _input.ShowWindow(row, column);
            _equations.Clear();
            _ui.Title = "같은 필터를 오른쪽 한 칸 옮겨 보세요";
            _ui.Explanation = "가중치는 바꾸지 않아요. RGB 세 층의 같은 위치가 함께 이동해요.";
            _ui.Instruction = "Grip으로 손잡이를 잡고 오른쪽 표시까지 옮긴 뒤 놓으세요.";
            _ui.ShowSkip(true, "한 칸 자동 이동");
            _kernel.Released += HandleReleased;
            _ui.SkipRequested += HandleAssist;
            _kernel.SetConstrainedMove(true);
            _kernel.SetTitleVisible(true);
            _kernel.TintHandle(WorkbenchColors.CalculationAccent, 0.25f);
            _kernel.SetGrabEnabled(true);
            while (true)
            {
                if (!_playback.IsPaused)
                {
                    if (_kernel.IsSelected) { _kernel.ConstrainToSegment(_manualStart, _manualTarget); }
                    if (_assisted || (_released && _kernel.DistanceTo(_manualTarget) <= 0.07f)) { break; }
                    if (_released)
                    {
                        _released = false;
                        _kernel.RestoreParent();
                        _kernel.SetPose(_manualStart);
                        _ui.Instruction = "오른쪽 표시까지 옮겨 주세요. ‘한 칸 자동 이동’도 사용할 수 있어요.";
                    }
                }
                yield return null;
            }
            Cancel();
            yield return Run(row, column, false);
        }

        public void SetPaused(bool paused)
        {
            if (!_manual) { return; }
            if (paused && _kernel.IsSelected)
            {
                _kernel.CancelGrab();
                _kernel.SetPose(_manualStart);
            }
            _kernel.SetGrabEnabled(!paused);
        }

        private void HandleReleased() => _released = true;
        private void HandleAssist()
        {
            if (_manual && !_playback.IsPaused) { _assisted = true; }
        }

        public void Cancel()
        {
            _manual = false;
            _kernel.Released -= HandleReleased;
            _ui.SkipRequested -= HandleAssist;
            _kernel.CancelGrab();
            _kernel.SetGrabEnabled(false);
            _kernel.SetConstrainedMove(false);
            _kernel.SetTitleVisible(false);
            _kernel.TintHandle(new Color(0.47f, 0.38f, 0.24f), 0);
            _ui.ShowSkip(false);
        }

        public IEnumerator Run(int row, int column, bool showGuidance)
        {
            _ui.ResetChannelProgress();
            _equations.Clear();
            if (showGuidance)
            {
                _ui.Title = WindowMovementContent.Title(row, column);
                _ui.Explanation = WindowMovementContent.Explanation();
            }
            Pose start = _kernel.Pose;
            Pose target = _input.WindowPose(row, column);
            float duration = column == 0 && row > 0 ? 0.65f : 0.30f;
            for (float time = 0; time < duration; time += _playback.DeltaTime)
            {
                _kernel.SetPose(new Pose(Vector3.Lerp(start.position, target.position, Mathf.SmoothStep(0, 1, time / duration)), start.rotation));
                yield return null;
            }
            _kernel.SetPose(target);
            _input.ShowWindow(row, column);
        }
    }
}
