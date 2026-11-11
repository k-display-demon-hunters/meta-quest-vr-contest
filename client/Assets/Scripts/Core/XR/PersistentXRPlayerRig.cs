using Tablet;
using UnityEngine;

namespace XR
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class PersistentXRPlayerRig : MonoBehaviour
    {
        private static PersistentXRPlayerRig _instance;
        [SerializeField] private OVRControllerHelper _controllerPrefab;
        [SerializeField] private Material _leftControllerMaterial;
        [SerializeField] private Material _rightControllerMaterial;
        [SerializeField] private GameObject _metaRuntime;
        [SerializeField] private Transform _leftGripVisual;
        [SerializeField] private Transform _rightGripVisual;
        [SerializeField] private TabletManager _tablet;
        private GameObject _leftControllerModel;
        private GameObject _rightControllerModel;

        public bool ControllerModelsVisible => _leftControllerModel.activeSelf;
        public bool TabletVisible => _tablet.IsAvailable;

        private void Start()
        {
            _leftControllerModel = CreateControllerVisual(_leftGripVisual, true).gameObject;
            _rightControllerModel = CreateControllerVisual(_rightGripVisual, false).gameObject;
        }

        public void SetControllerModelsVisible(bool visible)
        {
            _leftControllerModel.SetActive(visible);
            _rightControllerModel.SetActive(visible);
        }

        public void SetTabletVisible(bool visible)
        {
            _tablet.SetAvailable(visible);
        }

        public OVRControllerHelper CreateControllerVisual(Transform parent, bool left)
        {
            OVRControllerHelper visual = Instantiate(_controllerPrefab, parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.m_controller = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            GameObject touchPlusModel = left
                ? visual.m_modelMetaTouchPlusLeftController
                : visual.m_modelMetaTouchPlusRightController;
            string sourceMaterialName = left
                ? "MetaQuestTouchPlusLeft_Material"
                : "MetaQuestTouchPlusRight_Material";
            Material controllerMaterial = left ? _leftControllerMaterial : _rightControllerMaterial;
            foreach (Renderer renderer in touchPlusModel.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // Replace only the SDK prefab's FBX body material; retain battery materials.
                    if (materials[i].name == sourceMaterialName)
                        materials[i] = controllerMaterial;
                }
                renderer.sharedMaterials = materials;
            }
#if UNITY_EDITOR
            if (!UnityEngine.XR.XRSettings.isDeviceActive)
            {
                visual.enabled = false;
                // The keyboard simulator provides Unity XR input without an OVR device.
                foreach (Transform model in visual.transform)
                    model.gameObject.SetActive(model.gameObject == touchPlusModel);
            }
#endif
            return visual;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            // Reset when entering Play mode even when domain reload is disabled.
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Disable immediately so duplicate cameras and inputs never become active.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            _metaRuntime.SetActive(true);
            if (GetComponent<XRPlayerInputController>() == null)
            {
                gameObject.AddComponent<XRPlayerInputController>();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
