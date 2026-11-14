using System.Collections;
using System.Collections.Generic;
using Tablet;
using UnityEngine;
using UnityEngine.XR;

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
#if UNITY_ANDROID && !UNITY_EDITOR
        private const float FoveatedRenderingLevel = 0.5f;
        private const float TargetDisplayRefreshRate = 120f;
        private const float InitialRefreshRetrySeconds = 3f;
        private const float SecondRefreshRetrySeconds = 5f;
        private const float MaxRefreshRetrySeconds = 10f;
        private Coroutine _refreshRateMonitor;
        private bool _applicationPaused;
        private bool _applicationFocused = true;
        private bool _refreshRateRecheckRequested;
#endif

        public bool ControllerModelsVisible => _leftControllerModel.activeSelf;
        public bool TabletVisible => _tablet.IsAvailable;

        private void Start()
        {
            _leftControllerModel = CreateControllerVisual(_leftGripVisual, true).gameObject;
            _rightControllerModel = CreateControllerVisual(_rightGripVisual, false).gameObject;
#if UNITY_ANDROID && !UNITY_EDITOR
            ConfigureFoveatedRendering();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ConfigureFoveatedRendering()
        {
            List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (XRDisplaySubsystem display in displays)
            {
                // SRP foveation defaults to off; 0.5 selects Medium on Quest.
                display.foveatedRenderingLevel = FoveatedRenderingLevel;
            }
        }

        private void OnEnable()
        {
            _refreshRateMonitor = StartCoroutine(MonitorDisplayRefreshRate());
        }

        private void OnDisable()
        {
            if (_refreshRateMonitor != null)
            {
                StopCoroutine(_refreshRateMonitor);
                _refreshRateMonitor = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _applicationPaused = paused;
            _refreshRateRecheckRequested = true;
        }

        private void OnApplicationFocus(bool focused)
        {
            _applicationFocused = focused;
            _refreshRateRecheckRequested = true;
        }

        private IEnumerator MonitorDisplayRefreshRate()
        {
            WaitForSecondsRealtime pollInterval = new WaitForSecondsRealtime(1f);
            float nextRequestTime = 0f;
            float retrySeconds = InitialRefreshRetrySeconds;
            bool confirmed = false;
            bool powerSaving = false;

            while (true)
            {
                // Wait for foreground execution and let XR initialize before querying it.
                yield return pollInterval;
                if (_applicationPaused || !_applicationFocused)
                {
                    continue;
                }

                if (_refreshRateRecheckRequested)
                {
                    _refreshRateRecheckRequested = false;
                    nextRequestTime = 0f;
                    retrySeconds = InitialRefreshRetrySeconds;
                    confirmed = false;
                }

                bool currentPowerSaving = OVRPlugin.powerSaving;
                if (currentPowerSaving != powerSaving)
                {
                    powerSaving = currentPowerSaving;
                    Debug.Log($"[XR] Power saving: {powerSaving}. 120 Hz requests are deferred while active.", this);
                }

                float actualFrequency = OVRPlugin.systemDisplayFrequency;
                if (Mathf.Abs(actualFrequency - TargetDisplayRefreshRate) < 0.1f)
                {
                    if (!confirmed)
                    {
                        Debug.Log($"[XR] Confirmed display refresh rate: {actualFrequency:0.##} Hz.", this);
                    }
                    confirmed = true;
                    retrySeconds = InitialRefreshRetrySeconds;
                    // If the OS lowers the rate, allow it a cooldown before requesting again.
                    nextRequestTime = Time.realtimeSinceStartup + MaxRefreshRetrySeconds;
                    continue;
                }

                if (confirmed)
                {
                    string actualState = actualFrequency > 0f ? $"{actualFrequency:0.##} Hz" : "unavailable";
                    Debug.LogWarning($"[XR] Display refresh rate is no longer confirmed at 120 Hz: {actualState}.", this);
                    confirmed = false;
                    retrySeconds = MaxRefreshRetrySeconds;
                }

                // Zero means the runtime cannot currently report a refresh rate.
                if (actualFrequency <= 0f || powerSaving || Time.realtimeSinceStartup < nextRequestTime)
                {
                    continue;
                }

                OVRPlugin.systemDisplayFrequency = TargetDisplayRefreshRate;
                Debug.Log($"[XR] Requested display refresh rate: 120 Hz. Last queried: {actualFrequency:0.##} Hz. Retry in {retrySeconds:0} seconds if unconfirmed.", this);
                nextRequestTime = Time.realtimeSinceStartup + retrySeconds;
                retrySeconds = retrySeconds < SecondRefreshRetrySeconds
                    ? SecondRefreshRetrySeconds
                    : MaxRefreshRetrySeconds;
            }
        }
#endif

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
