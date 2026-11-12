using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNScreen
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class MagnifierPickupFX : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int LensFlash = Shader.PropertyToID("_Flash");
        private static readonly int LensSweep = Shader.PropertyToID("_Sweep");
        private const int SparkCount = 18;
        private const float BeamDelay = 0.14f;
        private const float FlashDuration = 0.32f;

        [Min(0f)] public float fadeDuration = 0.4f;
        public Color particleColor = new Color(0.39f, 0.82f, 1f, 1f);
        [Range(0f, 1f)] public float volume = 0.75f;
        public AudioClip pickupClip;

        [SerializeField] private MeshRenderer _beam;
        [SerializeField] private MeshRenderer _rim;
        [SerializeField] private MeshRenderer _lens;
        [SerializeField] private Material _sparkMaterial;
        [SerializeField] private ScreenLabUIController _ui;

        private XRGrabInteractable _grab;
        private ParticleSystem _sparks;
        private AudioSource _audio;
        private MaterialPropertyBlock _properties;
        private Color _beamColor;
        private Color _rimColor;
        private bool _hasPlayed;
        private bool _playing;
        private float _elapsed;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _properties = new MaterialPropertyBlock();
            _beamColor = _beam.sharedMaterial.GetColor(EmissionColor);
            _rimColor = _rim.sharedMaterial.GetColor(EmissionColor);
            CreateBurst();
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(PlayPickup);
            _ui.ResetRequested += RestorePedestal;
        }

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(PlayPickup);
            _ui.ResetRequested -= RestorePedestal;
        }

        private void OnDestroy()
        {
            if (_sparks != null) Destroy(_sparks.gameObject);
        }

        private void PlayPickup(SelectEnterEventArgs args)
        {
            if (_hasPlayed) return;

            _hasPlayed = true;
            _elapsed = 0f;
            _playing = true;
            SetLensFlash(1f, 0f);

            _sparks.transform.position = transform.position;
            ParticleSystem.MainModule main = _sparks.main;
            main.startColor = particleColor;
            _sparks.Play(false);
            _sparks.Emit(SparkCount);
            if (pickupClip != null) _audio.PlayOneShot(pickupClip, volume);
        }

        private void Update()
        {
            if (!_playing) return;

            _elapsed += Time.unscaledDeltaTime;
            float sweep = Mathf.Clamp01(_elapsed / FlashDuration);
            SetLensFlash(1f - Mathf.SmoothStep(0f, 1f, sweep), sweep);

            if (_beam.gameObject.activeSelf && _elapsed >= BeamDelay)
            {
                float fade = fadeDuration > 0f ? Mathf.Clamp01((_elapsed - BeamDelay) / fadeDuration) : 1f;
                ApplyGlow(1f - Mathf.SmoothStep(0f, 1f, fade));
                if (fade >= 1f) _beam.gameObject.SetActive(false);
            }

            _playing = sweep < 1f || _beam.gameObject.activeSelf;
        }

        private void RestorePedestal()
        {
            // Match the existing reset, which leaves the exercise alone while the filter is held.
            if (_grab.isSelected) return;

            _hasPlayed = false;
            _playing = false;
            SetLensFlash(0f, 0f);
            ApplyGlow(1f);
            _beam.gameObject.SetActive(true);
            _sparks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            _audio.Stop();
        }

        private void SetLensFlash(float strength, float sweep)
        {
            _lens.GetPropertyBlock(_properties);
            _properties.SetFloat(LensFlash, strength);
            _properties.SetFloat(LensSweep, sweep);
            _lens.SetPropertyBlock(_properties);
        }

        private void ApplyGlow(float remaining)
        {
            SetGlow(_beam, _beamColor, remaining);
            SetGlow(_rim, _rimColor, Mathf.Lerp(0.22f, 1f, remaining));
        }

        private void SetGlow(Renderer target, Color color, float strength)
        {
            target.GetPropertyBlock(_properties);
            color.a *= strength;
            _properties.SetColor(EmissionColor, color);
            target.SetPropertyBlock(_properties);
        }

        private void CreateBurst()
        {
            // Keep the burst where the grab happened while the magnifier moves away.
            GameObject effect = new GameObject("Magnifier pickup burst");
            effect.SetActive(false);
            effect.transform.SetParent(_beam.transform.parent, false);
            _sparks = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.007f, 0.014f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.maxParticles = SparkCount;

            ParticleSystem.EmissionModule emission = _sparks.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = _sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.06f;

            Gradient opacity = new Gradient();
            opacity.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colors = _sparks.colorOverLifetime;
            colors.enabled = true;
            colors.color = opacity;
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = _sparkMaterial;

            _audio = effect.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 1f;
            _audio.minDistance = 0.4f;
            _audio.maxDistance = 5f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            effect.SetActive(true);
        }
    }
}
