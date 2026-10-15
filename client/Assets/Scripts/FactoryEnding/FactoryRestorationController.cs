using System.Collections;
using UnityEngine;

namespace FactoryEnding
{
    public sealed class FactoryRestorationController : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField, Min(0.1f)] private float _duration = 5f;
        [SerializeField] private Renderer[] _factorySurfaces;
        [SerializeField] private Renderer[] _landscapeSurfaces;
        [SerializeField] private Transform[] _rotors;
        [SerializeField] private Light[] _factoryLights;
        [SerializeField] private GameObject _debris;
        [SerializeField] private GameObject _restoredDetails;
        [SerializeField] private Color _damagedColor = new Color(0.18f, 0.15f, 0.19f);
        [SerializeField] private Color _factoryColor = new Color(0.14f, 0.62f, 0.65f);
        [SerializeField] private Color _landscapeColor = new Color(0.22f, 0.56f, 0.3f);

        private MaterialPropertyBlock _properties;
        private Quaternion[] _initialRotations;
        private Vector3 _debrisScale;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            _initialRotations = new Quaternion[_rotors.Length];
            for (int index = 0; index < _rotors.Length; index++)
            {
                _initialRotations[index] = _rotors[index].localRotation;
            }
            _debrisScale = _debris.transform.localScale;
        }

        public IEnumerator RestoreFactory()
        {
            _debris.SetActive(true);
            _restoredDetails.SetActive(false);
            ApplyRestoration(0f);
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _duration));
                ApplyRestoration(progress);
                yield return null;
            }
            ApplyRestoration(1f);
            _debris.SetActive(false);
            _restoredDetails.SetActive(true);
        }

        private void ApplyRestoration(float progress)
        {
            Paint(_factorySurfaces, Color.Lerp(_damagedColor, _factoryColor, progress), progress * 0.08f);
            Paint(_landscapeSurfaces, Color.Lerp(_damagedColor, _landscapeColor, progress), 0f);
            for (int index = 0; index < _factoryLights.Length; index++)
            {
                _factoryLights[index].intensity = Mathf.Lerp(0.05f, 1.4f, progress);
            }
            for (int index = 0; index < _rotors.Length; index++)
            {
                _rotors[index].localRotation = _initialRotations[index] * Quaternion.Euler(0f, 0f, 180f * progress);
            }
            _debris.transform.localScale = _debrisScale * (1f - progress);
        }

        private void Paint(Renderer[] renderers, Color color, float emission)
        {
            foreach (Renderer target in renderers)
            {
                target.GetPropertyBlock(_properties);
                _properties.SetColor(BaseColor, color);
                _properties.SetColor(EmissionColor, color * emission);
                target.SetPropertyBlock(_properties);
            }
        }
    }
}
