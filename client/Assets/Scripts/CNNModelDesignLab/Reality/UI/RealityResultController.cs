using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RealityLab
{
    public sealed class RealityResultController : MonoBehaviour
    {
        [SerializeField] private RawImage _currentImage;
        [SerializeField] private RawImage _capturePreview;
        [SerializeField] private Text _prediction;
        [SerializeField] private Text _message;
        [SerializeField] private Text[] _scores;
        [SerializeField] private Image[] _bars;
        private Texture2D _currentTexture;

        internal void ShowInput(RealityRecognitionData input)
        {
            SetImage(_currentImage, ref _currentTexture, input);
            _capturePreview.texture = _currentTexture;
            _capturePreview.enabled = input != null;
        }

        internal void ShowPrediction(IReadOnlyList<double> scores)
        {
            int best = 0;
            for (int i = 0; i < scores.Count; i++)
            {
                _scores[i].text = $"{scores[i] * 100:0.0}%";
                _bars[i].fillAmount = (float)scores[i];
                if (scores[i] > scores[best]) { best = i; }
            }
            _prediction.text = best.ToString();
            _message.text = "";
        }

        internal void ShowMessage(string message) => _message.text = message;

        private static void SetImage(RawImage view, ref Texture2D texture, RealityRecognitionData input)
        {
            if (texture != null) { Destroy(texture); texture = null; }
            view.texture = null;
            view.enabled = input != null;
            if (input == null) { return; }
            texture = new Texture2D(input.Width, input.Height, TextureFormat.RGB24, false, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] colors = new Color32[input.Width * input.Height];
            for (int y = 0; y < input.Height; y++)
            for (int x = 0; x < input.Width; x++)
            {
                int source = (y * input.Width + x) * input.Channels;
                colors[(input.Height - 1 - y) * input.Width + x] = new Color(
                    input.Pixels[source], input.Pixels[source + 1], input.Pixels[source + 2], 1);
            }
            texture.SetPixels32(colors);
            texture.Apply();
            view.texture = texture;
        }

        private void OnDestroy()
        {
            if (_currentTexture != null) { Destroy(_currentTexture); }
        }
    }
}
