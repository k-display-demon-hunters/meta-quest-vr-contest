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

        public void OnPointerEnter(PointerEventData data) { _hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData data) { _hovered = false; _pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData data) { _pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData data) { _pressed = false; Refresh(); }

        private void LateUpdate()
        {
            if (!_button.IsInteractable() && (_hovered || _pressed))
            {
                _hovered = false;
                _pressed = false;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_outline == null)
            {
                return;
            }
            _outline.enabled = _button.IsInteractable() && (_hovered || _pressed);
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
