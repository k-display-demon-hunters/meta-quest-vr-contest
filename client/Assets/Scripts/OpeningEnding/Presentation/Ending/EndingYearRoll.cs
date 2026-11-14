using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    // Moves and clips the glyph quads of the existing year Text.
    [AddComponentMenu("")]
    [RequireComponent(typeof(Text))]
    public sealed class EndingYearRoll : BaseMeshEffect
    {
        private readonly float[] _offsets = new float[4];
        private readonly char[] _glyphs = new char[8];
        private readonly UIVertex[] _quad = new UIVertex[4];
        private Text _text;
        private int _lastSignature = -1;
        private string _from, _to;

        internal void Bind(Text text)
        {
            _text = text;
        }

        internal void Show(string previous, string year, float elapsed, float alpha)
        {
            _text.gameObject.SetActive(alpha > 0f);
            if (alpha <= 0f) { return; }

            if (_to != year) { _from = previous; _to = year; }
            Color color = _text.color;
            color.a = alpha;
            _text.color = color;

            int signature = 0;
            for (int i = 0; i < 4; i++)
            {
                int target = year[i] - '0';
                bool first = string.IsNullOrEmpty(_from);
                int start = first ? 0 : _from[i] - '0';
                int steps = first ? 10 + target : (target - start + 10) % 10;
                float duration = first ? .47f + i * .11f : .6f;
                float position = Mathf.Lerp(0f, steps, Mathf.Clamp01(elapsed / duration));
                int whole = Mathf.FloorToInt(position);
                _glyphs[i] = (char)('0' + (start + whole) % 10);
                _glyphs[i + 4] = (char)('0' + (start + whole + 1) % 10);
                _offsets[i] = position - whole;
                signature = signature * 10 + (_glyphs[i] - '0');
            }

            if (signature != _lastSignature)
            {
                _lastSignature = signature;
                _text.text = new string(_glyphs);
            }
            _text.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (_text == null || mesh.currentVertCount != 32) { return; }

            float step = _text.fontSize * .62f;
            float travel = _text.fontSize;
            Rect rect = _text.rectTransform.rect;
            for (int glyph = 0; glyph < 8; glyph++)
            {
                int start = glyph * 4;
                for (int j = 0; j < 4; j++) { mesh.PopulateUIVertex(ref _quad[j], start + j); }
                Vector2 center = (_quad[0].position + _quad[2].position) * .5f;
                bool incoming = glyph >= 4;
                int column = glyph % 4;
                float y = rect.center.y + (_offsets[column] - (incoming ? 1f : 0f)) * travel;
                float x = rect.xMax - (3.5f - column) * step;

                for (int j = 0; j < 4; j++)
                {
                    UIVertex vertex = _quad[j];
                    UIVertex opposite = _quad[(j + 2) % 4];
                    float original = vertex.position.y - center.y + y;
                    float oppositeY = opposite.position.y - center.y + y;
                    float clipped = Mathf.Clamp(original, rect.center.y - travel * .5f, rect.center.y + travel * .5f);
                    vertex.uv0.y = Mathf.Lerp(vertex.uv0.y, opposite.uv0.y, Mathf.InverseLerp(original, oppositeY, clipped));
                    vertex.position.x += x - center.x;
                    vertex.position.y = clipped;
                    mesh.SetUIVertex(vertex, start + j);
                }
            }
        }
    }
}
