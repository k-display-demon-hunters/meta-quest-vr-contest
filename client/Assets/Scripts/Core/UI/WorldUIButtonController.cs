using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [DisallowMultipleComponent]
    public sealed class WorldUIButtonController : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Graphic _background;
        [SerializeField] private Text _label;
        [SerializeField] private Outline _outline;
        [SerializeField] private Color _normalColor = new Color(.07f, .26f, .31f);
        [SerializeField] private Color _hoverColor = new Color(.12f, .4f, .45f);
        [SerializeField] private Color _pressedColor = new Color(.22f, .7f, .72f);
        [SerializeField] private Color _disabledColor = new Color(.12f, .16f, .18f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _disabledTextColor = new Color(.55f, .62f, .65f);
        [SerializeField] private Color _outlineColor = new Color(.28f, .95f, .91f);
        [SerializeField, Min(0f)] private float _transitionSeconds = .08f;
        private bool _outlineAtRest;
        private bool _hovered;
        private bool _pressed;
        private bool _wasInteractable;

        private void OnEnable()
        {
            _hovered = false;
            _pressed = false;
            Refresh(true);
        }

        private void LateUpdate()
        {
            if (_wasInteractable != (_button.isActiveAndEnabled && _button.IsInteractable()))
            {
                Refresh(false);
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
        public void SetText(string text) => _label.text = text;

        internal void ApplyPalette(Color normal, Color hover, Color pressed, Color text, Color outline, bool outlineAtRest = false)
        {
            _normalColor = normal;
            _hoverColor = hover;
            _pressedColor = pressed;
            _textColor = text;
            _outlineColor = outline;
            _outlineAtRest = outlineAtRest;
            _outline.useGraphicAlpha = !outlineAtRest;
            if (isActiveAndEnabled) { Refresh(true); }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Refresh(false);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            Refresh(false);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            _pressed = _button.IsInteractable();
            Refresh(false);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            _pressed = false;
            Refresh(false);
        }

        private void Refresh(bool immediate)
        {
            _wasInteractable = _button.isActiveAndEnabled && _button.IsInteractable();
            if (!_wasInteractable) { _pressed = false; }
            Color background = !_wasInteractable ? _disabledColor
                : _pressed ? _pressedColor : _hovered ? _hoverColor : _normalColor;
            float duration = immediate ? 0f : _transitionSeconds;
            _background.CrossFadeColor(background, duration, true, true);
            _label.CrossFadeColor(_wasInteractable ? _textColor : _disabledTextColor, duration, true, true);
            _outline.effectColor = _outlineColor;
            _outline.enabled = _wasInteractable && (_outlineAtRest || _hovered || _pressed);
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            Refresh(true);
        }
    }
}
