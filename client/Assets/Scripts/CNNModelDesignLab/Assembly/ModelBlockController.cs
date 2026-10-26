using System;
using ModelDesignScene.Editing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

namespace ModelDesignScene.Assembly
{
    public sealed class ModelBlockController : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private XRGrabInteractable _grabInteractable;
        [SerializeField] private Text _label;

        private Rigidbody _rigidbody;
        private bool _interactionEnabled;

        internal event Action<ModelBlockController> OnGrabbed;
        internal event Action<ModelBlockController, bool> OnReleased;

        internal LayerDefinition Layer { get; private set; }
        internal bool IsPalette { get; private set; }
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

        internal void Initialize(LayerDefinition layer, bool isPalette)
        {
            Layer = layer;
            IsPalette = isPalette;
            name = isPalette ? layer.Type + " Supply" : layer.Type + " Block";
            RefreshLabel();
        }

        internal void TakeFromPalette()
        {
            IsPalette = false;
            _grabInteractable.retainTransformParent = false;
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

        internal void RefreshLabel()
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
            _grabInteractable.selectFilters.Remove(this);
            _grabInteractable.selectEntered.RemoveListener(HandleGrabbed);
            _grabInteractable.selectExited.RemoveListener(HandleReleased);
        }
    }
}
