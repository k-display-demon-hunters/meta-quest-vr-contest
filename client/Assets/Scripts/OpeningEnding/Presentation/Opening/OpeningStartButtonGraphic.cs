using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class OpeningStartButtonGraphic : Image
    {
        private const int CornerSegments = 6;
        private static readonly Color32 Border = new Color32(188, 123, 38, 255);
        private static readonly Color32 Face = new Color32(36, 24, 12, 255);
        private static readonly Color32 Play = new Color32(255, 184, 75, 255);
        public static readonly Color LabelColor = new Color32(255, 240, 215, 255);

        [SerializeField] private float _iconOffset = 93f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float unit = rect.height / 122f;
            if (unit <= 0f || rect.width <= 6f * unit) { return; }

            AddRoundedRect(mesh, rect, 21f * unit, Border);
            float inset = 3f * unit;
            Rect face = Rect.MinMaxRect(rect.xMin + inset, rect.yMin + inset, rect.xMax - inset, rect.yMax - inset);
            AddRoundedRect(mesh, face, 18f * unit, Face);

            Vector2 center = rect.center + Vector2.left * (_iconOffset * unit);
            int first = mesh.currentVertCount;
            mesh.AddVert(center + new Vector2(-20f, -24f) * unit, Play, Vector2.zero);
            mesh.AddVert(center + new Vector2(-20f, 24f) * unit, Play, Vector2.zero);
            mesh.AddVert(center + new Vector2(23f, 0f) * unit, Play, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        private static void AddRoundedRect(VertexHelper mesh, Rect rect, float radius, Color32 tint)
        {
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
            int centerIndex = mesh.currentVertCount;
            mesh.AddVert(rect.center, tint, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(
                    corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= CornerSegments; step++)
                {
                    float angle = (corner + step / (float)CornerSegments) * (Mathf.PI * 0.5f);
                    Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.AddVert(point, tint, Vector2.zero);
                }
            }

            int last = mesh.currentVertCount - 1;
            for (int index = centerIndex + 1; index < last; index++)
            {
                mesh.AddTriangle(centerIndex, index + 1, index);
            }
            mesh.AddTriangle(centerIndex, centerIndex + 1, last);
        }
    }
}
