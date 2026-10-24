using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KdyLearning
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class KdyLessonGrabController : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private bool _trackPosition = true;
        [SerializeField] private bool _trackRotation = true;
        [SerializeField] private bool _returnHome;

        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private bool _interactionEnabled = true;
        private bool _homeCaptured;

        public bool IsGrabbed { get; private set; }
        public bool canProcess => isActiveAndEnabled;
        public event Action<bool> GrabbedChanged;

        private void Awake()
        {
            CaptureHomePose();
            _body.isKinematic = true;
            _body.useGravity = false;
            _grab = GetComponent<XRGrabInteractable>();
            if (_grab == null)
            {
                _grab = gameObject.AddComponent<XRGrabInteractable>();
            }

            // Re-register after excluding the separately selectable cells and handles below this object.
            _grab.enabled = false;
            _grab.colliders.Clear();
            _grab.colliders.Add(GetComponent<BoxCollider>());
            _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            _grab.trackPosition = _trackPosition;
            _grab.trackRotation = _trackRotation;
            _grab.trackScale = false;
            _grab.throwOnDetach = false;
            _grab.useDynamicAttach = true;
            _grab.selectFilters.Add(this);
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(HandleGrab);
            _grab.selectExited.AddListener(HandleRelease);
            _grab.enabled = _interactionEnabled;
        }

        private void CaptureHomePose()
        {
            if (_homeCaptured)
            {
                return;
            }

            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
            _body = GetComponent<Rigidbody>();
            _homeCaptured = true;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            if (_grab != null)
            {
                _grab.enabled = isActiveAndEnabled && enabled;
            }
            if (!enabled)
            {
                SetGrabbed(false);
            }
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return interactor.transform.name == "RightHand Controller";
        }

        public void ResetPose()
        {
            if (IsGrabbed || (_grab != null && _grab.isSelected))
            {
                return;
            }

            CaptureHomePose();
            transform.SetLocalPositionAndRotation(_homePosition, _homeRotation);
            _body.position = transform.position;
            _body.rotation = transform.rotation;
        }

        private void HandleGrab(SelectEnterEventArgs args)
        {
            SetGrabbed(true);
        }

        private void HandleRelease(SelectExitEventArgs args)
        {
            SetGrabbed(false);
            if (_returnHome)
            {
                ResetPose();
            }
        }

        private void SetGrabbed(bool grabbed)
        {
            if (IsGrabbed == grabbed)
            {
                return;
            }

            IsGrabbed = grabbed;
            GrabbedChanged?.Invoke(grabbed);
        }

        private void OnDisable()
        {
            if (_grab != null)
            {
                _grab.enabled = false;
                _grab.selectEntered.RemoveListener(HandleGrab);
                _grab.selectExited.RemoveListener(HandleRelease);
            }

            SetGrabbed(false);
        }
    }
}
