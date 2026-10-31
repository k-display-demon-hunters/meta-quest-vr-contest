using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace OpeningEnding.Ending
{
    [DisallowMultipleComponent]
    public sealed class FactoryRestorationController : MonoBehaviour
    {
        public const float FactoryRepairDuration = 12f;
        public const float CelebrationDuration = 10f;
        private const int SparkCount = 32;
        private const int RingSegments = 64;
        private static readonly int DuskId = Shader.PropertyToID("_Dusk");

        [SerializeField] private FactoryVillageController _village;
        [SerializeField] private Material _effectMaterial;
        [SerializeField] private Material _skyMaterial;
        [SerializeField] private Light _sun;

        private FactoryVillageDisplayController[] _displays;
        private FactoryVillageDisplayController _focusedDisplay;
        private Transform _focusedFrame;
        private Vector3 _originalFramePosition;
        private Quaternion _originalFrameRotation;
        private Vector3 _originalFrameScale;
        private readonly Vector3 _previewFramePosition = new Vector3(0f, 3.2f, 6f);
        private readonly Vector3 _previewFrameScale = Vector3.one * 1.4f;
        private bool _hasUpscalingPreview;
        private float _ambientTime;
        private float _celebrationStartTime;
        private int _ambientFrame;
        private Material _skyInstance;
        private Material _glowInstance;
        private Mesh _sparkMesh;
        private GameObject _effectsRoot;
        private Transform[] _sparks;
        private LineRenderer _coreRing;
        private LineRenderer _powerWave;
        private Camera _camera;
        private CameraClearFlags _previousClearFlags;
        private Material _previousSky;
        private Color _previousAmbient;
        private AmbientMode _previousAmbientMode;
        private bool _hasEnvironmentSnapshot;

        private void Awake()
        {
            _skyInstance = new Material(_skyMaterial) { name = "Ending village sky" };
            _glowInstance = new Material(_effectMaterial) { name = "Restoration glow" };
            _glowInstance.EnableKeyword("_EMISSION");
            _glowInstance.SetColor("_BaseColor", new Color(0.55f, 1f, 0.86f));
            _glowInstance.SetColor("_EmissionColor", new Color(0.7f, 2f, 1.45f));
            _displays = _village.GetComponentsInChildren<FactoryVillageDisplayController>(true);
            _focusedDisplay = _displays[0];
            _focusedFrame = _focusedDisplay.transform.parent;
            _originalFramePosition = _focusedFrame.localPosition;
            _originalFrameRotation = _focusedFrame.localRotation;
            _originalFrameScale = _focusedFrame.localScale;
            _effectsRoot = new GameObject("Village restoration light");
            _effectsRoot.transform.SetParent(transform, false);
            _coreRing = CreateRing("Core gathering ring");
            _powerWave = CreateRing("Restored power wave");
            CreateSparks();
            _effectsRoot.SetActive(false);
        }

        public void BindCamera(Camera camera)
        {
            if (_camera == camera) return;
            if (_camera != null) _camera.clearFlags = _previousClearFlags;
            _camera = camera;
            if (_camera == null) return;
            _previousClearFlags = _camera.clearFlags;
            _camera.clearFlags = CameraClearFlags.Skybox;
        }

        public void BeginFactoryRepair()
        {
            PrepareEnvironment();
            _effectsRoot.SetActive(true);
            _village.BeginEndingRepair();
            _ambientTime = 0f;
            _ambientFrame = Time.frameCount;
            EvaluateFactoryRepair(0f);
        }

        public void EvaluateFactoryRepair(float elapsed)
        {
            float time = Mathf.Clamp(elapsed, 0f, FactoryRepairDuration);
            _ambientTime = time;
            _ambientFrame = Time.frameCount;
            _village.EvaluateEndingWaiting(time);
            foreach (FactoryVillageDisplayController display in _displays) display.EvaluateUpscaling(0f);
            SetDaylight(Smooth(2f, FactoryRepairDuration, time));
            AnimateLight(time);
        }

        public void PrepareUpscaling()
        {
            _effectsRoot.SetActive(false);
            _hasUpscalingPreview = true;
            // The repaired factory remains running while the existing display moves forward.
            EvaluateFocus(0f);
        }

        public void EvaluateFocus(float progress)
        {
            AdvanceAmbient();
            _village.EvaluateEndingWaiting(_ambientTime);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            _focusedFrame.localPosition = Vector3.Lerp(_originalFramePosition, _previewFramePosition, eased);
            _focusedFrame.localRotation = Quaternion.Slerp(_originalFrameRotation, Quaternion.identity, eased);
            _focusedFrame.localScale = Vector3.Lerp(_originalFrameScale, _previewFrameScale, eased);
        }

        public void EvaluateProcessing(float elapsed)
        {
            // Phase-local clocks can restart for explanation/reveal; the village clock cannot.
            AdvanceAmbient();
            _village.EvaluateEndingWaiting(_ambientTime);
        }

        public void EvaluateUpscaling(float revealProgress)
        {
            AdvanceAmbient();
            _village.EvaluateEndingWaiting(_ambientTime);
            _focusedDisplay.EvaluateUpscaling(revealProgress);
        }

        public void BeginCelebration()
        {
            _celebrationStartTime = _ambientTime;
            _effectsRoot.SetActive(true);
            EvaluateCelebration(0f);
        }

        public void EvaluateCelebration(float elapsed)
        {
            AdvanceAmbient();
            _village.EvaluateEndingCelebration(_ambientTime, elapsed);
            float returnProgress = Smooth(0f, 3f, elapsed);
            _focusedFrame.localPosition = Vector3.Lerp(_previewFramePosition, _originalFramePosition, returnProgress);
            _focusedFrame.localRotation = Quaternion.Slerp(Quaternion.identity, _originalFrameRotation, returnProgress);
            _focusedFrame.localScale = Vector3.Lerp(_previewFrameScale, _originalFrameScale, returnProgress);
            UpdateCelebrationDisplays(elapsed);
            AnimateLight(13f + elapsed);
        }

        public void BeginResidentThanks()
        {
            _effectsRoot.SetActive(false);
            _village.BeginResidentThanks(_ambientTime - _celebrationStartTime, _ambientTime);
        }

        public void EvaluateResidentThanks(float elapsed, bool departing)
        {
            AdvanceAmbient();
            float celebrationTime = _ambientTime - _celebrationStartTime;
            _village.EvaluateEndingCelebration(_ambientTime, celebrationTime);
            UpdateCelebrationDisplays(celebrationTime);
            _village.EvaluateResidentThanks(elapsed, departing, _camera.transform.position, _ambientTime);
        }

        public void EvaluatePanorama(float elapsed)
        {
            AdvanceAmbient();
            float celebrationTime = _ambientTime - _celebrationStartTime;
            _village.EvaluateEndingCelebration(_ambientTime, celebrationTime);
            UpdateCelebrationDisplays(celebrationTime);
            _village.EvaluateResidentAside(_ambientTime, _camera.transform.position);
        }

        private void UpdateCelebrationDisplays(float elapsed)
        {
            for (int i = 0; i < _displays.Length; i++)
            {
                FactoryVillageDisplayController display = _displays[i];
                if (display == _focusedDisplay)
                {
                    display.EvaluateUpscaled(elapsed);
                    continue;
                }
                float finishedAt = 3.5f + i * 1.25f;
                if (elapsed < finishedAt) display.EvaluateUpscaling(Smooth(finishedAt - 2.5f, finishedAt, elapsed));
                else display.EvaluateUpscaled(elapsed - finishedAt);
            }
        }

        private void AdvanceAmbient()
        {
            // Some phases update both environment and reveal in one frame.
            if (_ambientFrame == Time.frameCount) return;
            _ambientFrame = Time.frameCount;
            _ambientTime += Time.deltaTime;
        }

        private void PrepareEnvironment()
        {
            if (!_hasEnvironmentSnapshot)
            {
                _previousSky = RenderSettings.skybox;
                _previousAmbient = RenderSettings.ambientLight;
                _previousAmbientMode = RenderSettings.ambientMode;
                _hasEnvironmentSnapshot = true;
            }
            RenderSettings.skybox = _skyInstance;
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        private void SetDaylight(float daylight)
        {
            _skyInstance.SetFloat(DuskId, 1f - daylight);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.19f, 0.21f, 0.29f), new Color(0.76f, 0.82f, 0.84f), daylight);
            if (_sun != null)
            {
                _sun.intensity = Mathf.Lerp(0.32f, 1.25f, daylight);
                _sun.color = Color.Lerp(new Color(0.51f, 0.57f, 0.72f), new Color(1f, 0.95f, 0.87f), daylight);
            }
        }

        private void AnimateLight(float time)
        {
            Vector3 core = _village.CorePosition;
            float ringEnvelope = Smooth(1.1f, 2.5f, time) * (1f - Smooth(6f, 8f, time));
            DrawRing(_coreRing, core, Mathf.Lerp(3f, 0.72f, Smooth(1.5f, 5f, time)),
                0.065f * ringEnvelope, time * 0.6f, false);
            float wave = Smooth(6.4f, 13.2f, time);
            Vector3 ground = new Vector3(core.x, _village.transform.position.y + 0.13f, core.z);
            DrawRing(_powerWave, ground, Mathf.Lerp(0.4f, 17f, wave),
                0.11f * Smooth(6.3f, 7.1f, time) * (1f - Smooth(11f, 13.2f, time)), 0f, true);

            for (int i = 0; i < _sparks.Length; i++)
            {
                Transform spark = _sparks[i];
                float phase = i * 2.399963f;
                if (time < 7f)
                {
                    float progress = Smooth(0.8f + i * 0.035f, 4.8f + i * 0.035f, time);
                    float radius = Mathf.Lerp(4.4f, 0.15f, progress);
                    float angle = phase + time * 0.9f;
                    spark.position = core + new Vector3(Mathf.Cos(angle) * radius,
                        Mathf.Sin(phase * 1.7f) * radius * 0.65f, Mathf.Sin(angle) * radius * 0.65f);
                    float size = 0.07f * Smooth(0.1f + i * 0.02f, 1.4f + i * 0.02f, time)
                        * (1f - Smooth(4.7f + i * 0.035f, 5.8f + i * 0.035f, time));
                    spark.localScale = Vector3.one * size;
                    spark.gameObject.SetActive(size > 0.001f);
                }
                else
                {
                    // A gentle release of light above the park; no camera shake or flashes.
                    float rise = Smooth(11.5f + i * 0.09f, 19.5f + i * 0.05f, time);
                    float radius = 2f + i % 7 * 1.15f;
                    Vector3 local = new Vector3(Mathf.Cos(phase) * radius,
                        1.2f + rise * 5.5f, 10f + Mathf.Sin(phase) * radius * 0.7f);
                    local.x += Mathf.Sin(time * 0.7f + phase) * 0.25f;
                    spark.position = _village.transform.TransformPoint(local);
                    float size = 0.06f * Smooth(11.5f + i * 0.09f, 13f + i * 0.09f, time)
                        * (1f - Smooth(18.5f, 22f, time));
                    spark.localScale = new Vector3(size, size * 1.5f, size);
                    spark.gameObject.SetActive(size > 0.001f);
                }
                spark.localRotation = Quaternion.Euler(time * 24f + i * 17f, time * 31f, i * 29f);
            }
        }

        private LineRenderer CreateRing(string objectName)
        {
            LineRenderer ring = new GameObject(objectName, typeof(LineRenderer)).GetComponent<LineRenderer>();
            ring.transform.SetParent(_effectsRoot.transform, false);
            ring.sharedMaterial = _glowInstance;
            ring.positionCount = RingSegments;
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.numCornerVertices = 2;
            ring.enabled = false;
            return ring;
        }

        private static void DrawRing(LineRenderer ring, Vector3 center, float radius, float width,
            float rotation, bool horizontal)
        {
            ring.enabled = width > 0.001f;
            if (!ring.enabled) return;
            ring.startWidth = ring.endWidth = width;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments + rotation;
                Vector3 direction = horizontal
                    ? new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                    : new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                ring.SetPosition(i, center + direction * radius);
            }
        }

        private void CreateSparks()
        {
            _sparkMesh = new Mesh { name = "Restoration light mesh" };
            _sparkMesh.vertices = new[] { Vector3.up, Vector3.right, Vector3.forward, Vector3.left, Vector3.back, Vector3.down };
            _sparkMesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
            _sparkMesh.RecalculateNormals();
            _sparkMesh.RecalculateBounds();
            _sparks = new Transform[SparkCount];
            for (int i = 0; i < SparkCount; i++)
            {
                GameObject spark = new GameObject("Restored light " + i, typeof(MeshFilter), typeof(MeshRenderer));
                spark.transform.SetParent(_effectsRoot.transform, false);
                spark.GetComponent<MeshFilter>().sharedMesh = _sparkMesh;
                MeshRenderer renderer = spark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _glowInstance;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                spark.SetActive(false);
                _sparks[i] = spark.transform;
            }
        }

        private static float Smooth(float from, float to, float time)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, time));

        private void OnDisable()
        {
            if (_hasUpscalingPreview && _focusedFrame != null)
            {
                _focusedFrame.localPosition = _originalFramePosition;
                _focusedFrame.localRotation = _originalFrameRotation;
                _focusedFrame.localScale = _originalFrameScale;
            }
            if (_camera != null)
            {
                _camera.clearFlags = _previousClearFlags;
                _camera = null;
            }
            if (_hasEnvironmentSnapshot && SceneManager.GetActiveScene() == gameObject.scene)
            {
                RenderSettings.skybox = _previousSky;
                RenderSettings.ambientLight = _previousAmbient;
                RenderSettings.ambientMode = _previousAmbientMode;
            }
        }

        private void OnDestroy()
        {
            if (_skyInstance != null) Destroy(_skyInstance);
            if (_glowInstance != null) Destroy(_glowInstance);
            if (_sparkMesh != null) Destroy(_sparkMesh);
        }
    }
}
