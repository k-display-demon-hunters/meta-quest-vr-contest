using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace OpeningEnding.Intro
{
    public sealed class FactoryIntroCinematicController : MonoBehaviour
    {
        public const float Duration = 18f;
        private const float ImpactTime = 8.2f;
        private const int RingSegments = 80;
        private const int SparkCount = 32;
        private static readonly int DuskId = Shader.PropertyToID("_Dusk");

        [SerializeField] private FactoryVillageController _village;
        [SerializeField] private FactoryWraithController _wraith;
        [SerializeField] private Material _effectMaterial;
        [SerializeField] private Material _skyMaterial;
        [SerializeField] private Light _sun;
        private Core.AudioManager _audio;
        [SerializeField] private AudioClip _soundtrack;
        [SerializeField] private AudioClip _titleMusic;
        [SerializeField] private AudioClip _attackSound;
        [SerializeField] private AudioClip _doorSound;

        private FactoryVillageDisplayController[] _displays;
        private Vector3[] _displayPositions;
        private Quaternion[] _displayRotations;
        private Vector3[] _displayScales;
        private Vector3 _villagePosition;
        private Vector3 _villageScale;
        private LineRenderer _portal;
        private LineRenderer _portalInner;
        private LineRenderer _shockwave;
        private Transform[] _sparks;
        private Mesh _sparkMesh;
        private GameObject _effectsRoot;
        private Material _skyInstance;
        private Material _previousSky;
        private Color _previousAmbient;
        private AmbientMode _previousAmbientMode;
        private bool _hasEnvironmentSnapshot;
        private Camera _camera;
        private CameraClearFlags _previousClearFlags;
        private float _lastCueTime = -1f;
        private float _hapticEndTime;

        public bool IsReady => _village != null && _wraith != null &&
                               _effectMaterial != null && _skyMaterial != null;

        private void Awake()
        {
            if (!IsReady)
            {
                Debug.LogError("[FactoryIntro] 마을·악령·오프닝 재질 연결을 확인해 주세요.", this);
                return;
            }
            _displays = _village.GetComponentsInChildren<FactoryVillageDisplayController>(true);
            _villagePosition = _village.transform.localPosition;
            _villageScale = _village.transform.localScale;
            _displayPositions = new Vector3[_displays.Length];
            _displayRotations = new Quaternion[_displays.Length];
            _displayScales = new Vector3[_displays.Length];
            for (int i = 0; i < _displays.Length; i++)
            {
                Transform housing = _displays[i].transform.parent;
                _displayPositions[i] = housing.localPosition;
                _displayRotations[i] = housing.localRotation;
                _displayScales[i] = housing.localScale;
            }
            _effectsRoot = new GameObject("Opening portal and impact");
            _effectsRoot.transform.SetParent(transform, false);
            _portal = CreateRing("Spirit rift");
            _portalInner = CreateRing("Rift inner orbit");
            _shockwave = CreateRing("Factory impact wave");
            CreateSparks();
            _skyInstance = new Material(_skyMaterial) { name = "Opening sky instance" };
        }

        public void BindAudio(Core.AudioManager audio) => _audio = audio;

        public void BindCamera(Camera camera)
        {
            _camera = camera;
            _previousClearFlags = camera.clearFlags;
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        public void ResetOpening()
        {
            if (!IsReady)
            {
                return;
            }
            CaptureEnvironment();
            _village.ResetVillage();
            foreach (FactoryVillageDisplayController display in _displays)
            {
                display.ResetDisplay();
            }
            _wraith.gameObject.SetActive(false);
            _effectsRoot.SetActive(false);
            _lastCueTime = -1f;
            StopHaptics();
            if (_audio != null)
            {
                _audio.StopEffects();
            }
            SetAtmosphere(0f);
        }

        public void EvaluateIdle(float elapsed)
        {
            _village.EvaluateIdle(elapsed);
            foreach (FactoryVillageDisplayController display in _displays)
            {
                display.EvaluateIdle(elapsed);
            }
        }

        public void ShowTitleComposition()
        {
            // One frozen village frame: factory at centre, three displays around it,
            // and the existing wraith above its right edge. No idle/story time advances.
            ResetOpening();
            _village.transform.localScale = _villageScale * 1.5f;
            _village.transform.localPosition = _villagePosition + new Vector3(0f, -2f, -13f);
            Vector3[] positions = {
                new Vector3(-5.6f, 4.4f, 10f),
                new Vector3(-3.6f, 1.8f, 8.5f),
                new Vector3(5.2f, 2.5f, 10f)
            };
            for (int i = 0; i < _displays.Length; i++)
            {
                Transform housing = _displays[i].transform.parent;
                housing.SetPositionAndRotation(
                    positions[i], _village.transform.rotation);
                housing.localScale = Vector3.one * 0.48f;
                _displays[i].EvaluateIdle(3f);
            }
            _wraith.gameObject.SetActive(true);
            _wraith.transform.SetPositionAndRotation(
                new Vector3(4.4f, 5.1f, 11f),
                _village.transform.rotation * Quaternion.Euler(0f, -28f, 18f));
            _wraith.transform.localScale = Vector3.one * 1.2f;
            _wraith.SetCinematicFrame(3.9f, 1f, _village.CorePosition, 1f);
            _effectsRoot.SetActive(true);
            DrawRing(_portal, new Vector3(4.4f, 5.1f, 11.8f), 2.2f, 0.12f, 0.4f, false, 0.16f);
            DrawRing(_portalInner, new Vector3(4.4f, 5.1f, 11.7f), 1.85f, 0.045f, 0.9f, false, 0.12f);
            AnimateImpact(ImpactTime + 0.35f);
            DrawRing(_shockwave, _village.CorePosition, 1.2f, 0.16f, 0f, false, 0.18f);
            SetAtmosphere(0.7f);
            _audio.PlayMusic(_titleMusic, true, 0.3f);
        }

        public void RestoreVillageComposition()
        {
            _village.transform.localPosition = _villagePosition;
            _village.transform.localScale = _villageScale;
            for (int i = 0; i < _displays.Length; i++)
            {
                Transform housing = _displays[i].transform.parent;
                housing.localPosition = _displayPositions[i];
                housing.localRotation = _displayRotations[i];
                housing.localScale = _displayScales[i];
            }
            ResetOpening();
        }

        public void BeginOpening(float ambientTime)
        {
            _lastCueTime = -1f;
            // Continue the visible village/broadcast phase instead of jumping back when Start is pressed.
            _village.BeginStory(ambientTime);
            foreach (FactoryVillageDisplayController display in _displays)
            {
                display.BeginStory(ambientTime);
            }
            _effectsRoot.SetActive(true);
            Evaluate(0f);
            _audio.PlayMusic(_soundtrack, false, 0.7f, 1.5f);
        }

        public void Evaluate(float elapsed)
        {
            _village.Evaluate(elapsed);
            foreach (FactoryVillageDisplayController display in _displays)
            {
                display.Evaluate(elapsed);
            }
            SetAtmosphere(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 8.8f, elapsed)));
            AnimateSpirit(elapsed);
            AnimateImpact(elapsed);
            EvaluateSoundAndHaptics(elapsed);
        }

        public void PlayDoorSound()
        {
            _audio.PlayEffect(_doorSound, 0.85f);
        }

        private void EvaluateSoundAndHaptics(float elapsed)
        {
            if (_lastCueTime < 4.3f && elapsed >= 4.3f)
            {
                _audio.PlayEffect(_attackSound, 0.8f);
                PulseHands(0.25f, 0.16f);
            }
            if (_lastCueTime < 7.2f && elapsed >= 7.2f)
            {
                PulseHands(0.18f, 0.25f);
            }
            if (_lastCueTime < ImpactTime && elapsed >= ImpactTime)
            {
                PulseHands(0.55f, 0.3f);
            }
            if (_lastCueTime < 10.5f && elapsed >= 10.5f)
            {
                PulseHands(0.2f, 0.12f);
            }
            _lastCueTime = elapsed;
        }

        private void PulseHands(float amplitude, float duration)
        {
            SendPulse(XRNode.LeftHand, amplitude, duration);
            SendPulse(XRNode.RightHand, amplitude, duration);
            _hapticEndTime = Time.unscaledTime + duration;
        }

        private static void SendPulse(XRNode hand, float amplitude, float duration)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            // Controllers can disconnect, and some devices do not support impulse feedback.
            if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        private void StopHaptics()
        {
            if (Time.unscaledTime < _hapticEndTime)
            {
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).StopHaptics();
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand).StopHaptics();
            }
            _hapticEndTime = 0f;
        }

        private void AnimateSpirit(float elapsed)
        {
            float arrival = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.3f, 5.5f, elapsed));
            float departure = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10.1f, 11.5f, elapsed));
            float visibility = arrival * departure;
            Vector3 portalCenter = _village.transform.TransformPoint(new Vector3(-4f, 7.4f, 16f));
            float portalVisibility = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 4.9f, elapsed)) *
                                     (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10.6f, 11.7f, elapsed)));
            DrawRing(_portal, portalCenter, 2.2f * portalVisibility, 0.09f * portalVisibility,
                elapsed * 0.65f, false, 0.13f);
            DrawRing(_portalInner, portalCenter, 1.7f * portalVisibility, 0.035f * portalVisibility,
                -elapsed * 0.9f, false, 0.09f);
            if (visibility <= 0f)
            {
                _wraith.gameObject.SetActive(false);
                return;
            }
            _wraith.gameObject.SetActive(true);
            float approach = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 7.1f, elapsed));
            Vector3 attackPosition = _village.transform.TransformPoint(new Vector3(-1.8f, 5.8f, 16f));
            Vector3 position = Vector3.Lerp(portalCenter, attackPosition, approach);
            position = Vector3.Lerp(position, portalCenter + Vector3.up * 1.4f,
                Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9.5f, 11.5f, elapsed)));
            _wraith.transform.position = position;
            _wraith.transform.rotation = _village.transform.rotation * Quaternion.Euler(0f, -12f, -8f * Mathf.Sin(elapsed));
            _wraith.transform.localScale = Vector3.one * 1.65f;
            float attack = elapsed >= 7.2f && elapsed < 8.95f
                ? Mathf.Lerp(0.25f, 1f, Mathf.InverseLerp(7.2f, ImpactTime, elapsed)) : 0f;
            _wraith.SetCinematicFrame(elapsed - 4.3f, visibility, _village.CorePosition, attack);
        }

        private void AnimateImpact(float elapsed)
        {
            float age = elapsed - ImpactTime;
            float wave = Mathf.Clamp01(age / 1.65f);
            Vector3 center = _village.CorePosition;
            DrawRing(_shockwave, center, 0.4f + wave * 13f,
                age >= 0f && age < 1.65f ? (1f - wave) * 0.22f : 0f, 0f, true, 0f);
            for (int i = 0; i < _sparks.Length; i++)
            {
                float sparkAge = age - i % 4 * 0.025f;
                bool visible = sparkAge >= 0f && sparkAge < 1.8f;
                Transform spark = _sparks[i];
                spark.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                float angle = i * 2.399963f;
                Vector3 velocity = new Vector3(Mathf.Cos(angle) * (2f + i % 4),
                    1.5f + i % 5 * 0.65f, Mathf.Sin(angle) * (2f + i % 3));
                spark.position = center + velocity * sparkAge + Vector3.down * (2.5f * sparkAge * sparkAge);
                spark.localRotation = Quaternion.Euler(i * 17f + sparkAge * 120f, sparkAge * 95f, i * 29f);
                spark.localScale = Vector3.one * ((0.05f + i % 3 * 0.035f) * (1f - sparkAge / 1.8f));
            }
        }

        private LineRenderer CreateRing(string objectName)
        {
            GameObject ring = new GameObject(objectName, typeof(LineRenderer));
            ring.transform.SetParent(_effectsRoot.transform, false);
            LineRenderer line = ring.GetComponent<LineRenderer>();
            line.sharedMaterial = _effectMaterial;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = RingSegments;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void DrawRing(LineRenderer ring, Vector3 center, float radius, float width,
            float rotation, bool horizontal, float distortion)
        {
            ring.enabled = width > 0.001f;
            if (!ring.enabled)
            {
                return;
            }
            ring.startWidth = width;
            ring.endWidth = width;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments + rotation;
                float distance = radius * (1f + Mathf.Sin(angle * 7f + rotation) * distortion);
                Vector3 offset = horizontal
                    ? new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                    : new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                ring.SetPosition(i, center + offset * distance);
            }
        }

        private void CreateSparks()
        {
            // A shared tiny octahedron avoids primitive colliders and extra materials.
            _sparkMesh = new Mesh { name = "Opening spark mesh" };
            _sparkMesh.vertices = new[] { Vector3.up, Vector3.right, Vector3.forward, Vector3.left, Vector3.back, Vector3.down };
            _sparkMesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
            _sparkMesh.RecalculateNormals();
            _sparkMesh.RecalculateBounds();
            _sparks = new Transform[SparkCount];
            for (int i = 0; i < SparkCount; i++)
            {
                GameObject spark = new GameObject("Impact fragment " + i, typeof(MeshFilter), typeof(MeshRenderer));
                spark.transform.SetParent(_effectsRoot.transform, false);
                spark.GetComponent<MeshFilter>().sharedMesh = _sparkMesh;
                MeshRenderer renderer = spark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _effectMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                spark.SetActive(false);
                _sparks[i] = spark.transform;
            }
        }

        private void CaptureEnvironment()
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

        private void SetAtmosphere(float dusk)
        {
            _skyInstance.SetFloat(DuskId, dusk);
            RenderSettings.ambientLight = Color.Lerp(new Color(0.76f, 0.82f, 0.84f), new Color(0.19f, 0.21f, 0.29f), dusk);
            if (_sun != null)
            {
                _sun.intensity = Mathf.Lerp(1.25f, 0.32f, dusk);
                _sun.color = Color.Lerp(new Color(1f, 0.95f, 0.87f), new Color(0.51f, 0.57f, 0.72f), dusk);
            }
        }

        private void OnDisable()
        {
            // The persistent rig may already be shutting down when Play or the app exits.
            if (_audio != null && _audio.isActiveAndEnabled)
            {
                _audio.StopMusic();
                _audio.StopEffects();
            }
            StopHaptics();
            // The shared XR camera survives this scene and needs its original clear mode back.
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
            if (_skyInstance != null)
            {
                Destroy(_skyInstance);
            }
            if (_sparkMesh != null)
            {
                Destroy(_sparkMesh);
            }
        }
    }
}
