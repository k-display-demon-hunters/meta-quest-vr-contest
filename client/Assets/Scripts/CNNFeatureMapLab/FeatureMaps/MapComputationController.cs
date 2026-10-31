using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class MapComputationController : MonoBehaviour
    {
        [SerializeField] private GameObject[] _sourceGroups;
        [SerializeField] private Renderer[] _sourceCells;
        [SerializeField] private Renderer[] _resultCells;
        [SerializeField] private TextMesh[] _sourceLabels;
        [SerializeField] private TextMesh _resultLabel;
        [SerializeField] private TextMesh _explanation;
        [SerializeField] private Button _closeButton;
        private MaterialPropertyBlock _properties;
        private Coroutine _animation;
        private static readonly Color[] Tints = {
            new Color(1f, .2f, .14f), new Color(.16f, .9f, .36f), new Color(.25f, .48f, 1f) };

        private static readonly Color[] FeatureTints = {
            new Color(.18f, .65f, 1f), new Color(.18f, .9f, .48f), new Color(1f, .7f, .16f),
            new Color(1f, .4f, .5f), new Color(.75f, .4f, 1f) };

        private void Awake() { _properties = new MaterialPropertyBlock(); }
        private void OnEnable() { _closeButton.onClick.AddListener(Close); }

        public void Show(ConvolutionSimulation data, int layer, int channel, int row, int column)
        {
            gameObject.SetActive(true);
            if (_animation != null) { StopCoroutine(_animation); }
            _animation = StartCoroutine(ShowContributions(data, layer, channel, row, column));
        }

        private IEnumerator ShowContributions(ConvolutionSimulation data, int layer, int channel, int row, int column)
        {
            int count = data.InputChannels(layer);
            for (int source = 0; source < 3; source++)
            {
                _sourceGroups[source].SetActive(source < count);
                _sourceLabels[source].text = layer == 0 ? "RGB"[source] + " 채널의 계산" : "앞 층 맵 " + (source + 1);
            }
            _resultLabel.text = "은닉층 " + (layer + 1) + " · 맵 " + (channel + 1);
            foreach (Renderer cell in _sourceCells) { Paint(cell, new Color(.025f, .035f, .045f)); }
            foreach (Renderer cell in _resultCells) { Paint(cell, new Color(.025f, .035f, .045f)); }
            _explanation.text = layer == 0 ? "R · G · B의 계산을 더하면 피쳐맵 한 장이 됩니다."
                : "앞 층의 모든 맵에 대한 계산을 더하면 다음 맵 한 장이 됩니다.";
            float scale = data.ReadScale(layer, channel);
            float[] terms = new float[count];
            for (int source = 0; source < count; source++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        float value = data.ReadContribution(layer, channel, source, y, x);
                        Color color = Color.Lerp(new Color(.025f, .035f, .045f), (layer == 0 ? Tints[source] : FeatureTints[source]), Mathf.Clamp01(Mathf.Abs(value) / scale));
                        if (y == row && x == column) { color = Color.yellow; }
                        Paint(_sourceCells[source * 64 + y * 8 + x], color);
                    }
                }
                terms[source] = data.ReadContribution(layer, channel, source, row, column);
                yield return new WaitForSeconds(.45f);
            }
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float value = data.ReadValue(layer, channel, y, x);
                    Color color = Color.Lerp(new Color(.025f, .035f, .045f), FeatureTints[channel], Mathf.Clamp01(Mathf.Abs(value) / scale));
                    if (y == row && x == column) { color = Color.yellow; }
                    Paint(_resultCells[y * 8 + x], color);
                }
            }
            string sum = terms[0].ToString("0.000");
            for (int i = 1; i < count; i++) { sum += " + (" + terms[i].ToString("0.000") + ")"; }
            _explanation.text = "필터 1개 → 피쳐맵 1개\n선택한 칸: " + sum + " = "
                + data.ReadValue(layer, channel, row, column).ToString("0.000");
            _animation = null;
        }

        private void Paint(Renderer target, Color color)
        {
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            target.SetPropertyBlock(_properties);
        }

        public void Close() { gameObject.SetActive(false); }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(Close);
            if (_animation != null) { StopCoroutine(_animation); _animation = null; }
        }
    }
}
