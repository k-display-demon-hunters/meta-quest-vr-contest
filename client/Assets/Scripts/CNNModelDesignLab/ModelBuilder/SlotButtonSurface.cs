using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class SlotButtonSurface : Image
    {
        private const int SegmentsPerCorner = 6;

        [SerializeField, Min(0f)] private float _cornerRadius = 9f;
        [SerializeField, Range(0f, 0.4f)] private float _topHighlight = 0.12f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) { return; }

            float radius = Mathf.Min(_cornerRadius, Mathf.Min(rect.width, rect.height) * 0.5f);
            AddVertex(rect.center);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(
                    corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= SegmentsPerCorner; step++)
                {
                    float angle = (corner + step / (float)SegmentsPerCorner) * (Mathf.PI * 0.5f);
                    AddVertex(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }

            int last = mesh.currentVertCount - 1;
            for (int index = 1; index < last; index++)
            {
                mesh.AddTriangle(0, index + 1, index);
            }
            mesh.AddTriangle(0, 1, last);

            void AddVertex(Vector2 position)
            {
                float highlight = Mathf.InverseLerp(rect.yMin, rect.yMax, position.y) * _topHighlight;
                Color tint = Color.Lerp(color, Color.white, highlight);
                tint.a = color.a;
                mesh.AddVert(new Vector3(position.x, position.y), tint, Vector2.zero);
            }
        }
    }
}
