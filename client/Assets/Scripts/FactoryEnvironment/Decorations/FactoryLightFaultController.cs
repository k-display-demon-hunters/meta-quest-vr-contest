using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryLightFaultController : MonoBehaviour
    {
        private const float SecondDischarge = 0.23f;
        private const float FaultDuration = 0.42f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly AnimationCurve Flicker = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(FaultDuration * 0.5f, 0.8f),
            new Keyframe(FaultDuration, 1f));

        [SerializeField] private Light _light;
        [SerializeField] private Renderer _diffuser;
        [SerializeField] private int _diffuserMaterialIndex;
        [SerializeField, Min(1f)] private float _interval = 18f;
        [SerializeField] private float _phaseDelay;
        [SerializeField] private AudioSource _crackle;
        [SerializeField] private AudioClip _crackleClip;
        [SerializeField] private Material _sparkMaterial;
        [SerializeField, Min(2)] private int _sparkCount = 18;
        [SerializeField] private Vector3 _sparkOffset;

        private MaterialPropertyBlock _diffuserProperties;
        private Color _diffuserColor;
        private float _lightIntensity;
        private ParticleSystem _sparks;
        private double _nextFault;
        private double _faultStarted;
        private bool _faultActive;
        private bool _secondDischargePending;

        private void Awake()
        {
            _lightIntensity = _light.intensity;
            _diffuserProperties = new MaterialPropertyBlock();
            _diffuser.GetPropertyBlock(_diffuserProperties, _diffuserMaterialIndex);
            _diffuserColor = _diffuser.sharedMaterials[_diffuserMaterialIndex].GetColor(BaseColor);

            if (_sparkMaterial != null)
            {
                _sparks = CreateSparks();
            }
        }

        private void OnEnable()
        {
            _nextFault = Time.timeAsDouble + _interval + _phaseDelay;
            SetBrightness(1f);
        }

        private void OnDisable()
        {
            _faultActive = false;
            SetBrightness(1f);
            _crackle.Stop();
            if (_sparks != null)
            {
                _sparks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Update()
        {
            double now = Time.timeAsDouble;
            if (now >= _nextFault)
            {
                _faultStarted = now;
                _nextFault += _interval * (System.Math.Floor((now - _nextFault) / _interval) + 1);
                _faultActive = true;
                _secondDischargePending = _sparks != null;
                _crackle.PlayOneShot(_crackleClip);
                if (_secondDischargePending)
                {
                    _sparks.Play(false);
                    _sparks.Emit(_sparkCount);
                }
            }

            if (!_faultActive) { return; }

            float elapsed = (float)(now - _faultStarted);
            if (elapsed >= FaultDuration)
            {
                _faultActive = false;
                SetBrightness(1f);
                return;
            }

            if (_secondDischargePending && elapsed >= SecondDischarge)
            {
                _secondDischargePending = false;
                _sparks.Emit(_sparkCount / 2);
            }

            SetBrightness(Flicker.Evaluate(elapsed));
        }

        private void SetBrightness(float brightness)
        {
            _light.intensity = _lightIntensity * brightness;
            Color diffuserColor = _diffuserColor * brightness;
            diffuserColor.a = _diffuserColor.a;
            _diffuserProperties.SetColor(BaseColor, diffuserColor);
            _diffuser.SetPropertyBlock(_diffuserProperties, _diffuserMaterialIndex);
        }

        private ParticleSystem CreateSparks()
        {
            GameObject effect = new GameObject("Electrical sparks");
            effect.SetActive(false);
            effect.transform.SetParent(transform, false);
            effect.transform.localPosition = _sparkOffset;
            effect.transform.localRotation = Quaternion.Euler(82f, 0f, 0f);

            ParticleSystem sparks = effect.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.75f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = _sparkCount + _sparkCount / 2;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.85f, 3.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.018f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.48f, 0.12f), new Color(1f, 0.94f, 0.76f));
            main.gravityModifier = 0.95f;

            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 48f;
            shape.radius = 0.025f;

            Gradient cooling = new Gradient();
            cooling.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.36f, 0.07f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.28f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = sparks.colorOverLifetime;
            color.enabled = true;
            color.color = cooling;

            ParticleSystemRenderer sparkRenderer = effect.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.sharedMaterial = _sparkMaterial;
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.lengthScale = 1.6f;
            sparkRenderer.velocityScale = 0.04f;
            effect.SetActive(true);
            return sparks;
        }
    }
}
