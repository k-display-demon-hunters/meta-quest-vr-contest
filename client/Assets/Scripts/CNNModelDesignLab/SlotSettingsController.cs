using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene
{
    public sealed class SlotSettingsController : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private XRSimpleInteractable _interactable;
        [SerializeField] private Renderer _body;
        [SerializeField] private Text _label;
        [SerializeField] private Transform _popupAnchor;
        [SerializeField] private Color _emptyColor = new Color(0.16f, 0.2f, 0.24f);
        [SerializeField] private Color _readyColor = new Color(0.12f, 0.42f, 0.55f);

        private MaterialPropertyBlock _properties;
        private int _slotIndex;
        private bool _hovered;
        private float _pressedUntil;

        public event Action<SlotSettingsController, IXRActivateInteractor> OnSettingsRequested;

        public ModelBlockController Block { get; private set; }
        public Transform PopupAnchor => _popupAnchor;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _interactable.activated.AddListener(HandleActivated);
            _interactable.hoverEntered.AddListener(HandleHoverEntered);
            _interactable.hoverExited.AddListener(HandleHoverExited);
        }

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;
            SetBlock(null);
        }

        public void SetBlock(ModelBlockController block)
        {
            Block = block;
            bool occupied = block != null;
            _interactable.enabled = occupied;
            _label.text = $"슬롯 {_slotIndex + 1}\n" + (occupied ? "설정" : "비어 있음");
            if (!occupied) _hovered = false;
            RefreshSurface();
        }

        private void RefreshSurface()
        {
            _body.GetPropertyBlock(_properties);
            Color color = Block == null ? _emptyColor : Time.unscaledTime < _pressedUntil ? Color.white
                : _hovered ? UI.SceneUIStyle.Accent : UI.SceneUIStyle.Button;
            _properties.SetColor(BaseColor, color);
            _properties.SetColor("_EmissionColor", Block != null && _hovered ? color * 0.4f : Color.black);
            _body.SetPropertyBlock(_properties);
        }

        private void HandleHoverEntered(HoverEnterEventArgs args) { _hovered = true; RefreshSurface(); }
        private void HandleHoverExited(HoverExitEventArgs args) { _hovered = _interactable.isHovered; RefreshSurface(); }

        private void Update()
        {
            if (_pressedUntil > 0f && Time.unscaledTime >= _pressedUntil)
            {
                _pressedUntil = 0f;
                RefreshSurface();
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressedUntil = 0f;
            if (_properties != null) RefreshSurface();
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            if (isActiveAndEnabled && Block != null)
            {
                _pressedUntil = Time.unscaledTime + 0.12f;
                RefreshSurface();
                OnSettingsRequested?.Invoke(this, args.interactorObject);
            }
        }

        private void OnDestroy()
        {
            _interactable.activated.RemoveListener(HandleActivated);
            _interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            _interactable.hoverExited.RemoveListener(HandleHoverExited);
        }
    }
}
