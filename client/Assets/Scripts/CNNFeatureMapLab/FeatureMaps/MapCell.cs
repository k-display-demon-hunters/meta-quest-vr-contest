using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    [RequireComponent(typeof(Renderer))]
    public sealed class MapCell : MonoBehaviour
    {
        private static readonly Color[] Palette = { new Color(.18f, .65f, 1f), new Color(.18f, .9f, .48f),
            new Color(1f, .7f, .16f), new Color(1f, .4f, .5f), new Color(.75f, .4f, 1f) };
        private int _layer = -1;
        private int _channel;
        [SerializeField] private int _row;
        [SerializeField] private int _column;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private Renderer _renderer;
        private MaterialPropertyBlock _properties;
        private bool _computed = true;
        private bool _spotlight;
        public bool IsDetached { get; private set; }
        public int Layer => _layer;
        public int Channel => _channel;
        public int Row => _row;
        public int Column => _column;

        public void Initialize(int layer, int channel)
        {
            _layer = layer;
            _channel = channel;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _properties = new MaterialPropertyBlock();
            _renderer.enabled = true;
        }

        public void SetLearningState(bool computed, bool spotlight)
        {
            _computed = computed;
            _spotlight = spotlight;
        }

        public void RefreshAppearance(ConvolutionSimulation data)
        {
            float value = data.ReadValue(_layer, _channel, _row, _column);
            Color color = _layer < 0 ? data.ReadInputColor(_row, _column) :
                Color.Lerp(new Color(.025f, .04f, .06f), Palette[_channel % Palette.Length],
                    Mathf.Clamp01(Mathf.Abs(value) / data.ReadScale(_layer, _channel)));
            if (!_computed)
            {
                color = new Color(.035f, .055f, .065f);
            }
            bool affected = _spotlight || data.IsAffected(_layer, _channel, _row, _column);
            if (affected)
            {
                Color highlight = new Color(1f, .83f, .22f);
                // Preserve the RGB input while marking its connected positions.
                color = _layer < 0 ? Color.Lerp(color, highlight, .32f) : highlight;
            }
            _properties.SetColor(ColorId, color);
            if (_layer >= 0)
            {
                _properties.SetColor(EmissionId, color * (affected ? .55f : .025f));
            }
            _renderer.SetPropertyBlock(_properties);
        }

        public void Extract()
        {
            // A pixel tile remains visible before and after extraction.
            IsDetached = true;
        }

        public void ReturnToGrid()
        {
            IsDetached = false;
            _renderer.enabled = true;
        }
    }
}
