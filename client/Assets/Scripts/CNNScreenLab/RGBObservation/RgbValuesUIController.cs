using UnityEngine;
using UnityEngine.UI;

namespace CNNScreen
{
    public sealed class RgbValuesUIController : MonoBehaviour
    {
        [SerializeField] private Text[] _values;
        [SerializeField] private CanvasGroup[] _labels;
        private readonly float[] _highlightUntil = new float[27];
        private void Update()
        {
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i].rectTransform.localScale = Vector3.one * (Time.unscaledTime < _highlightUntil[i] ? 1.1f : 1f);
            }
        }
        public void Initialize()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
            Hide();
        }
        public void Hide() { foreach (CanvasGroup label in _labels) { label.alpha = 0; } }
        public void ShowValues(PixelPatchData patch)
        {
            Hide();
            UpdateValues(patch);
        }
        public void UpdateValues(PixelPatchData patch)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    Color32 pixel = patch[i];
                    int value = c == 0 ? pixel.r : c == 1 ? pixel.g : pixel.b;
                    Text text = _values[c * 9 + i];
                    string next = value.ToString();
                    if (text.text != next) { _highlightUntil[c * 9 + i] = Time.unscaledTime + 0.18f; }
                    text.text = next;
                    text.color = c == 1 && value > 175 ? Color.black : Color.white;
                }
            }
        }
        public void SetProgress(int channel, float alpha) { _labels[channel].alpha = alpha; }
    }
}
