using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using XR;

namespace OpeningEnding
{
    [DisallowMultipleComponent]
    public sealed class CinematicMovementController : MonoBehaviour
    {
        private readonly XRInputButtonReader _blockedGripInput = new XRInputButtonReader(
            "Cinematic Grip Lock", inputSourceMode: XRInputButtonReader.InputSourceMode.Unused);

        private ContinuousMoveProvider _moveProvider;
        private XRPlayerInputController _playerInput;
        private XRRayInteractor _rightRay;
        private XRInteractorLineVisual _lineVisual;
        private LineRenderer _line;
        private XRInputButtonReader _previousGripInput;
        private bool _moveWasEnabled;
        private bool _inputWasSuppressed;
        private bool _rayWasForcedOn;
        private bool _rayWasEnabled;
        private bool _lineVisualWasEnabled;
        private bool _lineWasEnabled;
        private bool _hasPendingUIVisibility;
        private bool _uiVisible;

        private bool IsBound { get; set; }

        private void LateUpdate()
        {
            if (!IsBound || !_hasPendingUIVisibility)
            {
                return;
            }
            _hasPendingUIVisibility = false;
            if (_rightRay != null)
            {
                // XRI writes its pointer state back after onClick. Unregister only after that callback has finished.
                _rightRay.enabled = _uiVisible;
            }
        }

        public void Bind(XROrigin rig)
        {
            Release();
            if (rig == null)
            {
                Debug.LogError("[Cinematic] An XR rig is required to control story input.", this);
                return;
            }

            _moveProvider = rig.GetComponentInChildren<ContinuousMoveProvider>(true);
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
            _inputWasSuppressed = _playerInput != null && _playerInput.TutorialInputSuppressed;
            _rayWasForcedOn = _playerInput != null && _playerInput.TutorialRayForcedOn;
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
            if (_moveProvider != null) _moveProvider.enabled = false;
            if (_rightRay != null) _rightRay.selectInput = _blockedGripInput;
        }

        public void SetUIVisible(bool visible)
        {
            if (!IsBound)
            {
                return;
            }
            _uiVisible = visible;
            _hasPendingUIVisibility = true;
            if (_lineVisual != null)
            {
                _lineVisual.enabled = visible;
            }
            if (_line != null)
            {
                _line.enabled = visible;
            }
        }

        public void Release()
        {
            _hasPendingUIVisibility = false;
            if (!IsBound)
            {
                return;
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
                _playerInput.TutorialRayForcedOn = _rayWasForcedOn;
            }
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

            _moveProvider = null;
            _playerInput = null;
            _rightRay = null;
            _lineVisual = null;
            _line = null;
            _previousGripInput = null;
            IsBound = false;
        }

        private void OnDisable()
        {
            Release();
        }
    }
}
