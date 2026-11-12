using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Placement-stage entry point. The workbench owns XR components and visual objects.
    internal sealed class KernelInteractionController
    {
        private const float LocalPlacementRadius = 0.15f;
        private const float PlacementDuration = 0.18f;
        private const float ReturnDuration = 0.3f;
        private enum PlacementState { Hidden, Waiting, Hovered, Moving, Ready, Returning, Settling, Complete }
        private readonly WorkbenchKernelController _kernel;
        private readonly KernelPlacementDisplayController _display;
        private readonly LessonUIController _ui;
        private readonly LessonPlayback _playback;
        private PlacementState _state = PlacementState.Hidden;
        private Pose _motionStart;
        private float _motionTime;
        private bool _moving;
        private bool _movingToTarget;
        private bool _aligned;
        private bool _active;

        private bool IsWithinTarget => _kernel.IsWithinTarget(LocalPlacementRadius);
        private bool IsWaitingForGrab => _state == PlacementState.Waiting || _state == PlacementState.Hovered;

        public KernelInteractionController(WorkbenchPrimitiveFactory factory, WorkbenchKernelController kernel, Transform target,
            UnityEngine.UI.Text hint, LessonUIController ui, LessonPlayback playback)
        {
            _kernel = kernel;
            _display = new KernelPlacementDisplayController(factory, kernel, target, hint);
            _ui = ui;
            _playback = playback;
        }

        public IEnumerator Run()
        {
            _kernel.Grabbed += HandleKernelGrabbed;
            _kernel.Released += HandleKernelReleased;
            _ui.SkipRequested += HandleAssist;
            _kernel.RestoreParent();
            _kernel.SetPose(_kernel.HomePose);
            _active = true;
            _aligned = false;
            _moving = false;
            _kernel.SetGrabEnabled(true);
            ShowState(PlacementState.Waiting);
            _ui.Title = "필터가 지나간 자리";
            _ui.Explanation = "손잡이를 잡아 입력판 맨 왼쪽 위의 점선에 놓으세요.";
            _ui.ShowSkip(true);
            while (!_aligned)
            {
                if (!_playback.IsPaused)
                {
                    float deltaTime = _playback.DeltaTime;
                    UpdatePlacement(deltaTime);
                    _display.UpdateGhost(deltaTime);
                }
                yield return null;
            }
            _active = false;
            _kernel.SetGrabEnabled(false);
            _ui.ShowSkip(false);
            yield return _playback.Wait(0.45f);
            Cancel();
            _kernel.SetTitleVisible(false);
        }

        public void SetPaused(bool paused)
        {
            if (!_active) { return; }
            if (paused && _kernel.IsSelected)
            {
                _kernel.CancelGrab();
                BeginMotion(false);
            }
            _kernel.SetGrabEnabled(!paused && !_moving && !_aligned);
        }

        public void Cancel()
        {
            _active = false;
            _moving = false;
            _aligned = false;
            _state = PlacementState.Hidden;
            _kernel.Grabbed -= HandleKernelGrabbed;
            _kernel.Released -= HandleKernelReleased;
            _ui.SkipRequested -= HandleAssist;
            _kernel.CancelGrab();
            _kernel.SetGrabEnabled(false);
            _display.HidePlacement();
            _ui.ShowSkip(false);
        }

        private void HandleAssist()
        {
            if (!_active || _playback.IsPaused || _aligned || (_moving && _movingToTarget)) { return; }
            _kernel.CancelGrab();
            BeginMotion(true);
        }

        private void HandleKernelGrabbed()
        {
            _aligned = false;
            RefreshState();
        }

        private void HandleKernelReleased()
        {
            if (!_active || _playback.IsPaused) { return; }
            BeginMotion(IsWithinTarget);
        }

        private void UpdatePlacement(float deltaTime)
        {
            if (_moving)
            {
                _motionTime += deltaTime;
                float duration = _movingToTarget ? PlacementDuration : ReturnDuration;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_motionTime / duration));
                Pose target = _movingToTarget ? _kernel.TargetPose : _kernel.HomePose;
                _kernel.SetPose(new Pose(
                    Vector3.Lerp(_motionStart.position, target.position, t),
                    Quaternion.Slerp(_motionStart.rotation, target.rotation, t)));
                if (_motionTime >= duration)
                {
                    _aligned = _movingToTarget;
                    _kernel.RestoreParent();
                    _kernel.SetPose(target);
                    _moving = false;
                    if (!_aligned) { _kernel.SetGrabEnabled(true); }
                }
            }
            RefreshState();
        }

        private void BeginMotion(bool toTarget)
        {
            _kernel.RestoreParent();
            _motionStart = _kernel.Pose;
            _moving = true;
            _movingToTarget = toTarget;
            _motionTime = 0;
            _kernel.SetGrabEnabled(false);
            RefreshState();
        }

        private void RefreshState()
        {
            if (_aligned) { ShowState(PlacementState.Complete); }
            else if (_moving) { ShowState(_movingToTarget ? PlacementState.Settling : PlacementState.Returning); }
            else if (_kernel.IsSelected) { ShowState(IsWithinTarget ? PlacementState.Ready : PlacementState.Moving); }
            else { ShowState(_kernel.IsHovered ? PlacementState.Hovered : PlacementState.Waiting); }
        }

        private void ShowState(PlacementState state)
        {
            if (_state == state) { return; }
            _state = state;
            string message;
            switch (state)
            {
                case PlacementState.Waiting: message = "필터 손잡이를 Grip으로 잡아 주세요."; break;
                case PlacementState.Hovered: message = "Grip을 누른 채 필터를 옮겨 주세요."; break;
                case PlacementState.Moving: message = "점선 테두리 안으로 옮겨 주세요."; break;
                case PlacementState.Ready: message = "여기에 놓으면 배치돼요."; break;
                case PlacementState.Returning: message = "시작 위치로 돌아가요. 다시 잡아 주세요."; break;
                case PlacementState.Settling: message = "필터를 맞추고 있어요."; break;
                case PlacementState.Complete: message = "배치 완료 · 같은 위치끼리 계산해요."; break;
                default: return;
            }
            bool ready = state == PlacementState.Ready || state == PlacementState.Settling || state == PlacementState.Complete;
            _display.ShowPlacement(message, ready, state == PlacementState.Complete,
                IsWaitingForGrab,
                state == PlacementState.Hovered || state == PlacementState.Ready);
        }
    }
}
