using UnityEngine;

namespace CNNLearning
{
    [RequireComponent(typeof(Renderer))]
    public sealed class CNNLearningNode : MonoBehaviour
    {
        [SerializeField] private CNNLearningScene _scene;
        [SerializeField] private int _layer = -1;
        [SerializeField] private int _channel;
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

        public void RefreshAppearance(CNNLearningExampleData data)
        {
            float value = data.ReadValue(_layer, _channel, _row, _column);
            Color color = _layer < 0 ? new Color(value, value, value) :
                Color.Lerp(new Color(.1f, .22f, .28f), new Color(.25f, .7f, .83f), Mathf.Clamp01(value));
            if (!_computed)
            {
                color = new Color(.035f, .055f, .065f);
            }
            bool affected = _spotlight || data.IsAffected(_layer, _row, _column);
            if (affected)
            {
                Color highlight = new Color(1f, .83f, .22f);
                // Preserve the input's black/white contrast while showing the sampled patch.
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
            // A neuron remains a visible solid object before and after extraction.
            IsDetached = true;
        }

        public void ReturnToGrid()
        {
            IsDetached = false;
            _renderer.enabled = true;
        }

        public void SelectPixel()
        {
            if (_layer < 0)
            {
                _scene.SelectPixel(_row, _column);
            }
        }
    }
}
