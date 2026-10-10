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
        [SerializeField] private CNNFeatureMapGrabKind _kind;
        [SerializeField] private Collider[] _grabColliders;
        [SerializeField] private bool _isFilter;
        private CNNFeatureMapScene _scene;
        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private CNNFeatureMapNode _node;
        private CNNFeatureMapPanel _panel;
        private CNNFeatureMapLayer _layer;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homeScale;
        private float _scaleFactor = 1f;
        private bool _mouseHeld;

        public CNNFeatureMapGrabKind Kind => _kind;
        public bool IsHeld => _mouseHeld || (_grab != null && _grab.isSelected);
        public bool IsFilter => _isFilter;
        public CNNFeatureMapScene Workspace => _scene;
        public bool canProcess => isActiveAndEnabled;

        public void Initialize(CNNFeatureMapScene scene)
        {
            _scene = scene;
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
            _grab.enabled = true;
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return !_mouseHeld && _scene != null && _scene.IsReady && _scene.CanGrab(this);
        }

        public bool BeginMouseDrag(bool selectInput = true)
        {
            if (!isActiveAndEnabled || IsHeld || _scene == null || !_scene.IsReady || !_scene.CanGrab(this))
            {
                return false;
            }
            _mouseHeld = true;
            // Only the XR interactable is disabled. This component keeps the shared held-item state.
            _grab.enabled = false;
            BeginInteraction(selectInput);
            return true;
        }

        public void EndMouseDrag()
        {
            if (!_mouseHeld)
            {
                return;
            }
            _mouseHeld = false;
            if (_scene != null)
            {
                _scene.ReleaseTarget(this);
            }
            if (_grab != null && isActiveAndEnabled)
            {
                _grab.enabled = true;
            }
        }

        public void MoveTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            _body.position = worldPosition;
        }

        public void RotateBy(Vector2 delta, Transform cameraTransform)
        {
            if (_isFilter)
            {
                return;
            }
            Quaternion rotation = Quaternion.AngleAxis(-delta.x * .4f, cameraTransform.up) *
                Quaternion.AngleAxis(delta.y * .4f, cameraTransform.right) * transform.rotation;
            transform.rotation = rotation;
            _body.rotation = rotation;
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

        private void BeginInteraction(bool selectInput = true)
        {
            _scene.HoldTarget(this);
            if (_panel != null)
            {
                _panel.Extract();
            }
            if (_node != null)
            {
                _node.Extract();
                if (selectInput)
                {
                    _node.SelectPixel();
                }
            }
        }

        private void HandleSelect(SelectEnterEventArgs args) => BeginInteraction();
        private void HandleRelease(SelectExitEventArgs args) => _scene.ReleaseTarget(this);
        private void HandleHover(HoverEnterEventArgs args) => _scene.SetHoverTarget(this, true);
        private void HandleHoverExit(HoverExitEventArgs args) => _scene.SetHoverTarget(this, false);
        private void HandleActivate(ActivateEventArgs args) => ToggleSpread();

        private void OnDisable()
        {
            _mouseHeld = false;
            if (_grab == null)
            {
                return;
            }
            _grab.selectEntered.RemoveListener(HandleSelect);
            _grab.selectExited.RemoveListener(HandleRelease);
            _grab.hoverEntered.RemoveListener(HandleHover);
            _grab.hoverExited.RemoveListener(HandleHoverExit);
            _grab.activated.RemoveListener(HandleActivate);
            _grab.enabled = false;
            if (_scene != null)
            {
                _scene.ReleaseTarget(this);
                _scene.SetHoverTarget(this, false);
            }
        }
    }
}


