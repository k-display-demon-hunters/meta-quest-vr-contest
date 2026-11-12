using UnityEngine;
using UnityEngine.UI;

namespace Parchment
{
    public sealed class ParchmentDigitIllustration : MonoBehaviour
    {
        [SerializeField] private GameObject _pixels;
        [SerializeField] private GameObject _grayscale;
        [SerializeField] private GameObject _rgb;
        [SerializeField] private RawImage _smoothDigit;
        [SerializeField] private RawImage _pixelGrid;
        [SerializeField] private RawImage _colorGrid;
        [SerializeField] private RawImage[] _channels;
        [SerializeField] private Vector3 _channelStep = new Vector3(-32f, 12f, 32f);
        [SerializeField, Min(0f)] private float _pixelHold = 0.23f;
        [SerializeField, Min(0.05f)] private float _pixelTransition = 0.55f;
        [SerializeField, Min(0f)] private float _rgbHold = 0.45f;
        [SerializeField, Min(0.05f)] private float _rgbTransition = 0.65f;

        private ParchmentIllustrationType _current;
        private float _elapsed;
        private bool _playing;

        internal void Show(ParchmentIllustrationType illustration, bool animate)
        {
            _current = illustration;
            _pixels.SetActive(illustration == ParchmentIllustrationType.PixelDigit);
            _grayscale.SetActive(illustration == ParchmentIllustrationType.GrayscaleDigit);
            _rgb.SetActive(illustration == ParchmentIllustrationType.RgbDigit);
            _elapsed = 0f;
            _playing = false;
            if (_grayscale.activeSelf) { return; }

            Draw(0f);
            if (animate) { Play(); }
        }

        internal void Play()
        {
            _playing = !_grayscale.activeSelf;
        }

        private void OnDisable() => _playing = false;

        private void Update()
        {
            if (!_playing) { return; }

            _elapsed += Time.unscaledDeltaTime;
            bool pixelPage = _current == ParchmentIllustrationType.PixelDigit;
            float hold = pixelPage ? _pixelHold : _rgbHold;
            float duration = pixelPage ? _pixelTransition : _rgbTransition;
            float time = Mathf.Clamp01((_elapsed - hold) / duration);
            Draw(Mathf.SmoothStep(0f, 1f, time));
            _playing = time < 1f;
        }

        private void Draw(float progress)
        {
            if (_current == ParchmentIllustrationType.PixelDigit)
            {
                _smoothDigit.enabled = progress < 1f;
                _pixelGrid.color = new Color(1f, 1f, 1f, progress);
                return;
            }

            _colorGrid.enabled = progress < 1f;
            _colorGrid.color = new Color(1f, 1f, 1f, 1f - progress);
            for (int i = 0; i < _channels.Length; i++)
            {
                RawImage channel = _channels[i];
                channel.rectTransform.anchoredPosition3D =
                    _colorGrid.rectTransform.anchoredPosition3D + _channelStep * (i * progress);
                channel.color = new Color(1f, 1f, 1f, progress);
            }
        }
    }
}
