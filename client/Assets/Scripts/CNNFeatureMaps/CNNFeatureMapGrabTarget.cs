using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CNNFeatureMaps
{
    public enum CNNFeatureMapGrabKind { Node, Map, Layer }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class CNNFeatureMapGrabTarget : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private CNNFeatureMapScene _scene;
        [SerializeField] private CNNFeatureMapGrabKind _kind;
        [SerializeField] private Collider[] _grabColliders;
        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private CNNFeatureMapNode _node;
        private CNNFeatureMapPanel _panel;
        private CNNFeatureMapLayer _layer;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homeScale;
        private float _scaleFactor = 1f;
        private bool _isFilter;

        public CNNFeatureMapGrabKind Kind => _kind;
        public bool IsHeld => _grab != null && _grab.isSelected;
        public bool canProcess => isActiveAndEnabled;

        public void ConfigureFilter(CNNFeatureMapScene scene, Collider handle)
        {
            _scene = scene;
            _kind = CNNFeatureMapGrabKind.Map;
            _grabColliders = new Collider[] { handle };
            _isFilter = true;
        }

        private void Awake()
        {
            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
            _homeScale = transform.localScale;
            _node = GetComponent<CNNFeatureMapNode>();
            _panel = GetComponent<CNNFeatureMapPanel>();
            _layer = GetComponent<CNNFeatureMapLayer>();
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            // Explicit colliders prevent layer/map handles from also claiming child cells.
            _grab = gameObject.AddComponent<XRGrabInteractable>();
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
            _grab.hoverEntered.AddListener(HandleHover);
            _grab.hoverExited.AddListener(HandleHoverExit);
            _grab.activated.AddListener(HandleActivate);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return _scene.CanGrab(this);
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
            _scene.HoldTarget(this);
            if (_panel != null)
            {
                _panel.Extract();
            }
            if (_node != null)
            {
                _node.Extract();
                _node.SelectPixel();
            }
        }

        private void HandleSelect(SelectEnterEventArgs args) => BeginInteraction();
        private void HandleRelease(SelectExitEventArgs args) => _scene.ReleaseTarget(this);
        private void HandleHover(HoverEnterEventArgs args) => _scene.SetHoverTarget(this, true);
        private void HandleHoverExit(HoverExitEventArgs args) => _scene.SetHoverTarget(this, false);
        private void HandleActivate(ActivateEventArgs args) => ToggleSpread();

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(HandleSelect);
            _grab.selectExited.RemoveListener(HandleRelease);
            _grab.hoverEntered.RemoveListener(HandleHover);
            _grab.hoverExited.RemoveListener(HandleHoverExit);
            _grab.activated.RemoveListener(HandleActivate);
            _scene.ReleaseTarget(this);
            _scene.SetHoverTarget(this, false);
        }
    }
}


