using UnityEngine;

namespace KdyIntro
{
    public enum KdyIntroAction
    {
        Continue,
        PreviousTab,
        NextTab,
        RestartStory,
        EnterCnn,
        RestartIntro
    }

    public sealed class KdyActionController : MonoBehaviour
    {
        [SerializeField] private KdyIntroAction _action;
        [SerializeField] private Renderer _surface;
        private KdyIntroManager _manager;
        private MaterialPropertyBlock _properties;
        private Color _baseColor;
        private Color _emission;
        private bool _hovered;

        public KdyIntroManager Owner => _manager;

        public void Initialize(KdyIntroManager manager)
        {
            _manager = manager;
            if (_surface != null)
            {
                _properties = new MaterialPropertyBlock();
                _baseColor = _surface.sharedMaterial.GetColor("_BaseColor");
                _emission = _surface.sharedMaterial.GetColor("_EmissionColor");
            }
        }

        public void Press()
        {
            if (_manager != null)
            {
                _manager.Activate(_action);
            }
        }

        public void SetHover(bool hovered)
        {
            if (_hovered == hovered || _surface == null)
            {
                return;
            }
            _hovered = hovered;
            _properties.SetColor("_BaseColor", hovered ? Color.Lerp(_baseColor, Color.white, .4f) : _baseColor);
            _properties.SetColor("_EmissionColor", hovered ? _emission * 1.5f : _emission);
            _surface.SetPropertyBlock(_properties);
        }
    }
}
