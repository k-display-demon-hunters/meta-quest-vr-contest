using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ModelDesignScene
{
    public sealed class ModelXRInputController : MonoBehaviour
    {
        private XRRayInteractor _rightRay;
        private bool _previousUIInteraction;
        private bool _previousHoveredActivate;
        private bool _previousHitClosestOnly;
        private XRInputButtonReader _previousUIPress;
        private XRInputButtonReader _uiPress;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedUIInputModule;
        private bool _inputRequested;
        private bool _inputEnabled;

        private void OnEnable()
        {
            if (_inputRequested)
            {
                EnableInput();
            }
        }

        public void EnableInput()
        {
            _inputRequested = true;
            if (_inputEnabled || !isActiveAndEnabled)
            {
                return;
            }

            _inputEnabled = true;
            PrepareUIInput();
            BindRightHand();
        }

        public void DisableInput()
        {
            _inputRequested = false;
            ReleaseInput();
        }

        public bool CanActivateSlot(SlotSettingsController control, IXRActivateInteractor interactor)
        {
            return _inputEnabled && isActiveAndEnabled && control != null &&
                _rightRay != null && ReferenceEquals(interactor, _rightRay) && !_rightRay.hasSelection &&
                TryGetRayCollider(out Collider collider) &&
                collider.GetComponentInParent<SlotSettingsController>() == control;
        }

        private void ReleaseInput()
        {
            if (!_inputEnabled)
            {
                return;
            }

            _inputEnabled = false;
            if (_rightRay != null)
            {
                _rightRay.uiPressInput = _previousUIPress;
                _rightRay.enableUIInteraction = _previousUIInteraction;
                _rightRay.allowHoveredActivate = _previousHoveredActivate;
                _rightRay.hitClosestOnly = _previousHitClosestOnly;
                _rightRay = null;
            }
            _previousUIPress = null;

            if (_uiPress != null)
            {
                _uiPress.inputActionPerformed.Dispose();
                _uiPress.inputActionValue.Dispose();
                _uiPress = null;
            }
            if (_ownedEventSystem != null)
            {
                Destroy(_ownedEventSystem);
                _ownedEventSystem = null;
            }
            if (_addedUIInputModule != null)
            {
                Destroy(_addedUIInputModule);
                _addedUIInputModule = null;
            }
        }

        private bool TryGetRayCollider(out Collider collider)
        {
            collider = null;
            if (_rightRay != null && _rightRay.isActiveAndEnabled &&
                _rightRay.TryGetCurrentRaycast(out RaycastHit? hit, out _, out _, out _, out bool isUIHitClosest) &&
                !isUIHitClosest && hit.HasValue)
            {
                collider = hit.Value.collider;
                return collider != null;
            }
            return false;
        }

        private void BindRightHand()
        {
            foreach (XRRayInteractor ray in FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None))
            {
                if (ray.handedness == InteractorHandedness.Right && ray.isActiveAndEnabled)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null)
            {
                Debug.LogWarning("[ModelDesign] Enter this content scene from QuestMain to use its XR player rig.", this);
                return;
            }

            _previousUIInteraction = _rightRay.enableUIInteraction;
            _previousHoveredActivate = _rightRay.allowHoveredActivate;
            _previousHitClosestOnly = _rightRay.hitClosestOnly;
            _previousUIPress = _rightRay.uiPressInput;
            _uiPress = new XRInputButtonReader("Model Design UI", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = new InputAction("Model Design UI Press", InputActionType.Button,
                    "<XRController>{RightHand}/triggerPressed"),
                inputActionValue = new InputAction("Model Design UI Value", InputActionType.Value,
                    "<XRController>{RightHand}/trigger")
            };
            _rightRay.uiPressInput = _uiPress;
            _rightRay.enableUIInteraction = true;
            _rightRay.allowHoveredActivate = true;
            _rightRay.hitClosestOnly = true;
        }

        private void PrepareUIInput()
        {
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Model Design UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedUIInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void OnDisable()
        {
            // Keep the manager's request so re-enabling this component restores its input.
            ReleaseInput();
        }
    }
}
