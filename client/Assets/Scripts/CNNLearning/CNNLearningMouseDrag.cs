using UnityEngine;
using UnityEngine.InputSystem;

namespace CNNLearning
{
    public sealed class CNNLearningMouseDrag : MonoBehaviour
    {
        private Camera _camera;
        private CNNLearningScene _scene;
        private CNNLearningGrabTarget _dragged;
        private Plane _plane;
        private Vector3 _offset;
        private float _depth;

        private void Awake() => _scene = GetComponent<CNNLearningScene>();

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null)
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
                return;
            }
            Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
            CNNLearningGrabTarget pointed = null;
            CNNLearningButton button = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Ignore))
            {
                pointed = hit.collider.GetComponentInParent<CNNLearningGrabTarget>();
                button = hit.collider.GetComponentInParent<CNNLearningButton>();
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                ReleaseTarget();
                _scene.ResetLayout();
            }
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && pointed != null)
            {
                pointed.ToggleSpread();
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (button != null)
                {
                    button.Press();
                }
                else if (pointed != null)
                {
                    CNNLearningNode node = pointed.GetComponent<CNNLearningNode>();
                    bool alt = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
                    if (node != null && node.Layer < 0 && !alt)
                    {
                        node.SelectPixel();
                    }
                    else
                    {
                        BeginDrag(pointed, ray);
                    }
                }
            }
            if (_dragged != null && !mouse.leftButton.isPressed)
            {
                ReleaseTarget();
            }
            float scroll = mouse.scroll.ReadValue().y;
            bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            CNNLearningGrabTarget scaleTarget = _dragged != null ? _dragged : pointed;
            if (shift && scroll != 0f && scaleTarget != null && _scene.CanGrab(scaleTarget) && (_dragged == scaleTarget || !scaleTarget.IsHeld))
            {
                scaleTarget.ScaleBy(Mathf.Exp(scroll * .002f));
            }
            if (_dragged == null)
            {
                return;
            }
            bool rotate = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            if (rotate)
            {
                _dragged.RotateBy(mouse.delta.ReadValue(), _camera.transform);
            }
            else
            {
                if (!shift)
                {
                    _depth = Mathf.Clamp(_depth + scroll * .001f, .3f, 10f);
                }
                _plane = new Plane(_camera.transform.forward, _camera.transform.position + _camera.transform.forward * _depth);
                if (_plane.Raycast(ray, out float distance))
                {
                    _dragged.MoveTo(ray.GetPoint(distance) + _offset);
                }
            }
        }

        private void BeginDrag(CNNLearningGrabTarget target, Ray ray)
        {
            ReleaseTarget();
            if (!target.BeginMouseDrag())
            {
                return;
            }
            _dragged = target;
            _depth = Mathf.Clamp(Vector3.Dot(target.transform.position - _camera.transform.position, _camera.transform.forward), .3f, 10f);
            _plane = new Plane(_camera.transform.forward, target.transform.position);
            _offset = _plane.Raycast(ray, out float distance) ? target.transform.position - ray.GetPoint(distance) : Vector3.zero;
        }

        private void ReleaseTarget()
        {
            if (_dragged != null)
            {
                _dragged.EndMouseDrag();
                _dragged = null;
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                ReleaseTarget();
            }
        }
        private void OnDisable() => ReleaseTarget();
    }
}

