using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class ShutterFloorHologram : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _projector;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _panelOpacity;
        [SerializeField] private RectTransform _scanLine;
        [SerializeField, Min(0.05f)] private float _duration = 0.3f;

        private bool _visible;
        private float _reveal;
        private Vector3 _panelScale;

        private void Awake()
        {
            _panelScale = _panel.localScale;
            Refresh();
            enabled = false;
        }

        internal void SetVisible(bool visible)
        {
            if (_visible == visible) { return; }
            _visible = visible;
            enabled = true;
        }

        private void Update()
        {
            _reveal = Mathf.MoveTowards(_reveal, _visible ? 1f : 0f, Time.unscaledDeltaTime / _duration);
            Refresh();
            if (!_visible && _reveal == 0f)
            {
                enabled = false;
            }
        }

        private void Refresh()
        {
            _projector.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.24f, _reveal));
            float panelAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, _reveal));
            _panel.localScale = new Vector3(_panelScale.x, _panelScale.y * Mathf.Max(0.001f, panelAmount), _panelScale.z);
            _panelOpacity.alpha = panelAmount * (0.95f + 0.04f * Mathf.Sin(Time.unscaledTime * 3.5f));

            float scanProgress = Mathf.Repeat(Time.unscaledTime / 3f, 1f);
            float scanLimit = _panel.rect.height * 0.46f;
            _scanLine.anchoredPosition = new Vector2(0f, Mathf.Lerp(-scanLimit, scanLimit, scanProgress));
        }
    }
}
