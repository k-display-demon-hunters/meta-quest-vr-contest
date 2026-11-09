using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace FeatureMapCorridor
{
    // One dispatcher for the room. UI remains owned by the rig's XRUIInputModule.
    internal sealed class FeatureMapPointerController : IDisposable
    {
        private sealed class Target
        {
            internal Action<Vector3> Click;
            internal Action<Vector3> DoubleClick;
            internal Action<Vector3?> Hover;
        }

        private readonly Dictionary<Collider, Target> _targets = new Dictionary<Collider, Target>();
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        private XRRayInteractor _ray;
        private float _previousDistance;
        private bool _bound;
        private Collider _hover;
        private Collider _last;
        private float _lastTime = float.NegativeInfinity;
        private PointerEventData _mouseEvent;
        private EventSystem _eventSystem;
        private bool _mouseMode;
        private XRInteractorLineVisual _line;
        private float _previousLineLength;
        private Gradient _previousValid;
        private Gradient _previousInvalid;
        private Gradient _normalGradient;
        private Gradient _hoverGradient;
        internal string HoverName
        {
            get;
            private set;
        }
        = "—";
        internal string LastClick
        {
            get;
            private set;
        }
        = "—";
        internal FeatureMapPointerController()
        {
        }

        internal void Register(Collider collider, Action<Vector3> click, Action<Vector3> doubleClick = null, Action<Vector3?> hover = null)
        {
            _targets.Add(collider, new Target
            {
                Click = click, DoubleClick = doubleClick, Hover = hover
            }
            );
        }

        internal void Tick(Camera camera)
        {
            if (!_bound)
            {
                var rig = camera.GetComponentInParent<XR.PersistentXRPlayerRig>();
                if (rig == null)
                {
                    return;
                }
                // The persistent rig may be between scenes.
                rig.GetComponent<XR.XRPlayerInputController>().EnsureRayReady();
                foreach (var ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
                {
                    if (ray.handedness == InteractorHandedness.Right)
                    {
                        _ray = ray;
                    }
                }
                _previousDistance = _ray.maxRaycastDistance;
                _ray.maxRaycastDistance = 30;
                _line = _ray.GetComponent<XRInteractorLineVisual>();
                _previousLineLength = _line.lineLength;
                _previousValid = _line.validColorGradient;
                _previousInvalid = _line.invalidColorGradient;
                _line.lineLength = 30;
                _hoverGradient = Solid(new Color32(98, 228, 217, 255));
                _normalGradient = Solid(new Color(1, 1, 1, .45f));
                _line.validColorGradient = _hoverGradient;
                _line.invalidColorGradient = _normalGradient;
                _bound = true;
            }
            bool mouse = false;
#if UNITY_EDITOR
            if (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0 || Mouse.current.leftButton.wasPressedThisFrame))
            {
                _mouseMode = true;
            }
            if (_ray.activateInput.ReadWasPerformedThisFrame())
            {
                _mouseMode = false;
            }
            mouse = !UnityEngine.XR.XRSettings.isDeviceActive && Mouse.current != null && _mouseMode;
#endif
            Ray pointer;
            bool pressed;
            if (mouse)
            {
                if (EventSystem.current == null)
                {
                    return;
                }
                if (_eventSystem != EventSystem.current)
                {
                    _eventSystem = EventSystem.current;
                    _mouseEvent = new PointerEventData(EventSystem.current);
                }
                _mouseEvent.Reset();
                _mouseEvent.position = Mouse.current.position.ReadValue();
                _uiHits.Clear();
                EventSystem.current.RaycastAll(_mouseEvent, _uiHits);
                if (_uiHits.Count > 0)
                {
                    SetHover(null, Vector3.zero);
                    return;
                }
                pointer = camera.ScreenPointToRay(_mouseEvent.position);
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
            }
            else
            {
                if (!_ray.isActiveAndEnabled)
                {
                    SetHover(null, Vector3.zero);
                    return;
                }
                if (_ray.TryGetCurrentUIRaycastResult(out var uiHit) && uiHit.gameObject != null)
                {
                    SetHover(null, Vector3.zero);
                    HoverName = uiHit.gameObject.name;
                    if (_ray.uiPressInput.ReadWasPerformedThisFrame())
                    {
                        _ray.SendHapticImpulse(.3f, .05f);
                    }
                    return;
                }
                Transform origin = _ray.rayOriginTransform != null ? _ray.rayOriginTransform : _ray.transform;
                pointer = new Ray(origin.position, origin.forward);
                pressed = _ray.activateInput.ReadWasPerformedThisFrame();
            }
            Collider target = null;
            Vector3 point = Vector3.zero;
            if (Physics.Raycast(pointer, out var hit, 30, 1 << LayerMask.NameToLayer("CorridorInteractable"), QueryTriggerInteraction.Collide) && _targets.ContainsKey(hit.collider))
            {
                target = hit.collider;
                point = hit.point;
            }
            SetHover(target, point);
            if (!pressed || target == null)
            {
                return;
            }
            var actions = _targets[target];
            bool twice = target == _last && Time.unscaledTime - _lastTime <= .4f && actions.DoubleClick != null;
            _last = twice ? null : target;
            _lastTime = Time.unscaledTime;
            LastClick = target.name;
            Log((twice ? "Double click: " : "Click: ") + LastClick);
            if (!mouse)
            {
                _ray.SendHapticImpulse(.3f, .05f);
            }
            if (twice)
            {
                actions.DoubleClick(point);
            }
            else
            {
                actions.Click(point);
            }
        }

        private void SetHover(Collider target, Vector3 point)
        {
            if (_hover != target && _hover != null)
            {
                _targets[_hover].Hover?.Invoke(null);
            }
            _hover = target;
            if (_bound)
            {
                _line.invalidColorGradient = target == null ? _normalGradient : _hoverGradient;
            }
            HoverName = target == null ? "—" : target.name;
            if (target != null)
            {
                _targets[target].Hover?.Invoke(point);
            }
        }

        internal void Suspend()
        {
            SetHover(null, Vector3.zero);
            _last = null;
            if (_bound && _ray != null)
            {
                _ray.maxRaycastDistance = _previousDistance;
                _line.lineLength = _previousLineLength;
                _line.validColorGradient = _previousValid;
                _line.invalidColorGradient = _previousInvalid;
            }
            _bound = false;
        }

        private static Gradient Solid(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(color, 0), new GradientColorKey(color, 1)
            }, new[]
            {
                new GradientAlphaKey(color.a, 0), new GradientAlphaKey(color.a, 1)
            }
            );
            return gradient;
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        internal static void Log(string message) => Debug.Log("[Corridor] " + message);
        public void Dispose()
        {
            Suspend();
        }
    }
}
