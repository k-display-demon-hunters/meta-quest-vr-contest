using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Cinematic
{
    public sealed class CinematicInputController : MonoBehaviour
    {
        private InputAction _trigger;
        private float _held;
        private bool _latched;
        private bool _playing;
        private bool _wasPressed;
        public float HoldProgress => Mathf.Clamp01(_held / 2f);
        internal event Action OnHoldCompleted;
        internal event Action OnReleased;
        internal bool IsPressed => _trigger.IsPressed();

        internal void Bind(bool waitingForStart)
        {
            _playing = !waitingForStart;
            _latched = waitingForStart;
            _trigger = new InputAction("Cinematic skip", InputActionType.Button);
            _trigger.AddBinding("<XRController>{RightHand}/triggerPressed");
            _trigger.AddBinding("<XRController>{LeftHand}/triggerPressed");
#if UNITY_EDITOR
            _trigger.AddBinding("<Keyboard>/space");
#endif
            _trigger.Enable();
        }

        internal void BeginPlayback() => _playing = true;

        internal void ResetHold()
        {
            _held = 0;
            _latched = IsPressed;
            _wasPressed = _latched;
        }

        private void Update()
        {
            if (_trigger == null || !_playing) { return; }
            if (!_trigger.IsPressed())
            {
                _held = 0;
                _latched = false;
                if (_wasPressed)
                {
                    _wasPressed = false;
                    OnReleased?.Invoke();
                }
                return;
            }
            _wasPressed = true;
            if (_latched) { return; }
            _held += Time.unscaledDeltaTime;
            if (_held >= 2f) { _latched = true; OnHoldCompleted?.Invoke(); }
        }

        public void Pulse(float amplitude, float duration)
        {
            foreach (XRNode hand in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
                if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
                { device.SendHapticImpulse(0, amplitude, duration); }
            }
        }

        public void Release()
        {
            _trigger?.Disable();
            _trigger?.Dispose();
            _trigger = null;
            _playing = false;
        }
        private void OnDisable() { Release(); }
    }
}
