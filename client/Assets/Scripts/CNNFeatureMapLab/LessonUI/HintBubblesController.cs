using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class HintBubblesController
    {
        private const int RecapCount = 5;
        [SerializeField] private Transform _root;
        private CorridorRenderUtility _visuals;
        [SerializeField] private Transform _hint;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private Renderer _dot;
        [SerializeField] private Transform[] _recaps = new Transform[RecapCount];
        [SerializeField] private TMP_Text[] _chips = new TMP_Text[RecapCount*6];
        [SerializeField] private Renderer[] _chipFrames = new Renderer[RecapCount*6];
        private float[] _expiration = new float[RecapCount];
        private readonly int[] _recapLayers = new int[RecapCount];
        private int _next;
        internal void Bind(CorridorRenderUtility visuals)
        {
            _visuals = visuals;
            Hide();
        }

        private HintBubblesController()
        {
        }

        internal void Show(string text, Vector3 position)
        {
            _hint.localPosition = position;
            _caption.text = text;
            _hint.gameObject.SetActive(true);
        }

        internal void Hide()
        {
            _hint.gameObject.SetActive(false);
        }

        internal void AddRecap(int layer, Vector3 position, string[] sources, string result, Color32[] colors)
        {
            int index = _next++ % RecapCount;
            _recapLayers[index] = layer;
            _recaps[index].localPosition = position;
            _recaps[index].gameObject.SetActive(true);
            int count = sources.Length+1;
            for (int i = 0; i < 6; i++)
            {
                bool visible = i < count;
                _chipFrames[(index)*6+i].gameObject.SetActive(visible);
                _chips[(index)*6+i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }
                float x = (i-(count-1)*.5f)*.73f;
                _chipFrames[(index)*6+i].transform.localPosition = new Vector3(x, 0, 0);
                _chips[(index)*6+i].transform.localPosition = new Vector3(x, 0, -.03f);
                _chips[(index)*6+i].text = i < sources.Length ? sources[i]+(i < sources.Length-1 ? " +" : " →") : result;
                _visuals.Paint(_chipFrames[(index)*6+i], i < sources.Length ? colors[i] : _visuals.Theme.Amber);
            }
            _expiration[index] = Time.unscaledTime+20;
        }

        internal void SetRecapVisibility(int layer)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < RecapCount; i++)
            {
                _recaps[i].gameObject.SetActive(_recapLayers[i] <= layer && now < _expiration[i]);
            }
        }

        internal void Tick(float now)
        {
            for (int i = 0; i < RecapCount; i++)
            if (_recaps[i].gameObject.activeSelf && now >= _expiration[i])
            {
                _recaps[i].gameObject.SetActive(false);
            }
            if (!_hint.gameObject.activeSelf)
            {
                return;
            }
            _dot.transform.localScale = Vector3.one*(.064f+.014f*Mathf.Sin(now*4));
        }
    }
}
