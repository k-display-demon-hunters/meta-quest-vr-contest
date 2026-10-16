using Tablet;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using XR;

namespace FactoryIntro
{
    [DisallowMultipleComponent]
    public sealed class FactoryIntroInputController : MonoBehaviour
    {
        private readonly XRInputButtonReader _blockedGripInput = new XRInputButtonReader(
            "Factory Intro Grip Lock", inputSourceMode: XRInputButtonReader.InputSourceMode.Unused);

        private ContinuousMoveProvider _moveProvider;
        private TabletManager _tablet;
        private XRPlayerInputController _playerInput;
        private XRRayInteractor _rightRay;
        private XRInteractorLineVisual _lineVisual;
        private LineRenderer _line;
        private XRInputButtonReader _previousGripInput;
        private bool _moveWasEnabled;
        private bool _tabletWasEnabled;
        private bool _inputWasSuppressed;
        private bool _rayWasEnabled;
        private bool _lineVisualWasEnabled;
        private bool _lineWasEnabled;

        public bool IsBound { get; private set; }

        public void Bind(XROrigin rig)
        {
            Release();
            if (rig == null)
            {
                Debug.LogError("[FactoryIntro] An XR rig is required to control tutorial input.", this);
                return;
            }

            _moveProvider = rig.GetComponentInChildren<ContinuousMoveProvider>(true);
            _tablet = rig.GetComponentInChildren<TabletManager>(true);
            _playerInput = rig.GetComponentInChildren<XRPlayerInputController>(true);
            _playerInput?.EnsureRayReady();
            foreach (XRRayInteractor ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.handedness == InteractorHandedness.Right)
                {
                    _rightRay = ray;
                    break;
                }
            }

            _moveWasEnabled = _moveProvider != null && _moveProvider.enabled;
            _tabletWasEnabled = _tablet != null && _tablet.enabled;
            _inputWasSuppressed = _playerInput != null && _playerInput.TutorialInputSuppressed;
            _previousGripInput = _rightRay != null ? _rightRay.selectInput : null;
            _lineVisual = _rightRay != null ? _rightRay.GetComponent<XRInteractorLineVisual>() : null;
            _line = _rightRay != null ? _rightRay.GetComponent<LineRenderer>() : null;
            _rayWasEnabled = _rightRay != null && _rightRay.enabled;
            _lineVisualWasEnabled = _lineVisual != null && _lineVisual.enabled;
            _lineWasEnabled = _line != null && _line.enabled;
            IsBound = true;

            // Keep tracked hands and the right ray alive; only gameplay actions are gated.
            if (_playerInput != null)
            {
                _playerInput.TutorialRayForcedOn = true;
                _playerInput.TutorialInputSuppressed = true;
            }
            if (_rightRay != null)
            {
                _rightRay.enabled = true;
            }
            if (_lineVisual != null)
            {
                _lineVisual.enabled = true;
            }
            if (_line != null)
            {
                _line.enabled = true;
            }
            ApplyPermissions(false, false, false);
        }

        public void SetTutorialStep(TutorialStep step)
        {
            if (!IsBound)
            {
                return;
            }
            if (step == TutorialStep.Complete)
            {
                // The ready screen still needs its ray button. Restore the previous laser
                // state only when the player actually leaves this scene.
                if (_tablet != null)
                {
                    _tablet.enabled = false;
                }
                ApplyPermissions(true, true, true);
                if (_playerInput != null)
                {
                    _playerInput.TutorialInputSuppressed = _inputWasSuppressed;
                }
                return;
            }

            if (_playerInput != null)
            {
                _playerInput.TutorialInputSuppressed = true;
            }
            ApplyPermissions(
                step == TutorialStep.Movement,
                step == TutorialStep.Tablet,
                step == TutorialStep.Grab);
        }

        public void Release()
        {
            if (!IsBound)
            {
                return;
            }

            // Disabling the tablet releases any left-stick override it owns.
            if (_tablet != null)
            {
                _tablet.enabled = false;
                _tablet.enabled = _tabletWasEnabled;
            }
            if (_moveProvider != null)
            {
                _moveProvider.enabled = _moveWasEnabled;
            }
            if (_rightRay != null)
            {
                _rightRay.selectInput = _previousGripInput;
            }
            if (_playerInput != null)
            {
                _playerInput.TutorialInputSuppressed = _inputWasSuppressed;
                _playerInput.TutorialRayForcedOn = false;
            }
            else
            {
                if (_rightRay != null)
                {
                    _rightRay.enabled = _rayWasEnabled;
                }
                if (_lineVisual != null)
                {
                    _lineVisual.enabled = _lineVisualWasEnabled;
                }
                if (_line != null)
                {
                    _line.enabled = _lineWasEnabled;
                }
            }

            _moveProvider = null;
            _tablet = null;
            _playerInput = null;
            _rightRay = null;
            _lineVisual = null;
            _line = null;
            _previousGripInput = null;
            IsBound = false;
        }

        private void ApplyPermissions(bool allowMovement, bool allowTablet, bool allowGrab)
        {
            if (_tablet != null)
            {
                _tablet.enabled = allowTablet && _tabletWasEnabled;
            }
            if (_moveProvider != null)
            {
                _moveProvider.enabled = allowMovement && _moveWasEnabled;
            }
            if (_rightRay != null)
            {
                _rightRay.selectInput = allowGrab ? _previousGripInput : _blockedGripInput;
            }
        }

        private void OnDisable()
        {
            Release();
        }
    }
}
