using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KdyIntro
{
    public sealed class KdyControllerInput : MonoBehaviour
    {
        [SerializeField] private KdyIntroManager _manager;
        [SerializeField] private KdyRigController _rig;
        [SerializeField] private KdyTabletController _tablet;
        private InputAction _leftX;
        private InputAction _leftY;
        private KdyActionController _hover;

        private void Awake()
        {
            _leftX = new InputAction("Kdy Tablet Previous", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            _leftY = new InputAction("Kdy Tablet Next", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
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
            KdyActionController action = null;
            if (ray != null && ray.isActiveAndEnabled && !ray.hasSelection &&
                ray.TryGetCurrentRaycast(out RaycastHit? hit, out _, out _, out _, out bool isUIHitClosest) &&
                !isUIHitClosest && hit.HasValue)
            {
                KdyActionController candidate = hit.Value.collider.GetComponentInParent<KdyActionController>();
                if (candidate != null && candidate.Owner == _manager)
                {
                    action = candidate;
                }
            }
            SetHover(action);
            if (action != null && ray.activateInput.ReadWasPerformedThisFrame())
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
