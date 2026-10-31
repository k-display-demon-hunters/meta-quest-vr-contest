using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNFeatureMaps.Activation
{
    public sealed class ActivationCellController : MonoBehaviour
    {
        [SerializeField] private int _index;
        [SerializeField] private Renderer _surface;
        [SerializeField] private XRSimpleInteractable _interactable;
        private MaterialPropertyBlock _properties;
        private Vector3 _home;
        public event Action<int> OnSelected;

        private void Awake()
        {
            _home = transform.localPosition;
            _properties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _interactable.selectEntered.AddListener(Select);
            _interactable.activated.AddListener(Activate);
        }

        private void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(Select);
            _interactable.activated.RemoveListener(Activate);
        }

        private void Select(SelectEnterEventArgs args) => OnSelected?.Invoke(_index);
        private void Activate(ActivateEventArgs args) => OnSelected?.Invoke(_index);

        public void Show(float value, float scale, bool revealed)
        {
            float depth = revealed ? Depth(value, scale) : .016f;
            Vector3 size = transform.localScale;
            size.z = depth;
            transform.localScale = size;
            transform.localPosition = _home - Vector3.forward * (depth - .016f) * .5f;
            Color color = revealed ? Tint(value, scale) : new Color(.035f, .045f, .06f);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _surface.SetPropertyBlock(_properties);
        }

        internal static float Depth(float value, float scale) =>
            Mathf.Lerp(.016f, .11f, Mathf.Clamp01(Mathf.Abs(value) / scale));

        internal static Color Tint(float value, float scale)
        {
            Color zero = new Color(.12f, .15f, .18f);
            if (value == 0f) { return zero; }
            Color sign = value > 0f ? new Color(1f, .37f, .15f) : new Color(.2f, .55f, 1f);
            return Color.Lerp(zero, sign, .28f + .72f * Mathf.Clamp01(Mathf.Abs(value) / scale));
        }
    }
}
