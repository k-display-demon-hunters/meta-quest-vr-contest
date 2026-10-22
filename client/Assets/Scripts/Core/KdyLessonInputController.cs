using KdyIntro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KdyLearning
{
    public sealed class KdyLessonInputController : MonoBehaviour
    {
        [SerializeField] private KdyLessonManager _manager;
        [SerializeField] private KdyRigController _rig;
        [SerializeField] private KdyLessonTabletController _tablet;
        private InputAction _leftX;
        private InputAction _leftY;
        private KdyLessonSurfaceController _hover;

        private void Awake()
        {
            _leftX = new InputAction("Kdy Lesson Previous", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            _leftY = new InputAction("Kdy Lesson Next", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        }

        private void OnEnable()
        {
            _leftX.Enable();
            _leftY.Enable();
        }

        private void Update()
        {
            if (!_rig.Ready || _manager.IsTransitioning)
            {
                SetHover(null);
                return;
            }
            XRRayInteractor ray = _rig.RightRay;
            KdyLessonSurfaceController surface = null;
            RaycastHit? hit = null;
            bool hasHit = ray != null && ray.isActiveAndEnabled && !ray.hasSelection &&
                ray.TryGetCurrentRaycast(out hit, out _, out _, out _, out bool isUIHitClosest) &&
                !isUIHitClosest && hit.HasValue;
            if (hasHit)
            {
                KdyLessonSurfaceController candidate = hit.Value.collider.GetComponentInParent<KdyLessonSurfaceController>();
                if (candidate != null && candidate.Owner == _manager)
                {
                    surface = candidate;
                }
            }
            SetHover(surface);
            if (hasHit && ray.activateInput.ReadWasPerformedThisFrame())
            {
                if (surface != null)
                {
                    surface.Press();
                }
                else
                {
                    _manager.SelectPixel(hit.Value);
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
            _leftX.Disable();
            _leftY.Disable();
        }

        private void OnDestroy()
        {
            _leftX.Dispose();
            _leftY.Dispose();
        }
    }
}
