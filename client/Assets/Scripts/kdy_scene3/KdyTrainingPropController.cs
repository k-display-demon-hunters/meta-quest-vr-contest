using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KdyIntro
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class KdyTrainingPropController : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private KdyIntroManager _manager;
        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private bool _wasGrabbed;
        public bool canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            _grab = gameObject.AddComponent<XRGrabInteractable>();
            _grab.enabled = false;
            _grab.colliders.Clear();
            _grab.colliders.Add(GetComponent<BoxCollider>());
            _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            _grab.throwOnDetach = false;
            _grab.useDynamicAttach = true;
            _grab.selectFilters.Add(this);
            _grab.enabled = true;
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(HandleGrab);
            _grab.selectExited.AddListener(HandleRelease);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // The left hand holds the tablet; this exercise belongs to the right hand.
            return interactor.transform.name == "RightHand Controller" && !_manager.IsTransitioning;
        }

        private void HandleGrab(SelectEnterEventArgs args)
        {
            _wasGrabbed = true;
            _manager.ReportGrabPractice(false);
        }

        private void HandleRelease(SelectExitEventArgs args)
        {
            if (_wasGrabbed)
            {
                _manager.ReportGrabPractice(true);
            }
            // Keep the exercise within reach after releasing it.
            _body.position = _homePosition;
            _body.rotation = _homeRotation;
            transform.SetPositionAndRotation(_homePosition, _homeRotation);
        }

        private void OnDisable()
        {
            if (_grab != null)
            {
                _grab.selectEntered.RemoveListener(HandleGrab);
                _grab.selectExited.RemoveListener(HandleRelease);
            }
        }
    }
}
