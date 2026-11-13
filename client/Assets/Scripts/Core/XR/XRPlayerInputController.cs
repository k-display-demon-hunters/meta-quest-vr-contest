using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
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
        private ContinuousMoveProvider _moveProvider;
        private bool _parchmentMovementBlocked;
        private float _moveSpeedBeforeParchment;
        private Transform _viewOffset;
        private Quaternion _initialViewRotation;
        private float _pitch;
        private InputAction _look;
        private InputAction _recenter;
        private InputAction _laser;
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
        private bool _bound;

        public void SetParchmentMovementBlocked(bool blocked)
        {
            if (_parchmentMovementBlocked == blocked) { return; }
            _parchmentMovementBlocked = blocked;
            if (blocked)
            {
                _moveSpeedBeforeParchment = _moveProvider.moveSpeed;
                _moveProvider.moveSpeed = 0f;
            }
            else
            {
                _moveProvider.moveSpeed = _moveSpeedBeforeParchment;
            }
        }

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
            _moveProvider = GetComponent<ContinuousMoveProvider>();
            _viewOffset = _rig.CameraFloorOffsetObject.transform;
            _initialViewRotation = _viewOffset.localRotation;
            _look = new InputAction("Look", InputActionType.Value,
                "<XRController>{RightHand}/primary2DAxis", processors: "stickDeadzone(min=0.15,max=0.95)");
            _recenter = new InputAction("Restore view tilt", InputActionType.Button,
                "<XRController>{RightHand}/primaryButton");
            _laser = new InputAction("Toggle laser", InputActionType.Button,
                "<XRController>{RightHand}/secondaryButton");
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            _look.Enable();
            _recenter.Enable();
            _laser.Enable();
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
                RestoreUIRegistration();
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
            if (_rig.Camera == null)
            {
                return;
            }
            if (_recenter.WasPressedThisFrame())
            {
                RestoreViewTilt();
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
            // Keep XRI's hand-relative ray and attach point so held objects follow the controller.
            // Holding Grip should also acquire a target reached after the initial button press.
            _rightRay.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
            _bound = true;
            SetLaserEnabled(_tutorialRayForcedOn || _laserEnabled);
            RestoreUIRegistration();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RestoreUIRegistration();
        }

        private void RestoreUIRegistration()
        {
            if (!_bound || _rightRay == null || !_rightRay.isActiveAndEnabled ||
                !_rightRay.enableUIInteraction || _rightRay.TryGetUIModel(out _))
            {
                return;
            }
            // Reconnect to the rig-owned UI module without disabling the interactor or cancelling its held selection.
            _rightRay.enableUIInteraction = false;
            _rightRay.enableUIInteraction = true;
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

        public void RestoreViewTilt()
        {
            Transform cameraTransform = _rig.Camera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);

            _viewOffset.localRotation = _initialViewRotation;
            _viewOffset.position += cameraPosition - cameraTransform.position;
            _pitch = 0f;

            Vector3 restoredForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            // Looking vertically has no horizontal heading to align.
            if (forward.sqrMagnitude > 0.0001f && restoredForward.sqrMagnitude > 0.0001f)
            {
                float yaw = Vector3.SignedAngle(restoredForward, forward, Vector3.up);
                _rig.RotateAroundCameraUsingOriginUp(yaw);
            }
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

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _look?.Disable();
            _recenter?.Disable();
            _laser?.Disable();
            if (!_bound || _rightRay == null)
            {
                return;
            }
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
            _uiPress?.inputActionPerformed.Dispose();
            _uiPress?.inputActionValue.Dispose();
        }
    }
}
