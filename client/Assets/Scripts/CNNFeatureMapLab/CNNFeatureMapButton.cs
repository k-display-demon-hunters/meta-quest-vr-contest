using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNFeatureMaps
{
    public enum CNNFeatureMapAction { Previous, Next, Scan, Filter, Spread, Restore, Enlarge, Shrink }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class CNNFeatureMapButton : MonoBehaviour
    {
        private CNNFeatureMapLabManager _scene;
        private CNNFeatureMapAction _action;
        private XRSimpleInteractable _interactable;
        private Renderer _renderer;
        private MaterialPropertyBlock _properties;
        private bool _available = true;
        private float _lastPress = -1f;

        public void Configure(CNNFeatureMapLabManager scene, CNNFeatureMapAction action)
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
            if (!_available || Time.unscaledTime - _lastPress < .35f || _scene == null)
            {
                return;
            }
            _lastPress = Time.unscaledTime;
            _scene.Perform(_action);
        }

        private void HandleActivate(ActivateEventArgs args) => Press();

        private void OnDisable()
        {
            _interactable.activated.RemoveListener(HandleActivate);
        }
    }
}
