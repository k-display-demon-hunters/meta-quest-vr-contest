using UnityEngine;
using UnityEngine.InputSystem;

namespace KdyIntro
{
    public sealed class KdyControllerInput : MonoBehaviour
    {
        [SerializeField] private KdyIntroManager _manager;
        [SerializeField] private KdyRigController _rig;
        [SerializeField] private KdyTabletController _tablet;
        [SerializeField] private Material _rayMaterial;
        private InputAction _trigger;
        private InputAction _leftX;
        private InputAction _leftY;
        private KdyActionController _hover;
        private LineRenderer _ray;

        private void Awake()
        {
            _trigger = new InputAction("Kdy Right Trigger", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            _leftX = new InputAction("Kdy Tablet Previous", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            _leftY = new InputAction("Kdy Tablet Next", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        }

        private void OnEnable()
        {
            _trigger.Enable();
            _leftX.Enable();
            _leftY.Enable();
        }

        private void Start()
        {
            GameObject rayObject = new GameObject("Kdy Right Selection Ray");
            rayObject.transform.SetParent(transform, false);
            _ray = rayObject.AddComponent<LineRenderer>();
            _ray.useWorldSpace = true;
            _ray.positionCount = 2;
            _ray.startWidth = .006f;
            _ray.endWidth = .002f;
            _ray.startColor = new Color(.1f, .8f, 1f, .6f);
            _ray.endColor = Color.cyan;
            _ray.sharedMaterial = _rayMaterial;
            _ray.enabled = false;
        }

        private void Update()
        {
            if (!_rig.Ready || _rig.RightHand == null || _manager.IsTransitioning)
            {
                SetHover(null);
                if (_ray != null)
                {
                    _ray.enabled = false;
                }
                return;
            }
            Transform hand = _rig.RightHand;
            Ray ray = new Ray(hand.position, hand.forward);
            Vector3 endpoint = ray.GetPoint(12f);
            KdyActionController action = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                endpoint = hit.point;
                KdyActionController candidate = hit.collider.GetComponentInParent<KdyActionController>();
                if (candidate != null && candidate.Owner == _manager)
                {
                    action = candidate;
                }
            }
            SetHover(action);
            _ray.enabled = true;
            _ray.SetPosition(0, ray.origin);
            _ray.SetPosition(1, endpoint);
            if (_trigger.WasPressedThisFrame() && action != null)
            {
                action.Press();
            }
            if (_leftX.WasPressedThisFrame())
            {
                _tablet.ChangeTab(-1);
            }
            else if (_leftY.WasPressedThisFrame())
            {
                _tablet.ChangeTab(1);
            }
        }

        private void SetHover(KdyActionController action)
        {
            if (_hover == action)
            {
                return;
            }
            if (_hover != null)
            {
                _hover.SetHover(false);
            }
            _hover = action;
            if (_hover != null)
            {
                _hover.SetHover(true);
            }
        }

        private void OnDisable()
        {
            SetHover(null);
            _trigger.Disable();
            _leftX.Disable();
            _leftY.Disable();
        }

        private void OnDestroy()
        {
            _trigger.Dispose();
            _leftX.Dispose();
            _leftY.Dispose();
        }
    }
}
