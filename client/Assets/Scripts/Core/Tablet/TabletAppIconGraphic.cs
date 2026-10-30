using UnityEngine;
using UnityEngine.UI;

namespace Tablet
{
    // Flat UI geometry: no texture, lighting, shading, bevel or shadow.
    [DisallowMultipleComponent]
    public sealed class TabletAppIconGraphic : MaskableGraphic
    {
        private enum IconKind { Map, Stages, Parchment }
        [SerializeField] private IconKind _icon;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (_icon)
            {
                case IconKind.Map:
                    Triangle(mesh, new Vector2(32f, 44f), new Vector2(50f, 12f), new Vector2(68f, 44f));
                    Ring(mesh, new Vector2(50f, 59f), 24f, 10f);
                    break;
                case IconKind.Stages:
                    Rectangle(mesh, 19f, 53f, 25f, 25f);
                    Rectangle(mesh, 55f, 53f, 25f, 25f);
                    Rectangle(mesh, 19f, 17f, 25f, 25f);
                    Rectangle(mesh, 55f, 17f, 25f, 25f);
                    break;
                case IconKind.Parchment:
                    Rectangle(mesh, 26f, 23f, 5f, 55f);
                    Rectangle(mesh, 69f, 23f, 5f, 55f);
                    Rectangle(mesh, 19f, 75f, 62f, 5f);
                    Rectangle(mesh, 19f, 21f, 62f, 5f);
                    Rectangle(mesh, 39f, 58f, 22f, 4f);
                    Rectangle(mesh, 39f, 47f, 22f, 4f);
                    Rectangle(mesh, 39f, 36f, 15f, 4f);
                    break;
            }
        }

        private Vector2 Position(Vector2 point)
        {
            Rect bounds = rectTransform.rect;
            float scale = Mathf.Min(bounds.width, bounds.height) / 100f;
            return bounds.center + (point - Vector2.one * 50f) * scale;
        }

        private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(Position(a), color, Vector2.zero);
            mesh.AddVert(Position(b), color, Vector2.zero);
            mesh.AddVert(Position(c), color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }

        private void Rectangle(VertexHelper mesh, float x, float y, float width, float height)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(Position(new Vector2(x, y)), color, Vector2.zero);
            mesh.AddVert(Position(new Vector2(x, y + height)), color, Vector2.zero);
            mesh.AddVert(Position(new Vector2(x + width, y + height)), color, Vector2.zero);
            mesh.AddVert(Position(new Vector2(x + width, y)), color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private void Ring(VertexHelper mesh, Vector2 center, float outerRadius, float innerRadius)
        {
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float start = i * Mathf.PI * 2f / segments;
                float end = (i + 1) * Mathf.PI * 2f / segments;
                Vector2 a = new Vector2(Mathf.Cos(start), Mathf.Sin(start));
                Vector2 b = new Vector2(Mathf.Cos(end), Mathf.Sin(end));
                int first = mesh.currentVertCount;
                mesh.AddVert(Position(center + a * outerRadius), color, Vector2.zero);
                mesh.AddVert(Position(center + b * outerRadius), color, Vector2.zero);
                mesh.AddVert(Position(center + b * innerRadius), color, Vector2.zero);
                mesh.AddVert(Position(center + a * innerRadius), color, Vector2.zero);
                mesh.AddTriangle(first, first + 1, first + 2);
                mesh.AddTriangle(first, first + 2, first + 3);
            }
        }
    }
}
