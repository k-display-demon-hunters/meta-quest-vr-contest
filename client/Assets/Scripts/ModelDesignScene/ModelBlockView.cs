using ModelDesign;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ModelDesignScene
{
    public sealed class ModelBlockView : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private XRGrabInteractable _grabInteractable;
        [SerializeField] private Renderer _body;
        [SerializeField] private Text _label;
        [SerializeField] private Renderer _leftPort;
        [SerializeField] private Renderer _rightPort;

        private ModelDesignSceneController _owner;
        private MaterialPropertyBlock _properties;
        private Rigidbody _rigidbody;
        private readonly Color[] _portColors = new Color[2];
        private int _hoveredPort = -1;

        public LayerDefinition Layer { get; private set; }
        public bool IsPalette { get; private set; }
        public bool IsGrabbed => _grabInteractable.isSelected;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _grabInteractable.throwOnDetach = false;
            _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grabInteractable.trackRotation = false;
            _grabInteractable.useDynamicAttach = true;
            _grabInteractable.attachEaseInTime = 0f;
            _grabInteractable.selectEntered.AddListener(HandleGrabbed);
            _grabInteractable.selectExited.AddListener(HandleReleased);
            _grabInteractable.activated.AddListener(HandleTapped);
        }

        public void Initialize(ModelDesignSceneController owner, LayerDefinition layer, bool isPalette)
        {
            _owner = owner;
            Layer = layer;
            IsPalette = isPalette;
            Layer.OnChanged += RefreshLabel;
            SetColor(_body, GetLayerColor(layer.Type));
            _leftPort.gameObject.SetActive(layer.Type != LayerType.Input);
            _rightPort.gameObject.SetActive(layer.Type != LayerType.Output);
            RefreshLabel();
            SetPortState(0, false, false);
            SetPortState(1, false, false);
        }

        public void TakeFromPalette()
        {
            IsPalette = false;
            _grabInteractable.retainTransformParent = false;
        }

        public void CancelGrab()
        {
            if (_grabInteractable.isSelected && _grabInteractable.interactionManager != null)
            {
                _grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grabInteractable);
            }
        }

        public Vector3 GetPortPosition(int port)
        {
            return (port == 0 ? _leftPort : _rightPort).transform.position;
        }

        public bool HasPort(int port)
        {
            return port == 0 ? Layer.Type != LayerType.Input : Layer.Type != LayerType.Output;
        }

        internal bool TryGetPort(Collider collider, out int port)
        {
            port = -1;
            if (collider == null)
            {
                return false;
            }

            if (collider.gameObject == _leftPort.gameObject && HasPort(0))
            {
                port = 0;
            }
            else if (collider.gameObject == _rightPort.gameObject && HasPort(1))
            {
                port = 1;
            }

            return port != -1;
        }

        internal void SetHoveredPort(int port)
        {
            if (_hoveredPort == port)
            {
                return;
            }

            _hoveredPort = port;
            RefreshPortColor(0);
            RefreshPortColor(1);
        }

        public void Translate(Vector3 delta)
        {
            Vector3 position = transform.position + delta;
            transform.position = position;
            _rigidbody.position = position;
        }

        public void SetPortState(int port, bool connected, bool candidate)
        {
            _portColors[port] = candidate ? new Color(1f, 0.8f, 0.2f) :
                connected ? new Color(0.2f, 1f, 0.7f) : new Color(0.55f, 0.65f, 0.75f);
            RefreshPortColor(port);
        }

        private void RefreshPortColor(int port)
        {
            Color color = port == _hoveredPort ? Color.red : _portColors[port];
            SetColor(port == 0 ? _leftPort : _rightPort, color);
        }

        private void SetColor(Renderer target, Color color)
        {
            _properties.SetColor(BaseColor, color);
            target.SetPropertyBlock(_properties);
        }

        private void RefreshLabel()
        {
            switch (Layer.Type)
            {
                case LayerType.Conv2D:
                    _label.text = $"CONV2D\n{Layer.Filters} filters / {Layer.Height} x {Layer.Width}\n{Layer.Activation}";
                    break;
                case LayerType.MaxPooling2D:
                    _label.text = $"MAX POOL\n{Layer.Height} x {Layer.Width}";
                    break;
                case LayerType.Dense:
                    _label.text = $"DENSE\n{Layer.Units} units / {Layer.Activation}";
                    break;
                case LayerType.Flatten:
                    _label.text = "FLATTEN\nImage to vector";
                    break;
                case LayerType.Input:
                    _label.text = "INPUT\n28 x 28 x 1";
                    break;
                case LayerType.Output:
                    _label.text = "OUTPUT\n10 classes / Softmax";
                    break;
            }
        }

        private static Color GetLayerColor(LayerType type)
        {
            switch (type)
            {
                case LayerType.Conv2D: return new Color(0.1f, 0.3f, 0.48f);
                case LayerType.MaxPooling2D: return new Color(0.32f, 0.19f, 0.48f);
                case LayerType.Flatten: return new Color(0.42f, 0.24f, 0.08f);
                case LayerType.Dense: return new Color(0.12f, 0.36f, 0.29f);
                default: return new Color(0.2f, 0.26f, 0.33f);
            }
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            if (_owner != null && _owner.isActiveAndEnabled)
            {
                _owner.BeginGrab(this);
            }
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            if (_owner != null && _owner.isActiveAndEnabled)
            {
                _owner.EndGrab(this, args.isCanceled);
            }
        }

        private void HandleTapped(ActivateEventArgs args)
        {
            if (_owner != null && _owner.isActiveAndEnabled && !IsPalette && !IsGrabbed)
            {
                _owner.ShowSettings(this);
            }
        }

        private void OnDestroy()
        {
            if (Layer != null)
            {
                Layer.OnChanged -= RefreshLabel;
            }
            _grabInteractable.selectEntered.RemoveListener(HandleGrabbed);
            _grabInteractable.selectExited.RemoveListener(HandleReleased);
            _grabInteractable.activated.RemoveListener(HandleTapped);
        }
    }
}
