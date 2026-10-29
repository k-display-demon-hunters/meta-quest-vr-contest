using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    // A shared bevelled unit block; no textures or external model assets.
    internal static class PixelVolumeMesh
    {
        public static Mesh Create()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const float bevel = 0.045f;
            for (int layer = 0; layer < 4; layer++)
            {
                float half = layer == 0 || layer == 3 ? 0.5f - bevel : 0.5f;
                float z = layer == 0 ? -0.5f : layer == 1 ? -0.5f + bevel
                    : layer == 2 ? 0.5f - bevel : 0.5f;
                Vector2[] ring = { new Vector2(half, half - bevel), new Vector2(half - bevel, half),
                    new Vector2(-half + bevel, half), new Vector2(-half, half - bevel),
                    new Vector2(-half, -half + bevel), new Vector2(-half + bevel, -half),
                    new Vector2(half - bevel, -half), new Vector2(half, -half + bevel) };
                foreach (Vector2 point in ring) { vertices.Add(new Vector3(point.x, point.y, z)); }
            }
            for (int layer = 0; layer < 3; layer++)
            {
                for (int i = 0; i < 8; i++)
                {
                    int a = layer * 8 + i, b = layer * 8 + (i + 1) % 8;
                    triangles.AddRange(new[] { a, b, b + 8, a, b + 8, a + 8 });
                }
            }
            for (int i = 1; i < 7; i++)
            {
                triangles.AddRange(new[] { 0, i + 1, i, 24, 24 + i, 25 + i });
            }
            Mesh mesh = new Mesh { name = "Bevelled pixel block" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Material CreateMaterial(bool translucent)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_SpecularHighlights", 0);
            material.SetFloat("_EnvironmentReflections", 0);
            if (translucent)
            {
                material.SetFloat("_Surface", 1);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = 3000;
            }
            return material;
        }

        public static MeshRenderer Add(Transform parent, string name, Mesh mesh, Material material,
            Vector3 position, Vector3 size)
        {
            GameObject block = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            block.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }
    }
}
