using UnityEngine;

namespace KdyLearning
{
    public sealed class KdyLessonSurfaceController : MonoBehaviour
    {
        [SerializeField] private KdyLessonAction _action;
        [SerializeField] private int _argument;
        [SerializeField] private Renderer _surface;

        private KdyLessonManager _manager;
        private MaterialPropertyBlock _properties;
        private Color _baseColor;
        private Color _emissionColor;
        private bool _hasBaseColor;
        private bool _hasEmissionColor;
        private bool _hovered;
        private bool _available = true;

        public KdyLessonManager Owner => _manager;

        public void Initialize(KdyLessonManager manager)
        {
            _manager = manager;
            if (_surface == null || _surface.sharedMaterial == null)
            {
                return;
            }

            Material material = _surface.sharedMaterial;
            _properties = new MaterialPropertyBlock();
            _surface.GetPropertyBlock(_properties);
            _hasBaseColor = material.HasProperty("_BaseColor");
            _hasEmissionColor = material.HasProperty("_EmissionColor");
            if (_hasBaseColor)
            {
                _baseColor = material.GetColor("_BaseColor");
            }
            if (_hasEmissionColor)
            {
                _emissionColor = material.GetColor("_EmissionColor");
            }

            RefreshAppearance();
        }

        public void Press()
        {
            if (isActiveAndEnabled && _available && _manager != null)
            {
                _manager.Activate(_action, _argument);
            }
        }

        public void SetHover(bool hovered)
        {
            if (_hovered == hovered)
            {
                return;
            }

            _hovered = hovered;
            RefreshAppearance();
        }

        public void SetAvailable(bool available)
        {
            if (_available == available)
            {
                return;
            }

            _available = available;
            RefreshAppearance();
        }

        private void RefreshAppearance()
        {
            if (_surface == null || _properties == null)
            {
                return;
            }

            if (_hasBaseColor)
            {
                Color color = _available ? _baseColor : Color.Lerp(_baseColor, Color.black, .65f);
                if (_available && _hovered)
                {
                    color = Color.Lerp(color, Color.white, .35f);
                }
                _properties.SetColor("_BaseColor", color);
            }
            if (_hasEmissionColor)
            {
                float brightness = !_available ? .15f : _hovered ? 1.6f : 1f;
                _properties.SetColor("_EmissionColor", _emissionColor * brightness);
            }

            _surface.SetPropertyBlock(_properties);
        }

        private void OnDisable()
        {
            _hovered = false;
            RefreshAppearance();
        }
    }
}
