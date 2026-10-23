using Tablet;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace KdyIntro
{
    public sealed class KdyRigController : MonoBehaviour
    {
        [SerializeField] private Transform _tabletScreen;
        private XROrigin _origin;
        private TabletManager _sharedTablet;
        private Canvas _sharedCanvas;
        private bool _tabletWasEnabled;
        private bool _canvasWasActive;
        private bool _bound;

        public XRRayInteractor RightRay { get; private set; }
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
                if (part.name == "Tablet")
                {
                    tablet = part;
                }
            }
            foreach (XRRayInteractor ray in _origin.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.handedness == InteractorHandedness.Right)
                {
                    RightRay = ray;
                    break;
                }
            }
            // These chapters own their lesson panel; restore the shared tablet when leaving them.
            _sharedTablet = _origin.GetComponentInChildren<TabletManager>(true);
            if (_sharedTablet != null)
            {
                _tabletWasEnabled = _sharedTablet.enabled;
                _sharedTablet.enabled = false;
                _sharedCanvas = _sharedTablet.GetComponentInChildren<Canvas>(true);
                if (_sharedCanvas != null)
                {
                    _canvasWasActive = _sharedCanvas.gameObject.activeSelf;
                    _sharedCanvas.gameObject.SetActive(false);
                }
            }
            if (tablet != null && _tabletScreen != null)
            {
                _tabletScreen.SetParent(tablet, false);
                _tabletScreen.localPosition = new Vector3(0f, 0f, -.008f);
                _tabletScreen.localRotation = Quaternion.identity;
                _tabletScreen.localScale = Vector3.one * .0005f;
            }
            if (resetView && ViewCamera != null && !XRSettings.isDeviceActive)
            {
                _origin.MoveCameraToWorldLocation(new Vector3(0f, 1.65f, 0f));
                _origin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            }
            _bound = true;
        }

        private void OnDestroy()
        {
            if (_tabletScreen != null)
            {
                Destroy(_tabletScreen.gameObject);
            }
            if (_sharedCanvas != null)
            {
                _sharedCanvas.gameObject.SetActive(_canvasWasActive);
            }
            if (_sharedTablet != null)
            {
                _sharedTablet.enabled = _tabletWasEnabled;
            }
        }
    }
}
