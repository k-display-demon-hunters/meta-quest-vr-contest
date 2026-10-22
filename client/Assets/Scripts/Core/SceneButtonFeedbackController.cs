using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class SceneButtonFeedbackController : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Button _button;
        private Outline _outline;
        private bool _hovered;
        private bool _pressed;
        private bool _wasInteractable;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _outline = GetComponent<Outline>();
            if (_outline == null)
            {
                _outline = gameObject.AddComponent<Outline>();
            }
            _outline.effectDistance = new Vector2(3f, -3f);
            _outline.useGraphicAlpha = false;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData data)
        {
            _hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData data)
        {
            _hovered = false;
            _pressed = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            _pressed = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            _pressed = false;
            Refresh();
        }

        private void LateUpdate()
        {
            bool interactable = _button.isActiveAndEnabled && _button.IsInteractable();
            if (interactable != _wasInteractable)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_outline == null)
            {
                return;
            }
            _wasInteractable = _button.isActiveAndEnabled && _button.IsInteractable();
            if (!_wasInteractable)
            {
                _pressed = false;
            }
            // Availability can change while the ray stays over the button without another pointer-enter event.
            _outline.enabled = _wasInteractable && (_hovered || _pressed);
            _outline.effectColor = _pressed ? Color.white : SceneUIStyle.Accent;
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            Refresh();
        }
    }
}
