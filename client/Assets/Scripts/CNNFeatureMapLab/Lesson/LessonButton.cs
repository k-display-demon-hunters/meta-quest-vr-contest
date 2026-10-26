using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNFeatureMaps.Lesson
{
    public enum LessonAction { Previous, Next, Scan, Filter, Spread, Restore, Enlarge, Shrink }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class LessonButton : MonoBehaviour
    {
        private CNNFeatureMapLabManager _scene;
        private LessonAction _action;
        private XRSimpleInteractable _interactable;
        private Renderer _renderer;
        private MaterialPropertyBlock _properties;
        private bool _available = true;
        private float _lastPress = -1f;
        private bool _hovered;
        private bool _pressed;

        public void Configure(CNNFeatureMapLabManager scene, LessonAction action)
        {
            _scene = scene;
            _action = action;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _properties = new MaterialPropertyBlock();
            _interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        private void OnEnable()
        {
            _interactable.activated.AddListener(HandleActivate);
            _interactable.hoverEntered.AddListener(HandleHoverEntered);
            _interactable.hoverExited.AddListener(HandleHoverExited);
            RefreshSurface();
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            RefreshSurface();
        }

        private void RefreshSurface()
        {
            Color color = !_available ? UI.SceneUIStyle.Disabled : _pressed ? Color.white
                : _hovered ? UI.SceneUIStyle.Accent : UI.SceneUIStyle.Button;
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_EmissionColor", _available && (_hovered || _pressed) ? color * 0.45f : Color.black);
            _renderer.SetPropertyBlock(_properties);
        }

        private void HandleHoverEntered(HoverEnterEventArgs args) { _hovered = true; RefreshSurface(); }
        private void HandleHoverExited(HoverExitEventArgs args) { _hovered = _interactable.isHovered; RefreshSurface(); }

        private void Update()
        {
            if (_pressed && Time.unscaledTime - _lastPress >= 0.12f)
            {
                _pressed = false;
                RefreshSurface();
            }
        }

        public void Press()
        {
            if (!_available || Time.unscaledTime - _lastPress < .35f || _scene == null)
            {
                return;
            }
            _lastPress = Time.unscaledTime;
            _pressed = true;
            RefreshSurface();
            _scene.Perform(_action);
        }

        private void HandleActivate(ActivateEventArgs args) => Press();

        private void OnDisable()
        {
            _interactable.activated.RemoveListener(HandleActivate);
            _interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            _interactable.hoverExited.RemoveListener(HandleHoverExited);
            _hovered = false;
            _pressed = false;
            RefreshSurface();
        }
    }
}
