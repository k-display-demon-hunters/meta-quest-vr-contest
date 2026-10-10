using UnityEngine;
using UnityEngine.InputSystem;

namespace CNNFeatureMaps
{
    /// <summary>Desktop interaction for the same serialized CNN parts used by the XR rig.</summary>
    [RequireComponent(typeof(CNNFeatureMapScene))]
    public sealed class CNNFeatureMapMouseDrag : MonoBehaviour
    {
        private const float DragThresholdSquared = 16f;
        private Camera _camera;
        private CNNFeatureMapScene _scene;
        private CNNFeatureMapGrabTarget _dragged;
        private CNNFeatureMapGrabTarget _pendingInput;
        private Vector2 _pressPosition;
        private Ray _pressRay;
        private Vector3 _offset;
        private float _depth;

        private void Awake()
        {
            _scene = GetComponent<CNNFeatureMapScene>();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null || !_scene.IsReady)
            {
                ReleaseTarget();
                return;
            }
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            if (_camera == null)
            {
                ReleaseTarget();
                return;
            }
            if (_dragged != null && (!_dragged.isActiveAndEnabled || !_dragged.IsHeld))
            {
                ReleaseTarget();
            }

            Vector2 pointer = mouse.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(pointer);
            CNNFeatureMapGrabTarget pointed = null;
            CNNFeatureMapButton button = null;
            if (_camera.pixelRect.Contains(pointer) && Physics.Raycast(ray, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Ignore))
            {
                CNNFeatureMapGrabTarget candidate = hit.collider.GetComponentInParent<CNNFeatureMapGrabTarget>();
                if (candidate != null && candidate.Workspace == _scene && candidate.isActiveAndEnabled)
                {
                    pointed = candidate;
                }
                CNNFeatureMapButton candidateButton = hit.collider.GetComponentInParent<CNNFeatureMapButton>();
                if (candidateButton != null && candidateButton.transform.IsChildOf(transform))
                {
                    button = candidateButton;
                }
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                ReleaseTarget();
                _scene.ResetLayout();
                return;
            }
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && pointed != null && _scene.CanGrab(pointed))
            {
                pointed.ToggleSpread();
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                ReleaseTarget();
                if (button != null)
                {
                    button.Press();
                }
                else if (pointed != null && !pointed.IsHeld && _scene.CanGrab(pointed))
                {
                    CNNFeatureMapNode node = pointed.GetComponent<CNNFeatureMapNode>();
                    if (node != null && node.Layer < 0)
                    {
                        // Click selects/paints once. A subsequent drag extracts that same pixel.
                        node.SelectPixel();
                        _pendingInput = pointed;
                        _pressPosition = pointer;
                        _pressRay = ray;
                    }
                    else
                    {
                        BeginDrag(pointed, ray, true);
                    }
                }
            }
            if (!mouse.leftButton.isPressed)
            {
                ReleaseTarget();
            }
            else if (_pendingInput != null && (pointer - _pressPosition).sqrMagnitude >= DragThresholdSquared)
            {
                CNNFeatureMapGrabTarget input = _pendingInput;
                Ray startRay = _pressRay;
                _pendingInput = null;
                // The press already selected/painted it; do not toggle its input value a second time.
                BeginDrag(input, startRay, false);
            }

            float scroll = mouse.scroll.ReadValue().y;
            bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            CNNFeatureMapGrabTarget scaleTarget = _dragged != null ? _dragged : pointed;
            if (shift && scroll != 0f && scaleTarget != null && _scene.CanGrab(scaleTarget) &&
                (_dragged == scaleTarget || !scaleTarget.IsHeld))
            {
                scaleTarget.ScaleBy(Mathf.Exp(scroll * .002f));
            }
            if (_dragged == null)
            {
                return;
            }
            bool rotate = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            if (rotate && !_dragged.IsFilter)
            {
                _dragged.RotateBy(mouse.delta.ReadValue(), _camera.transform);
                return;
            }
            if (!_dragged.IsFilter && !shift)
            {
                _depth = Mathf.Clamp(_depth + scroll * .001f, .3f, 10f);
            }
            Plane plane = CreateDragPlane(_dragged, false);
            if (plane.Raycast(ray, out float distance))
            {
                _dragged.MoveTo(ray.GetPoint(distance) + _offset);
            }
        }

        private Plane CreateDragPlane(CNNFeatureMapGrabTarget target, bool starting)
        {
            if (target.IsFilter && target.transform.parent != null)
            {
                // The serialized filter belongs to the input root. Follow its plane even after rotation.
                return new Plane(target.transform.parent.forward, target.transform.position);
            }
            Vector3 point = starting ? target.transform.position :
                _camera.transform.position + _camera.transform.forward * _depth;
            return new Plane(_camera.transform.forward, point);
        }

        private void BeginDrag(CNNFeatureMapGrabTarget target, Ray ray, bool selectInput)
        {
            ReleaseTarget();
            if (!target.BeginMouseDrag(selectInput))
            {
                return;
            }
            _dragged = target;
            _depth = Mathf.Clamp(Vector3.Dot(target.transform.position - _camera.transform.position,
                _camera.transform.forward), .3f, 10f);
            Plane plane = CreateDragPlane(target, true);
            _offset = plane.Raycast(ray, out float distance) ? target.transform.position - ray.GetPoint(distance) : Vector3.zero;
        }

        private void ReleaseTarget()
        {
            _pendingInput = null;
            if (_dragged != null)
            {
                _dragged.EndMouseDrag();
            }
            _dragged = null;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                ReleaseTarget();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ReleaseTarget();
            }
        }

        private void OnDisable()
        {
            ReleaseTarget();
        }
    }
}
