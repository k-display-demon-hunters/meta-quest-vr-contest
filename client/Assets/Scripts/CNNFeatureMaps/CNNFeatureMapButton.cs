using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNFeatureMaps
{
    public enum CNNFeatureMapAction { Previous, Next, Scan, Filter, Spread, Restore }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class CNNFeatureMapButton : MonoBehaviour
    {
        private CNNFeatureMapGuide _guide;
        private CNNFeatureMapAction _action;
        private XRSimpleInteractable _interactable;
        private Renderer _renderer;
        private MaterialPropertyBlock _properties;
        private bool _available = true;
        private float _lastPress = -1f;

        public void Configure(CNNFeatureMapGuide guide, CNNFeatureMapAction action)
        {
            _guide = guide;
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
            _interactable.selectEntered.AddListener(HandleSelect);
            _interactable.activated.AddListener(HandleActivate);
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            _properties.SetColor("_BaseColor", available ? new Color(.08f, .38f, .43f) : new Color(.065f, .08f, .09f));
            _properties.SetColor("_EmissionColor", available ? new Color(.015f, .08f, .09f) : Color.black);
            _renderer.SetPropertyBlock(_properties);
        }

        public void Press()
        {
            // Grip selection and trigger activation can arrive together from the same hand.
            if (!_available || Time.unscaledTime - _lastPress < .35f || _guide == null)
            {
                return;
            }
            _lastPress = Time.unscaledTime;
            _guide.Perform(_action);
        }

        private void HandleSelect(SelectEnterEventArgs args) => Press();
        private void HandleActivate(ActivateEventArgs args) => Press();

        private void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(HandleSelect);
            _interactable.activated.RemoveListener(HandleActivate);
        }
    }
}
