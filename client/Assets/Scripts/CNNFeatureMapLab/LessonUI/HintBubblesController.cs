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
        [SerializeField] private Transform _offscreenChip;
        [SerializeField] private TMP_Text _offscreen;
        [SerializeField] private Transform[] _recaps = new Transform[RecapCount];
        [SerializeField] private TMP_Text[] _chips = new TMP_Text[RecapCount*6];
        [SerializeField] private Renderer[] _chipFrames = new Renderer[RecapCount*6];
        private float[] _expiration = new float[RecapCount];
        private int _next;
        private int _direction = -1;
        private static readonly string[] Directions =
        {
            "→ "+LessonContent.Arrow, "↗ "+LessonContent.Arrow, "↑ "+LessonContent.Arrow, "↖ "+LessonContent.Arrow, "← "+LessonContent.Arrow, "↙ "+LessonContent.Arrow, "↓ "+LessonContent.Arrow, "↘ "+LessonContent.Arrow
        };
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
            _offscreenChip.gameObject.SetActive(false);
        }

        internal void AddRecap(Vector3 position, string[] sources, string result, Color32[] colors)
        {
            int index = _next++ % RecapCount;
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

        internal void Tick(Camera camera, float now)
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
            Vector3 viewport = camera.WorldToViewportPoint(_hint.position);
            bool outside = viewport.z <= 0 || viewport.x < .08f || viewport.x > .92f || viewport.y < .1f || viewport.y > .9f;
            _offscreenChip.gameObject.SetActive(outside);
            if (!outside)
            {
                return;
            }
            Vector2 relative = new Vector2(viewport.x-.5f, viewport.y-.5f);
            if (viewport.z <= 0)
            {
                relative = -relative;
            }
            if (relative.sqrMagnitude < .0001f)
            {
                relative = Vector2.right;
            }
            float border = Mathf.Max(Mathf.Abs(relative.x)/.36f, Mathf.Abs(relative.y)/.36f);
            Vector2 edge = relative/border+Vector2.one*.5f;
            _offscreenChip.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(edge.x, edge.y, 1.4f)), camera.transform.rotation);
            int direction = Mathf.RoundToInt(Mathf.Atan2(relative.y, relative.x)*4/Mathf.PI+8)%8;
            if (_direction == direction)
            {
                return;
            }
            _direction = direction;
            _offscreen.text = Directions[direction];
        }
    }
}
