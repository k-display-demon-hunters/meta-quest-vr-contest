using UnityEngine;

namespace FactoryEnvironment
{
    // The panel and collider travel together into the housing above the doorway.
    [DefaultExecutionOrder(100)]
    public sealed class FactoryShutterController : MonoBehaviour
    {
        [SerializeField] private FactoryShutterId _id;
        [SerializeField] private Transform _panel;
        [SerializeField] private GameObject _unavailableSign;
        [SerializeField] private CanvasGroup _hologramProjector;
        [SerializeField] private RectTransform _hologramPanel;
        [SerializeField] private CanvasGroup _hologramPanelOpacity;
        [SerializeField] private RectTransform _hologramScanLine;
        [SerializeField, Min(0.05f)] private float _hologramDuration = 0.3f;
        [SerializeField] private float _travel = 3.3f;
        [SerializeField] private float _duration = 1.6f;
        private Vector3 _closedPosition;
        private float _amount;
        private bool _open;
        private bool _isNear;
        private bool _hologramVisible;
        private float _hologramReveal;
        private Vector3 _hologramPanelScale;

        public FactoryShutterId Id => _id;
        internal bool IsFullyOpen => _amount == 1f;

        private void Awake()
        {
            _closedPosition = _panel.localPosition;
            if (_hologramProjector != null)
            {
                _hologramPanelScale = _hologramPanel.localScale;
                RefreshHologram();
            }
        }

        public void Open() => _open = true;
        public void Close() => _open = false;
        public void SetNear(bool isNear) => _isNear = isNear;

        public void SetUnavailable(bool unavailable)
        {
            bool visible = unavailable && !_open && _amount < 1f;
            if (_hologramProjector != null)
            {
                _hologramVisible = visible;
            }
            else
            {
                _unavailableSign.SetActive(visible);
            }
        }

        private void Update()
        {
            if (!_hologramVisible && _hologramReveal == 0f) { return; }

            _hologramReveal = Mathf.MoveTowards(
                _hologramReveal, _hologramVisible ? 1f : 0f, Time.unscaledDeltaTime / _hologramDuration);
            RefreshHologram();
        }

        private void RefreshHologram()
        {
            _hologramProjector.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.24f, _hologramReveal));
            float panelAmount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, _hologramReveal));
            Vector3 scale = _hologramPanelScale;
            scale.y *= Mathf.Max(0.001f, panelAmount);
            _hologramPanel.localScale = scale;
            _hologramPanelOpacity.alpha = panelAmount * (0.95f + 0.04f * Mathf.Sin(Time.unscaledTime * 3.5f));

            float scanProgress = Mathf.Repeat(Time.unscaledTime / 3f, 1f);
            float scanLimit = _hologramPanel.rect.height * 0.46f;
            _hologramScanLine.anchoredPosition = new Vector2(0f, Mathf.Lerp(-scanLimit, scanLimit, scanProgress));
        }

        // Run after the area controller's LateUpdate so closing uses this frame's proximity.
        private void LateUpdate()
        {
            float target = _open ? 1f : 0f;
            if (_amount == target) { return; }
            if (!_open && _isNear) { return; }
            _amount = Mathf.MoveTowards(_amount, target, Time.deltaTime / _duration);
            _panel.localPosition = _closedPosition + Vector3.up * (_travel * Mathf.SmoothStep(0f, 1f, _amount));
        }
    }
}
