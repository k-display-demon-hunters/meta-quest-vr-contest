using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ModelDesignScene
{
    public sealed class ModelXRInputController : MonoBehaviour
    {
        private XRRayInteractor _rightRay;
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
                _rightRay = null;
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
                if (ray.handedness == InteractorHandedness.Right && ray.gameObject.activeInHierarchy)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null)
            {
                Debug.LogWarning("[ModelDesign] Enter the Model Design Lab in FactoryEnvironment to use the shared XR player rig.", this);
                return;
            }
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
