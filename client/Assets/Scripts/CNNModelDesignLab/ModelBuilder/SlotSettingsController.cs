using System;
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

        private Color _idleLabelColor;
        private int _slotIndex;
        private bool _hovered;
        private float _pressedUntil;

        internal event Action<SlotSettingsController> OnSettingsRequested;

        internal ModelBlockController Block { get; private set; }
        internal Transform PopupAnchor => _popupAnchor;

        private void Awake()
        {
            _idleLabelColor = _label.color;
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
            _hitArea.enabled = occupied;
            _interactable.enabled = occupied;
            _label.rectTransform.anchoredPosition = occupied ? _occupiedLabelPosition : Vector2.zero;
            _label.text = occupied ? $"슬롯 {_slotIndex + 1} · 설정" : $"슬롯 {_slotIndex + 1}\n비어 있음";
            if (!occupied)
            {
                _hovered = false;
                _pressedUntil = 0f;
            }
            RefreshLabelColor();
        }

        private void RefreshLabelColor()
        {
            _label.color = Time.unscaledTime < _pressedUntil ? Color.white
                : _hovered ? UI.SceneUIStyle.Accent : _idleLabelColor;
        }

        private void HandleHoverEntered(HoverEnterEventArgs args) { _hovered = true; RefreshLabelColor(); }
        private void HandleHoverExited(HoverExitEventArgs args) { _hovered = _interactable.isHovered; RefreshLabelColor(); }

        private void Update()
        {
            if (_pressedUntil > 0f && Time.unscaledTime >= _pressedUntil)
            {
                _pressedUntil = 0f;
                RefreshLabelColor();
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressedUntil = 0f;
            RefreshLabelColor();
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
            RefreshLabelColor();
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
