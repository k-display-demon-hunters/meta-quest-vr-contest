using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using OpeningEnding;

namespace Cinematic
{
    public sealed class CinematicInputController : MonoBehaviour
    {
        [SerializeField] private FactoryIntroInputController _movementGate;
        private readonly Dictionary<GameObject, bool> _visualStates = new Dictionary<GameObject, bool>();
        private InputAction _trigger;
        private float _held;
        private bool _latched;
        public float HoldProgress => Mathf.Clamp01(_held / 2f);
        public event Action SkipRequested;

        public void Bind(XROrigin rig)
        {
            // Hide the grip visuals, including the wrist tablet, without disabling
            // the tracked controller interactors or the independent skip action.
            foreach (Transform target in rig.GetComponentsInChildren<Transform>(true))
            {
                if (target.name != "LeftHand Grip Visual" && target.name != "RightHand Grip Visual") { continue; }
                _visualStates.Add(target.gameObject, target.gameObject.activeSelf);
                target.gameObject.SetActive(false);
            }
            _movementGate.Bind(rig);
            _movementGate.SetUIVisible(false);
            _trigger = new InputAction("Cinematic skip", InputActionType.Button);
            _trigger.AddBinding("<XRController>{RightHand}/triggerPressed");
            _trigger.AddBinding("<XRController>{LeftHand}/triggerPressed");
#if UNITY_EDITOR
            _trigger.AddBinding("<Keyboard>/space");
#endif
            _trigger.Enable();
        }

        private void Update()
        {
            if (_trigger == null) { return; }
            if (!_trigger.IsPressed()) { _held = 0; _latched = false; return; }
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
            _movementGate.Release();
            foreach (KeyValuePair<GameObject, bool> visual in _visualStates)
            {
                if (visual.Key != null) { visual.Key.SetActive(visual.Value); }
            }
            _visualStates.Clear();
        }
        private void OnDisable() { Release(); }
    }
}
