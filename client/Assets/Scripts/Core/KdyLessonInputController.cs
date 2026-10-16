using KdyIntro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KdyLearning
{
    public sealed class KdyLessonInputController : MonoBehaviour
    {
        [SerializeField] private KdyLessonManager _manager;
        [SerializeField] private KdyRigController _rig;
        [SerializeField] private KdyLessonTabletController _tablet;
        [SerializeField] private Material _rayMaterial;
        private InputAction _trigger;
        private InputAction _leftX;
        private InputAction _leftY;
        private KdyLessonSurfaceController _hover;
        private LineRenderer _ray;

        private void Awake()
        {
            _trigger = new InputAction("Kdy Lesson Select", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            _leftX = new InputAction("Kdy Lesson Previous", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            _leftY = new InputAction("Kdy Lesson Next", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        }

        private void OnEnable()
        {
            _trigger.Enable();
            _leftX.Enable();
            _leftY.Enable();
        }

        private void Start()
        {
            GameObject pointer = new GameObject("Kdy Lesson Pointer");
            pointer.transform.SetParent(transform, false);
            _ray = pointer.AddComponent<LineRenderer>();
            _ray.useWorldSpace = true;
            _ray.positionCount = 2;
            _ray.startWidth = .005f;
            _ray.endWidth = .002f;
            _ray.sharedMaterial = _rayMaterial;
            _ray.startColor = new Color(.2f, .9f, 1f, .65f);
            _ray.endColor = Color.cyan;
            _ray.enabled = false;
        }

        private void Update()
        {
            if (!_rig.Ready || _rig.RightHand == null || _manager.IsTransitioning)
            {
                SetHover(null);
                _ray.enabled = false;
                return;
            }
            Transform hand = _rig.RightHand;
            Ray ray = new Ray(hand.position, hand.forward);
            Vector3 endpoint = ray.GetPoint(16f);
            KdyLessonSurfaceController surface = null;
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (hasHit)
            {
                endpoint = hit.point;
                KdyLessonSurfaceController candidate = hit.collider.GetComponentInParent<KdyLessonSurfaceController>();
                if (candidate != null && candidate.Owner == _manager)
                {
                    surface = candidate;
                }
            }
            SetHover(surface);
            _ray.enabled = true;
            _ray.SetPosition(0, hand.position);
            _ray.SetPosition(1, endpoint);
            if (_trigger.WasPressedThisFrame())
            {
                if (surface != null)
                {
                    surface.Press();
                }
                else if (hasHit)
                {
                    _manager.SelectPixel(hit);
                }
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

        private void SetHover(KdyLessonSurfaceController surface)
        {
            if (_hover == surface)
            {
                return;
            }
            _hover?.SetHover(false);
            _hover = surface;
            _hover?.SetHover(true);
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
