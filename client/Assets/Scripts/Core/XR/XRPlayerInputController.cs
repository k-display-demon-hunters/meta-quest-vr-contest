using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace XR
{
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin))]
    public sealed class XRPlayerInputController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _turnSpeed = 60f;
        [SerializeField, Range(0f, 80f)] private float _maximumPitch = 60f;

        private XROrigin _rig;
        private Transform _viewOffset;
        private Quaternion _initialViewRotation;
        private Vector3 _frontDirection;
        private float _pitch;
        private InputAction _look;
        private InputAction _recenter;
        private InputAction _laser;
        private InputAction _quit;
        private XRRayInteractor _rightRay;
        private XRInteractorLineVisual _lineVisual;
        private LineRenderer _line;
        private XRInputButtonReader _uiPress;
        private XRInputButtonReader _previousUIPress;
        private bool _previousUIInteraction;
        private bool _previousHoveredActivate;
        private bool _previousHitClosestOnly;
        private bool _previousManipulation;
        private XRBaseInputInteractor.InputTriggerType _previousSelectTrigger;
        private bool _laserEnabled = true;
        private bool _tutorialInputSuppressed;
        private bool _tutorialRayForcedOn;
        private bool _laserStateBeforeTutorial;
        private Transform _handRayOrigin;
        private Transform _handAttach;
        private Pose _handAttachPose;
        private Transform _gazeAttach;
        private bool _followingGaze;
        private Transform _previousLineOrigin;
        private bool _previousLineOriginOverride;
        private float _previousLineBend;
        private bool _bound;

        public bool TutorialInputSuppressed
        {
            get => _tutorialInputSuppressed;
            set => _tutorialInputSuppressed = value;
        }

        public bool TutorialRayForcedOn
        {
            get => _tutorialRayForcedOn;
            set
            {
                if (_tutorialRayForcedOn == value)
                {
                    return;
                }
                _tutorialRayForcedOn = value;
                if (value)
                {
                    _laserStateBeforeTutorial = _laserEnabled;
                    if (_rightRay != null)
                    {
                        SetLaserEnabled(true);
                    }
                }
                else if (_rightRay != null)
                {
                    SetLaserEnabled(_laserStateBeforeTutorial);
                }
                else
                {
                    _laserEnabled = _laserStateBeforeTutorial;
                }
            }
        }

        private void Awake()
        {
            _rig = GetComponent<XROrigin>();
            _viewOffset = _rig.CameraFloorOffsetObject.transform;
            _initialViewRotation = _viewOffset.localRotation;
            _frontDirection = Vector3.ProjectOnPlane(_rig.Origin.transform.forward, Vector3.up).normalized;
            _look = new InputAction("Look", InputActionType.Value,
                "<XRController>{RightHand}/primary2DAxis", processors: "stickDeadzone(min=0.15,max=0.95)");
            _recenter = new InputAction("Restore front view", InputActionType.Button,
                "<XRController>{RightHand}/primaryButton");
            _laser = new InputAction("Toggle laser", InputActionType.Button,
                "<XRController>{RightHand}/secondaryButton");
            _quit = new InputAction("Quit application", InputActionType.Button,
                "<XRController>{LeftHand}/gripPressed");
        }

        private void OnEnable()
        {
            _look.Enable();
            _recenter.Enable();
            _laser.Enable();
            _quit.Enable();
            if (_rightRay != null)
            {
                BindRay();
            }
        }

        private void Start()
        {
            EnsureRayReady();
        }

        public void EnsureRayReady()
        {
            if (_bound)
            {
                return;
            }
            foreach (XRRayInteractor ray in GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.handedness == InteractorHandedness.Right)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null || _rig.Camera == null)
            {
                Debug.LogError("[XR] The player rig needs a camera and a right-hand ray.", this);
                enabled = false;
                return;
            }
            _lineVisual = _rightRay.GetComponent<XRInteractorLineVisual>();
            _line = _rightRay.GetComponent<LineRenderer>();
            if (_gazeAttach == null)
            {
                _gazeAttach = new GameObject("Gaze Grab Anchor").transform;
                _gazeAttach.SetParent(_rig.Camera.transform, false);
            }
            if (_uiPress == null)
            {
                _uiPress = new XRInputButtonReader("Right Trigger UI",
                    inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
                {
                    inputActionPerformed = new InputAction("Right UI Click", InputActionType.Button,
                        "<XRController>{RightHand}/triggerPressed"),
                    inputActionValue = new InputAction("Right UI Click Value", InputActionType.Value,
                        "<XRController>{RightHand}/trigger")
                };
            }
            BindRay();
        }

        private void Update()
        {
            if (_tutorialInputSuppressed)
            {
                return;
            }
            if (_quit.WasPressedThisFrame())
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return;
            }
            if (_rig.Camera == null)
            {
                return;
            }
            if (_recenter.WasPressedThisFrame())
            {
                RestoreFrontView();
            }
            else
            {
                RotateView(_look.ReadValue<Vector2>());
            }
            if (!_tutorialRayForcedOn && _laser.WasPressedThisFrame() && _rightRay != null)
            {
                SetLaserEnabled(!_laserEnabled);
            }
        }

        private void BindRay()
        {
            if (_bound || _uiPress == null)
            {
                return;
            }
            _previousUIPress = _rightRay.uiPressInput;
            _previousUIInteraction = _rightRay.enableUIInteraction;
            _previousHoveredActivate = _rightRay.allowHoveredActivate;
            _previousHitClosestOnly = _rightRay.hitClosestOnly;
            _previousManipulation = _rightRay.manipulateAttachTransform;
            _previousSelectTrigger = _rightRay.selectActionTrigger;
            _rightRay.uiPressInput = _uiPress;
            _rightRay.enableUIInteraction = true;
            _rightRay.allowHoveredActivate = true;
            _rightRay.hitClosestOnly = true;
            // The right stick controls the view, including while an object is held.
            _rightRay.manipulateAttachTransform = false;
            _rightRay.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.StateChange;
            _rightRay.selectEntered.AddListener(HandleGrabbed);
            _rightRay.selectExited.AddListener(HandleReleased);
            _bound = true;
            SetLaserEnabled(_tutorialRayForcedOn || _laserEnabled);
        }

        private void RotateView(Vector2 stick)
        {
            Transform cameraTransform = _rig.Camera.transform;
            _rig.RotateAroundCameraUsingOriginUp(stick.x * _turnSpeed * Time.deltaTime);
            float pitch = Mathf.Clamp(_pitch - stick.y * _turnSpeed * Time.deltaTime,
                -_maximumPitch, _maximumPitch);
            // Pitch the tracked camera and hands together while keeping the origin upright for walking.
            Vector3 right = Vector3.Cross(Vector3.up, cameraTransform.forward).normalized;
            if (right.sqrMagnitude > 0.01f)
            {
                _viewOffset.RotateAround(cameraTransform.position, right, pitch - _pitch);
                _pitch = pitch;
            }
        }

        public void RestoreFrontView()
        {
            Vector3 cameraPosition = _rig.Camera.transform.position;
            _viewOffset.localRotation = _initialViewRotation;
            _viewOffset.position += cameraPosition - _rig.Camera.transform.position;
            _pitch = 0f;
            _rig.MatchOriginUpCameraForward(Vector3.up, _frontDirection);
        }

        private void SetLaserEnabled(bool active)
        {
            _laserEnabled = active;
            // Disabling the interactor also cancels a held selection and blocks invisible clicks.
            _rightRay.enabled = active;
            if (_lineVisual != null)
            {
                _lineVisual.enabled = active;
            }
            if (_line != null)
            {
                _line.enabled = active;
            }
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            if (_followingGaze || args.interactableObject is not XRGrabInteractable)
            {
                return;
            }
            _handRayOrigin = _rightRay.rayOriginTransform;
            _handAttach = _rightRay.attachTransform;
            _handAttachPose = _rightRay.GetLocalAttachPoseOnSelect(args.interactableObject);
            // Preserve the picked point and distance so switching to gaze does not snap the object.
            _gazeAttach.SetPositionAndRotation(_handAttach.position, _handAttach.rotation);
            _rightRay.attachTransform = _gazeAttach;
            _rightRay.rayOriginTransform = _rig.Camera.transform;
            if (_lineVisual != null)
            {
                _previousLineOrigin = _lineVisual.lineOriginTransform;
                _previousLineOriginOverride = _lineVisual.overrideInteractorLineOrigin;
                _previousLineBend = _lineVisual.lineBendRatio;
                _lineVisual.lineOriginTransform = _handRayOrigin;
                _lineVisual.overrideInteractorLineOrigin = true;
                _lineVisual.lineBendRatio = 0f;
            }
            _followingGaze = true;
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            RestoreHandRay();
        }

        private void RestoreHandRay()
        {
            if (!_followingGaze)
            {
                return;
            }
            _rightRay.attachTransform = _handAttach;
            _handAttach.SetLocalPositionAndRotation(_handAttachPose.position, _handAttachPose.rotation);
            _rightRay.rayOriginTransform = _handRayOrigin;
            if (_lineVisual != null)
            {
                _lineVisual.lineOriginTransform = _previousLineOrigin;
                _lineVisual.overrideInteractorLineOrigin = _previousLineOriginOverride;
                _lineVisual.lineBendRatio = _previousLineBend;
            }
            _followingGaze = false;
        }

        private void OnDisable()
        {
            _look?.Disable();
            _recenter?.Disable();
            _laser?.Disable();
            _quit?.Disable();
            if (!_bound || _rightRay == null)
            {
                return;
            }
            RestoreHandRay();
            _rightRay.selectEntered.RemoveListener(HandleGrabbed);
            _rightRay.selectExited.RemoveListener(HandleReleased);
            _rightRay.uiPressInput = _previousUIPress;
            _rightRay.enableUIInteraction = _previousUIInteraction;
            _rightRay.allowHoveredActivate = _previousHoveredActivate;
            _rightRay.hitClosestOnly = _previousHitClosestOnly;
            _rightRay.manipulateAttachTransform = _previousManipulation;
            _rightRay.selectActionTrigger = _previousSelectTrigger;
            _bound = false;
        }

        private void OnDestroy()
        {
            _look?.Dispose();
            _recenter?.Dispose();
            _laser?.Dispose();
            _quit?.Dispose();
            _uiPress?.inputActionPerformed.Dispose();
            _uiPress?.inputActionValue.Dispose();
            if (_gazeAttach != null)
            {
                Destroy(_gazeAttach.gameObject);
            }
        }
    }
}
