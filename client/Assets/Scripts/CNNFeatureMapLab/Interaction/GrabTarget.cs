using CNNFeatureMaps.FeatureMaps;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CNNFeatureMaps.Interaction
{
    public enum GrabKind { Node, Map, Layer }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class GrabTarget : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private GrabKind _kind;
        [SerializeField] private Collider[] _grabColliders;
        [SerializeField] private bool _isFilter;
        private InteractionController _interaction;
        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private MapCell _node;
        private MapPanel _panel;
        private MapLayer _layer;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homeScale;
        private float _scaleFactor = 1f;

        public GrabKind Kind => _kind;
        public bool IsHeld => _grab != null && _grab.isSelected;
        public bool CanScale => !_isFilter;
        internal Collider[] GrabColliders => _grabColliders;
        public bool canProcess => isActiveAndEnabled;

        public void Initialize(InteractionController interaction)
        {
            _interaction = interaction;
        }

        private void Awake()
        {
            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
            _homeScale = transform.localScale;
            _node = GetComponent<MapCell>();
            _panel = GetComponent<MapPanel>();
            _layer = GetComponent<MapLayer>();
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            // Explicit colliders prevent layer/map handles from also claiming child cells.
            _grab = gameObject.AddComponent<GrabInteractable>();
            _grab.enabled = false;
            _grab.colliders.Clear();
            _grab.colliders.AddRange(_grabColliders);
            _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            _grab.throwOnDetach = false;
            _grab.trackRotation = !_isFilter;
            _grab.trackScale = false;
            _grab.useDynamicAttach = true;
            _grab.retainTransformParent = true;
            _grab.selectFilters.Add(this);
            _grab.enabled = true;
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(HandleSelect);
            _grab.selectExited.AddListener(HandleRelease);
            _grab.activated.AddListener(HandleActivate);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return _interaction.CanGrab(this);
        }

        public void ScaleBy(float multiplier)
        {
            if (_isFilter)
            {
                return;
            }
            _scaleFactor = Mathf.Clamp(_scaleFactor * multiplier, .4f, 4f);
            transform.localScale = _homeScale * _scaleFactor;
        }

        public void ToggleSpread()
        {
            if (_layer != null)
            {
                _layer.ToggleSpread();
            }
        }

        public void ResetPose()
        {
            if (IsHeld)
            {
                return;
            }
            transform.localPosition = _homePosition;
            transform.localRotation = _homeRotation;
            transform.localScale = _homeScale;
            _body.position = transform.position;
            _body.rotation = transform.rotation;
            _scaleFactor = 1f;
            if (_node != null)
            {
                _node.ReturnToGrid();
            }
            if (_panel != null)
            {
                _panel.ReturnToStack();
            }
        }

        private void BeginInteraction()
        {
            _interaction.HoldTarget(this);
            if (_panel != null)
            {
                _panel.Extract();
            }
            if (_node != null)
            {
                _node.Extract();
            }
        }

        private void HandleSelect(SelectEnterEventArgs args) => BeginInteraction();
        private void HandleRelease(SelectExitEventArgs args) => _interaction.ReleaseTarget(this);
        private void HandleActivate(ActivateEventArgs args)
        {
            _interaction.SelectScaleTarget(this);
            if (_node != null)
            {
                _node.SelectPixel();
            }
            ToggleSpread();
        }

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(HandleSelect);
            _grab.selectExited.RemoveListener(HandleRelease);
            _grab.activated.RemoveListener(HandleActivate);
            _interaction.ReleaseTarget(this);
        }
    }
}


