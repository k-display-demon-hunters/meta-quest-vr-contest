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

        public event Action<SlotSettingsController, IXRActivateInteractor> OnSettingsRequested;

        public ModelBlockController Block { get; private set; }
        public Transform PopupAnchor => _popupAnchor;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _interactable.activated.AddListener(HandleActivated);
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
            _label.text = $"SLOT {_slotIndex + 1}\n" + (occupied ? "SETTINGS" : "EMPTY");
            _body.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColor, occupied ? _readyColor : _emptyColor);
            _body.SetPropertyBlock(_properties);
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            if (isActiveAndEnabled && Block != null)
            {
                OnSettingsRequested?.Invoke(this, args.interactorObject);
            }
        }

        private void OnDestroy()
        {
            _interactable.activated.RemoveListener(HandleActivated);
        }
    }
}
