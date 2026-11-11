using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using OpeningEnding;
using XR;

namespace Cinematic
{
    public sealed class CinematicInputController : MonoBehaviour
    {
        [SerializeField] private CinematicMovementController _movementGate;
        private PersistentXRPlayerRig _playerRig;
        private bool _controllersWereVisible;
        private bool _tabletWasVisible;
        private InputAction _trigger;
        private float _held;
        private bool _latched;
        private bool _playing;
        private bool _showRayOnRelease;
        public float HoldProgress => Mathf.Clamp01(_held / 2f);
        public event Action SkipRequested;

        public void Bind(XROrigin rig, bool waitingForStart)
        {
            _playerRig = rig.GetComponent<PersistentXRPlayerRig>();
            _controllersWereVisible = _playerRig.ControllerModelsVisible;
            _tabletWasVisible = _playerRig.TabletVisible;
            _playerRig.SetTabletVisible(false);
            _playerRig.SetControllerModelsVisible(waitingForStart);
            _movementGate.Bind(rig);
            _movementGate.SetUIVisible(waitingForStart);
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

        public void BeginPlayback()
        {
            _playing = true;
            _playerRig.SetControllerModelsVisible(false);
            _movementGate.SetUIVisible(false);
        }

        public void ShowEndingControls()
        {
            _playerRig.SetControllerModelsVisible(true);
            // A held trigger can finish playback. Wait for release before it can select Restart.
            _held = 0;
            _latched = _trigger.IsPressed();
            _showRayOnRelease = _latched;
            if (!_showRayOnRelease) { _movementGate.SetUIVisible(true); }
        }

        private void Update()
        {
            if (_trigger == null || !_playing) { return; }
            if (!_trigger.IsPressed())
            {
                _held = 0;
                _latched = false;
                if (_showRayOnRelease)
                {
                    _showRayOnRelease = false;
                    _movementGate.SetUIVisible(true);
                }
                return;
            }
            if (_latched) { return; }
            _held += Time.unscaledDeltaTime;
            if (_held >= 2f) { _latched = true; SkipRequested?.Invoke(); }
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
            _showRayOnRelease = false;
            _movementGate.Release();
            if (_playerRig != null)
            {
                _playerRig.SetControllerModelsVisible(_controllersWereVisible);
                _playerRig.SetTabletVisible(_tabletWasVisible);
            }
            _playerRig = null;
        }
        private void OnDisable() { Release(); }
    }
}
