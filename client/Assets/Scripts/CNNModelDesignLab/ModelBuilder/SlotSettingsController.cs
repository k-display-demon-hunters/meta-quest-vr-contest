using System;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class SlotSettingsController : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable _interactable;
        [SerializeField] private BoxCollider _hitArea;
        [SerializeField] private Text _label;
        [SerializeField] private Transform _popupAnchor;
        [SerializeField] private Vector2 _occupiedLabelPosition;
        [SerializeField] private RectTransform _settingsButton;
        [SerializeField] private Image _buttonFace;
        [SerializeField] private Text _settingValues;

        [SerializeField, Min(1)] private int _occupiedFontSize = 32;

        private Color _idleButtonColor;
        private int _emptyFontSize;
        private int _slotIndex;
        private bool _hovered;
        private float _pressedUntil;

        internal event Action<SlotSettingsController> OnSettingsRequested;

        internal ModelBlockController Block { get; private set; }
        internal Transform PopupAnchor => _popupAnchor;

        private void Awake()
        {
            _idleButtonColor = _buttonFace.color;
            _emptyFontSize = _label.fontSize;
            _interactable.activated.AddListener(HandleActivated);
            _interactable.hoverEntered.AddListener(HandleHoverEntered);
            _interactable.hoverExited.AddListener(HandleHoverExited);
        }

        internal void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;
            SetBlock(null);
        }

        internal void SetBlock(ModelBlockController block)
        {
            Block = block;
            bool occupied = block != null;
            bool configurable = occupied && block.Layer.Type != LayerType.Flatten;
            // Flatten still uses the existing popup's delete action.
            _hitArea.enabled = occupied;
            _interactable.enabled = occupied;
            _settingsButton.gameObject.SetActive(occupied);
            _settingValues.gameObject.SetActive(configurable);
            _label.rectTransform.anchoredPosition = occupied ? _occupiedLabelPosition : Vector2.zero;
            RefreshLabel();
            if (!occupied)
            {
                _hovered = false;
                _pressedUntil = 0f;
            }
            RefreshButtonColor();
        }

        internal void RefreshLabel()
        {
            string slot = $"슬롯 {_slotIndex + 1}";
            LayerDefinition layer = Block == null ? null : Block.Layer;
            _label.fontSize = layer == null ? _emptyFontSize : _occupiedFontSize;
            _label.text = layer == null ? $"{slot}\n비어 있음" : slot;
            _settingValues.text = layer?.Type switch
            {
                LayerType.Conv2D => $"필터 {layer.Filters} · {layer.Height}×{layer.Width} · {layer.Activation}",
                LayerType.MaxPooling2D => $"{layer.Height}×{layer.Width}",
                LayerType.Dense => $"뉴런 {layer.Units} · {layer.Activation}",
                _ => string.Empty
            };
        }

        private void RefreshButtonColor()
        {
            _buttonFace.color = Time.unscaledTime < _pressedUntil ? new Color32(28, 115, 117, 255)
                : _hovered ? new Color32(32, 102, 104, 255) : _idleButtonColor;
        }

        private void HandleHoverEntered(HoverEnterEventArgs args) { _hovered = true; RefreshButtonColor(); }
        private void HandleHoverExited(HoverExitEventArgs args) { _hovered = _interactable.isHovered; RefreshButtonColor(); }

        private void Update()
        {
            if (_pressedUntil > 0f && Time.unscaledTime >= _pressedUntil)
            {
                _pressedUntil = 0f;
                RefreshButtonColor();
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressedUntil = 0f;
            RefreshButtonColor();
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            if (!isActiveAndEnabled || Block == null ||
                !(args.interactorObject is XRRayInteractor ray) ||
                ray.handedness != InteractorHandedness.Right || ray.hasSelection)
            {
                return;
            }
            if (!ray.TryGetCurrentRaycast(out _, out _, out _, out _, out bool isUIHitClosest) ||
                isUIHitClosest)
            {
                return;
            }

            _pressedUntil = Time.unscaledTime + 0.12f;
            RefreshButtonColor();
            OnSettingsRequested?.Invoke(this);
        }

        private void OnDestroy()
        {
            _interactable.activated.RemoveListener(HandleActivated);
            _interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            _interactable.hoverExited.RemoveListener(HandleHoverExited);
        }
    }
}
