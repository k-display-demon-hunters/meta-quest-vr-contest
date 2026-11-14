using System;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelBlockController : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private XRGrabInteractable _grabInteractable;
        [SerializeField] private Text _label;
        [SerializeField] private MeshFilter _surface;
        [SerializeField] private MeshFilter[] _inputChannelSurfaces;
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private Text _outputCaption;
        private Mesh _outputMesh;
        private MeshRenderer _supplyRenderer;
        private Material[] _assemblyMaterials;
        private Vector3Int _visualShape = new Vector3Int(-1, -1, -1);
        private bool _visualSpatial;
        private bool _visualEnlarged;
        private bool _visualSupply;

        private float FlowHeight => _visualSpatial
            ? -0.08f + Mathf.Max(0.12f, _visualShape.x * 0.025f + 0.04f) * 0.5f : 0.27f;

        internal Vector3 FlowStart => transform.TransformPoint(new Vector3(_collider.center.x - VisualBounds.size.x * 0.5f - 0.015f, FlowHeight, 0.04f));
        internal Vector3 FlowEnd => transform.TransformPoint(new Vector3(_collider.center.x + VisualBounds.size.x * 0.5f + 0.015f, FlowHeight, 0.04f));

        private Rigidbody _rigidbody;
        private bool _interactionEnabled;
        private Pose _supplyIdlePose;
        private float _supplyIdleStarted;
        private const float SupplyTurnSpeed = 15f;
        private const float SupplyBobHeight = 0.01f;
        private const float SupplyBobFrequency = 0.5f;
        private int _inputChannels = LayerLimits.InputChannels;
        private int _inputHeight = 8;
        private int _inputWidth = 8;
        private bool _hasInputShape;
        private int _inputElements = 16;

        internal event Action<ModelBlockController> OnGrabbed;
        internal event Action<ModelBlockController, bool> OnReleased;

        internal LayerDefinition Layer { get; private set; }
        internal bool IsPalette { get; private set; }
        internal Mesh ExampleMesh => _surface.sharedMesh;
        internal string OutputSizeLabel { get; private set; }
        internal Vector3Int SpatialOutputSize { get; private set; }
        internal int VectorOutputSize { get; private set; }
        internal Bounds VisualBounds { get; private set; }
        internal bool IsGrabbed => _grabInteractable.isSelected;
        internal bool IsFixed => Layer != null && (Layer.Type == LayerType.Input || Layer.Type == LayerType.Output);
        public bool canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _grabInteractable.throwOnDetach = false;
            _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grabInteractable.trackRotation = false;
            _grabInteractable.useDynamicAttach = true;
            _grabInteractable.attachEaseInTime = 0f;
            _grabInteractable.selectFilters.Add(this);
            _grabInteractable.selectEntered.AddListener(HandleGrabbed);
            _grabInteractable.selectExited.AddListener(HandleReleased);
        }

        private void Update()
        {
            if (!IsPalette || IsGrabbed)
            {
                return;
            }

            float elapsed = Time.unscaledTime - _supplyIdleStarted;
            Transform parent = transform.parent;
            float localHeight = SupplyBobHeight / parent.lossyScale.y;
            Vector3 position = _supplyIdlePose.position + Vector3.up *
                (localHeight * Mathf.Sin(elapsed * SupplyBobFrequency * 2f * Mathf.PI));
            Quaternion rotation = Quaternion.AngleAxis(elapsed * SupplyTurnSpeed % 360f, Vector3.up)
                * _supplyIdlePose.rotation;
            SetPose(parent.TransformPoint(position), parent.rotation * rotation);
        }

        internal void Initialize(LayerDefinition layer, bool isPalette, Material supplyFrame = null)
        {
            Layer = layer;
            IsPalette = isPalette;
            if (isPalette)
            {
                _supplyRenderer = _surface.GetComponent<MeshRenderer>();
                _assemblyMaterials = _supplyRenderer.sharedMaterials;
                Material[] supplyMaterials = new Material[_assemblyMaterials.Length + 1];
                Array.Copy(_assemblyMaterials, supplyMaterials, _assemblyMaterials.Length);
                supplyMaterials[_assemblyMaterials.Length] = supplyFrame;
                _supplyRenderer.sharedMaterials = supplyMaterials;
                _supplyIdlePose = new Pose(transform.localPosition, transform.localRotation);
                _supplyIdleStarted = Time.unscaledTime;
            }
            SetOutputLabelsVisible(IsFixed);
            name = isPalette ? layer.Type + " Supply" : layer.Type + " Block";
            RefreshLabel();
        }

        internal void TakeFromPalette()
        {
            IsPalette = false;
            _supplyRenderer.sharedMaterials = _assemblyMaterials;
            _grabInteractable.retainTransformParent = false;
            RefreshLabel();
        }

        internal void SetOutputLabelsVisible(bool visible)
        {
            _label.transform.parent.gameObject.SetActive(visible);
            _outputCaption.transform.parent.gameObject.SetActive(visible);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // Only movable layer blocks can be grabbed; settings belong to the slot controls.
            return Layer != null && !IsFixed && _interactionEnabled;
        }

        internal void SetInteractionEnabled(bool interactionEnabled)
        {
            _interactionEnabled = interactionEnabled;
        }

        internal void SetPose(Vector3 position, Quaternion rotation)
        {
            _grabInteractable.SetTargetPose(new Pose(position, rotation));
            transform.SetPositionAndRotation(position, rotation);
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        internal void CancelGrab()
        {
            if (_grabInteractable.isSelected && _grabInteractable.interactionManager != null)
            {
                _grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grabInteractable);
            }
        }

        internal void RefreshLabel(int? inputChannels = null, int inputHeight = 0, int inputWidth = 0,
            int inputElements = 0)
        {
            if (inputChannels.HasValue)
            {
                _inputChannels = inputChannels.Value;
                _inputHeight = inputHeight;
                _inputWidth = inputWidth;
                _inputElements = inputElements;
                _hasInputShape = true;
            }
            bool spatial = Layer.Type == LayerType.Input || Layer.Type == LayerType.Conv2D || Layer.Type == LayerType.MaxPooling2D;
            int height = _inputHeight;
            int width = _inputWidth;
            int channels = _inputChannels;
            int elements = 0;
            string title;
            switch (Layer.Type)
            {
                case LayerType.Input:
                    height = LayerLimits.InputHeight;
                    width = LayerLimits.InputWidth;
                    channels = LayerLimits.InputChannels;
                    title = "INPUT / RGB";
                    break;
                case LayerType.Conv2D:
                    height = Mathf.Max(0, (_hasInputShape ? height : LayerLimits.InputHeight) - Layer.Height + 1);
                    width = Mathf.Max(0, (_hasInputShape ? width : LayerLimits.InputWidth) - Layer.Width + 1);
                    channels = channels > 0 && height > 0 && width > 0 ? Layer.Filters : 0;
                    title = $"CONV / {Layer.Height} x {Layer.Width} / {Layer.Activation}";
                    break;
                case LayerType.MaxPooling2D:
                    height /= Layer.Height;
                    width /= Layer.Width;
                    channels = height > 0 && width > 0 ? channels : 0;
                    title = $"MAX POOL / {Layer.Height} x {Layer.Width}";
                    break;
                case LayerType.Flatten:
                    elements = (_hasInputShape ? height * width : 16) * channels;
                    title = "FLATTEN / same element count";
                    break;
                case LayerType.Dense:
                    elements = !_hasInputShape || _inputElements > 0 ? Layer.Units : 0;
                    title = $"DENSE / {Layer.Activation}";
                    break;
                default:
                    elements = Layer.Units;
                    title = "OUTPUT / Softmax";
                    break;
            }
            if (spatial && channels == 0)
            {
                height = width = 0;
            }
            SpatialOutputSize = spatial ? new Vector3Int(width, height, channels) : Vector3Int.zero;
            VectorOutputSize = elements;
            OutputSizeLabel = spatial ? (channels > 0 ? $"{height}×{width}×{channels}" : "—")
                : elements > 0 ? elements.ToString() : "—";
            string shapeLabel = spatial ? $"{height} × {width} × {channels}" : $"{elements} elements";
            _label.text = title + "\n" + shapeLabel
                + (!_hasInputShape && !IsFixed ? "\nExample" : "");
            _outputCaption.text = spatial
                ? (channels > 0 ? (Layer.Type == LayerType.Input ? "RGB / 3 channels shown" : $"{channels} channels / 1 shown") : "Spatial output unavailable")
                : (elements > 0 ? VectorBlockMesh.VectorCaption(elements) : "Input unavailable");
            bool enlargedExample = IsPalette && Layer.Type == LayerType.MaxPooling2D;
            Vector3Int shape = new Vector3Int(spatial ? height : 0, spatial ? width : 0, elements);
            if (_visualShape == shape && _visualSpatial == spatial && _visualEnlarged == enlargedExample
                && _visualSupply == IsPalette)
            {
                return;
            }
            _visualShape = shape;
            _visualSpatial = spatial;
            _visualEnlarged = enlargedExample;
            _visualSupply = IsPalette;
            Mesh previousMesh = _outputMesh;
            _outputMesh = VectorBlockMesh.CreateOutput(shape.x, shape.y, elements, spatial, enlargedExample,
                IsPalette, IsPalette && Layer.Type == LayerType.Flatten);
            _surface.sharedMesh = _outputMesh;
            _collider.center = _outputMesh.bounds.center;
            VisualBounds = new Bounds(_collider.center, _outputMesh.bounds.size + new Vector3(0.02f, 0.02f, 0.03f));
            _collider.size = VisualBounds.size;
            if (!IsFixed) { _collider.size += new Vector3(0.06f, 0.04f, 0.10f); }
            if (Layer.Type == LayerType.Input)
            {
                foreach (MeshFilter channelSurface in _inputChannelSurfaces)
                {
                    channelSurface.sharedMesh = _outputMesh;
                }
                _collider.center += new Vector3(0.04f, 0f, 0.12f);
                _collider.size += new Vector3(0.08f, 0f, 0.24f);
                VisualBounds = new Bounds(_collider.center, _collider.size);
            }
            if (previousMesh != null)
            {
                Destroy(previousMesh);
            }
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            if (_interactionEnabled)
            {
                OnGrabbed?.Invoke(this);
            }
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            if (_interactionEnabled)
            {
                OnReleased?.Invoke(this, args.isCanceled);
            }
        }

        private void OnDestroy()
        {
            Destroy(_outputMesh);
            _grabInteractable.selectFilters.Remove(this);
            _grabInteractable.selectEntered.RemoveListener(HandleGrabbed);
            _grabInteractable.selectExited.RemoveListener(HandleReleased);
        }
    }
}
