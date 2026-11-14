using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    // Decorative dashed border for an empty minimap slot.
    public sealed class AssemblySlotOutline : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            const float thickness = 2f;
            const float dash = 8f;
            const float stride = 13f;
            for (float x = rect.xMin; x < rect.xMax; x += stride)
            {
                float right = Mathf.Min(x + dash, rect.xMax);
                AddDash(mesh, x, rect.yMin, right, rect.yMin + thickness);
                AddDash(mesh, x, rect.yMax - thickness, right, rect.yMax);
            }
            for (float y = rect.yMin + stride; y < rect.yMax - thickness; y += stride)
            {
                float top = Mathf.Min(y + dash, rect.yMax - thickness);
                AddDash(mesh, rect.xMin, y, rect.xMin + thickness, top);
                AddDash(mesh, rect.xMax - thickness, y, rect.xMax, top);
            }
        }

        private void AddDash(VertexHelper mesh, float left, float bottom, float right, float top)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
