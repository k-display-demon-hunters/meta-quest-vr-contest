using System;
using OpeningEnding;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicInteractionManager : MonoBehaviour
    {
        [SerializeField] private CinematicInputController _input;
        [SerializeField] private CinematicMovementController _movement;
        [SerializeField] private CinematicControlsController _controls;
        private bool _showRayOnRelease;

        internal event Action OnButtonRequested;
        internal event Action OnHoldCompleted;

        private void OnEnable()
        {
            _input.OnHoldCompleted += HandleHoldCompleted;
            _input.OnReleased += HandleReleased;
            _controls.OnButtonRequested += HandleButtonRequested;
        }

        private void OnDisable()
        {
            _input.OnHoldCompleted -= HandleHoldCompleted;
            _input.OnReleased -= HandleReleased;
            _controls.OnButtonRequested -= HandleButtonRequested;
        }

        internal void Bind(XROrigin rig, bool ending)
        {
            _movement.Bind(rig, !ending);
            _movement.SetUIVisible(!ending);
            _input.Bind(true);
            _controls.Bind(rig.Camera, ending);
        }

        internal void BeginOpening()
        {
            _input.BeginPlayback();
            _movement.SetControllersVisible(false);
            _movement.SetUIVisible(false);
            _controls.SetButtonVisible(false);
        }

        internal void BeginEnding()
        {
            _input.ResetHold();
            _input.BeginPlayback();
        }

        internal void ShowEndingControls()
        {
            _controls.SetButtonVisible(true);
            _movement.SetControllersVisible(true);
            _input.ResetHold();
            _showRayOnRelease = _input.IsPressed;
            if (!_showRayOnRelease) { _movement.SetUIVisible(true); }
        }

        internal void UpdateHoldProgress() => _controls.SetHoldProgress(_input.HoldProgress);
        internal void ClearHoldProgress() => _controls.SetHoldProgress(0f);
        internal void ShowFailure(string message) => _controls.ShowFailure(message);
        internal void Pulse(float amplitude, float duration) => _input.Pulse(amplitude, duration);

        private void HandleButtonRequested() => OnButtonRequested?.Invoke();
        private void HandleHoldCompleted() => OnHoldCompleted?.Invoke();

        private void HandleReleased()
        {
            if (!_showRayOnRelease) { return; }
            _showRayOnRelease = false;
            _movement.SetUIVisible(true);
        }
    }
}
