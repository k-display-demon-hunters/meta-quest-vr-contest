using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Placement-stage entry point. The workbench owns XR components and visual objects.
    internal sealed class KernelInteractionController
    {
        private const float LocalPlacementRadius = 0.1f;
        private const float PlacementDuration = 0.18f;
        private const float ReturnDuration = 0.3f;
        private enum PlacementState { Hidden, Waiting, Hovered, Moving, Ready, Returning, Settling, Complete }
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private PlacementState _state = PlacementState.Hidden;
        private Pose _motionStart;
        private float _motionTime;
        private bool _moving;
        private bool _movingToTarget;
        private bool _aligned;
        private bool _active;

        private bool IsWithinTarget => _spatial.IsKernelWithinTarget(LocalPlacementRadius);

        public KernelInteractionController(RgbVolumeController spatial, LessonUIController ui)
        {
            _spatial = spatial;
            _ui = ui;
        }

        public IEnumerator Run()
        {
            _spatial.KernelGrabbed += HandleKernelGrabbed;
            _spatial.KernelReleased += HandleKernelReleased;
            _ui.SkipRequested += HandleAssist;
            _spatial.RestoreKernelParent();
            _spatial.SetKernelPose(_spatial.KernelHomePose);
            _active = true;
            _aligned = false;
            _moving = false;
            _spatial.SetKernelGrabEnabled(true);
            ShowState(PlacementState.Waiting);
            _ui.Title = "필터가 지나간 자리";
            _ui.Explanation = "손잡이를 잡아 입력판 맨 왼쪽 위의 점선에 놓으세요.";
            _ui.Guide = "고정 학습 예제 · 입력 5×5 · 필터 3×3";
            _ui.Feedback = "계산을 익히기 위한 고정 예제예요. 각 채널의 값은 0~3이에요.";
            _ui.ShowSkip(true);
            while (!_aligned)
            {
                UpdatePlacement(Time.unscaledDeltaTime);
                yield return null;
            }
            _active = false;
            _spatial.SetKernelGrabEnabled(false);
            _ui.ShowSkip(false);
            yield return new WaitForSecondsRealtime(0.45f);
            Cancel();
            _spatial.HideKernelTitle();
        }

        public void Cancel()
        {
            _active = false;
            _moving = false;
            _aligned = false;
            _state = PlacementState.Hidden;
            _spatial.KernelGrabbed -= HandleKernelGrabbed;
            _spatial.KernelReleased -= HandleKernelReleased;
            _ui.SkipRequested -= HandleAssist;
            _spatial.CancelKernelGrab();
            _spatial.SetKernelGrabEnabled(false);
            _spatial.HideKernelPlacement();
            _ui.ShowSkip(false);
        }

        private void HandleAssist()
        {
            if (!_active || _aligned || (_moving && _movingToTarget)) { return; }
            _spatial.CancelKernelGrab();
            BeginMotion(true);
        }

        private void HandleKernelGrabbed()
        {
            _aligned = false;
            RefreshState();
        }

        private void HandleKernelReleased()
        {
            if (!_active) { return; }
            BeginMotion(IsWithinTarget);
        }

        private void UpdatePlacement(float deltaTime)
        {
            if (_moving)
            {
                _motionTime += deltaTime;
                float duration = _movingToTarget ? PlacementDuration : ReturnDuration;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_motionTime / duration));
                Pose target = _movingToTarget ? _spatial.KernelTargetPose : _spatial.KernelHomePose;
                _spatial.SetKernelPose(new Pose(
                    Vector3.Lerp(_motionStart.position, target.position, t),
                    Quaternion.Slerp(_motionStart.rotation, target.rotation, t)));
                if (_motionTime >= duration)
                {
                    _aligned = _movingToTarget;
                    _spatial.RestoreKernelParent();
                    _spatial.SetKernelPose(target);
                    _moving = false;
                    if (!_aligned) { _spatial.SetKernelGrabEnabled(true); }
                }
            }
            RefreshState();
        }

        private void BeginMotion(bool toTarget)
        {
            _spatial.RestoreKernelParent();
            _motionStart = _spatial.KernelPose;
            _moving = true;
            _movingToTarget = toTarget;
            _motionTime = 0;
            _spatial.SetKernelGrabEnabled(false);
            RefreshState();
        }

        private void RefreshState()
        {
            if (_aligned) { ShowState(PlacementState.Complete); }
            else if (_moving) { ShowState(_movingToTarget ? PlacementState.Settling : PlacementState.Returning); }
            else if (_spatial.IsKernelSelected) { ShowState(IsWithinTarget ? PlacementState.Ready : PlacementState.Moving); }
            else { ShowState(_spatial.IsKernelHovered ? PlacementState.Hovered : PlacementState.Waiting); }
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
                case PlacementState.Returning: message = "거치대로 돌아가요. 다시 잡아 주세요."; break;
                case PlacementState.Settling: message = "필터를 맞추고 있어요."; break;
                case PlacementState.Complete: message = "배치 완료 · 같은 위치끼리 계산해요."; break;
                default: return;
            }
            bool ready = state == PlacementState.Ready || state == PlacementState.Settling || state == PlacementState.Complete;
            _spatial.ShowKernelPlacement(message, ready, state == PlacementState.Complete,
                state == PlacementState.Waiting || state == PlacementState.Hovered,
                state == PlacementState.Hovered || state == PlacementState.Ready);
        }
    }
}
