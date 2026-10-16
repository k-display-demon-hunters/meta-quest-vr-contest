using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace KdyIntro
{
    public sealed class KdyRigController : MonoBehaviour
    {
        [SerializeField] private Transform _tabletScreen;
        private XROrigin _origin;
        private XR.LeftHandTablet _legacyTablet;
        private Canvas _legacyCanvas;
        private bool _tabletWasEnabled;
        private bool _canvasWasActive;
        private bool _bound;
        private XRInteractorLineVisual[] _legacyRays;
        private bool[] _rayWasEnabled;

        public Transform RightHand { get; private set; }
        public Camera ViewCamera { get; private set; }
        public bool Ready => _bound;

        public void BindRig(bool resetView)
        {
            _origin = FindFirstObjectByType<XROrigin>();
            if (_origin == null)
            {
                return;
            }
            ViewCamera = _origin.Camera;
            Transform tablet = null;
            foreach (Transform part in _origin.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == "RightHand Controller")
                {
                    RightHand = part;
                }
                else if (part.name == "Tablet")
                {
                    tablet = part;
                }
            }
            _legacyTablet = _origin.GetComponentInChildren<XR.LeftHandTablet>(true);
            if (_legacyTablet != null)
            {
                _tabletWasEnabled = _legacyTablet.enabled;
                _legacyTablet.enabled = false;
                _legacyCanvas = _legacyTablet.GetComponentInChildren<Canvas>(true);
                if (_legacyCanvas != null)
                {
                    _canvasWasActive = _legacyCanvas.gameObject.activeSelf;
                    _legacyCanvas.gameObject.SetActive(false);
                }
            }
            if (tablet != null && _tabletScreen != null)
            {
                _tabletScreen.SetParent(tablet, false);
                _tabletScreen.localPosition = new Vector3(0f, 0f, -.008f);
                _tabletScreen.localRotation = Quaternion.identity;
                _tabletScreen.localScale = Vector3.one * .0005f;
            }
            _legacyRays = _origin.GetComponentsInChildren<XRInteractorLineVisual>(true);
            _rayWasEnabled = new bool[_legacyRays.Length];
            for (int i = 0; i < _legacyRays.Length; i++)
            {
                _rayWasEnabled[i] = _legacyRays[i].enabled;
                _legacyRays[i].enabled = false;
            }
#if UNITY_EDITOR
            if (!XRSettings.isDeviceActive && _origin.GetComponentInChildren<KdyDesktopController>(true) == null)
            {
                GameObject desktop = new GameObject("Kdy Desktop Controls");
                desktop.transform.SetParent(_origin.transform, false);
                desktop.AddComponent<KdyDesktopController>();
            }
#endif
            if (resetView && ViewCamera != null && !XRSettings.isDeviceActive)
            {
                _origin.MoveCameraToWorldLocation(new Vector3(0f, 1.65f, 0f));
                _origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            }
            _bound = true;
        }

        private void Update()
        {
            if (!_bound)
            {
                return;
            }
            // Physical XR keeps its real tracking; the keyboard only translates the user's origin.
            if (Keyboard.current != null && ViewCamera != null)
            {
                Keyboard keyboard = Keyboard.current;
                Vector3 forward = Vector3.ProjectOnPlane(ViewCamera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                float y = (keyboard.eKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f);
                Vector3 movement = Vector3.ClampMagnitude(right * x + forward * z + Vector3.up * y, 1f);
                _origin.transform.position += movement * (1.5f * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_tabletScreen != null)
            {
                Destroy(_tabletScreen.gameObject);
            }
            if (_legacyCanvas != null)
            {
                _legacyCanvas.gameObject.SetActive(_canvasWasActive);
            }
            if (_legacyTablet != null)
            {
                _legacyTablet.enabled = _tabletWasEnabled;
            }
            if (_legacyRays != null)
            {
                for (int i = 0; i < _legacyRays.Length; i++)
                {
                    if (_legacyRays[i] != null)
                    {
                        _legacyRays[i].enabled = _rayWasEnabled[i];
                    }
                }
            }
        }
    }
}
