using System.Collections.Generic;
using UnityEngine;

namespace ModelDesignScene.ModelBuilder
{
    // One result per layer; colours describe structure, not inferred values.
    internal sealed class VectorBlockMesh
    {
        private const int Primary = 1;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<int>[] _triangles = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

        internal static string VectorCaption(int count)
        {
            return count > 8 ? $"8 shown / {count - 8} omitted" : "All shown";
        }

        internal static Mesh CreateOutput(int height, int width, int elements, bool spatial)
        {
            VectorBlockMesh builder = new VectorBlockMesh();
            float frameWidth = spatial ? Mathf.Max(0.12f, width * 0.025f + 0.04f) : 0.16f;
            float frameHeight = spatial ? Mathf.Max(0.12f, height * 0.025f + 0.04f) : 0.7f;
            float centerY = -0.08f + frameHeight * 0.5f;
            builder.AddBox(new Vector3(0f, -0.105f, 0f), new Vector3(Mathf.Max(0.4f, frameWidth + 0.06f), 0.03f, 0.24f), 0);
            builder.AddBox(new Vector3(0f, -0.075f, -0.12f), new Vector3(0.38f, 0.06f, 0.012f), 0);
            for (int side = -1; side <= 1; side += 2)
            {
                builder.AddBox(new Vector3(side * frameWidth * 0.5f, centerY, 0f), new Vector3(0.014f, frameHeight, 0.014f), Primary);
                builder.AddBox(new Vector3(0f, centerY + side * frameHeight * 0.5f, 0f), new Vector3(frameWidth, 0.014f, 0.014f), Primary);
            }
            if (spatial)
            {
                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        builder.AddSphere(new Vector3((column - (width - 1) * 0.5f) * 0.025f,
                            centerY + ((height - 1) * 0.5f - row) * 0.025f, 0f), 0.008f);
                    }
                }
            }
            else
            {
                int shown = Mathf.Min(8, elements);
                int slots = elements > 8 ? 9 : shown;
                for (int index = 0; index < shown; index++)
                {
                    int slot = elements > 8 && index >= 4 ? index + 1 : index;
                    builder.AddSphere(new Vector3(0f, centerY + ((slots - 1) * 0.5f - slot) * 0.065f, 0f), 0.022f);
                }
                if (elements > 8)
                {
                    for (int index = -1; index <= 1; index++)
                    {
                        builder.AddBox(new Vector3(0f, centerY + index * 0.014f, 0f), Vector3.one * 0.006f, 2);
                    }
                }
            }
            return builder.Build("Layer output");
        }

        internal static Mesh CreateConnections(List<Vector3> points)
        {
            VectorBlockMesh builder = new VectorBlockMesh();
            for (int index = 0; index < points.Count; index += 2)
            {
                Vector3 from = points[index];
                Vector3 to = points[index + 1];
                builder.AddBeam(from, to, 0.008f, 3);
                Vector3 direction = (to - from).normalized;
                Vector3 side = Vector3.Cross(direction, Vector3.forward) * 0.035f;
                builder.AddBeam(to, to - direction * 0.06f + side, 0.008f, 3);
                builder.AddBeam(to, to - direction * 0.06f - side, 0.008f, 3);
            }
            return builder.Build("Layer flow arrows");
        }

        private Mesh Build(string name)
        {
            Mesh mesh = new Mesh { name = name, subMeshCount = 4 };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            for (int index = 0; index < 4; index++)
            {
                mesh.SetTriangles(_triangles[index], index);
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddSphere(Vector3 center, float radius)
        {
            const int Rings = 6;
            const int Segments = 8;
            int start = _vertices.Count;
            for (int ring = 0; ring <= Rings; ring++)
            {
                float latitude = Mathf.PI * ring / Rings;
                for (int segment = 0; segment <= Segments; segment++)
                {
                    float longitude = 2f * Mathf.PI * segment / Segments;
                    Vector3 normal = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude),
                        Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                    _vertices.Add(center + normal * radius);
                    _normals.Add(normal);
                }
            }
            List<int> triangles = _triangles[Primary];
            for (int ring = 0; ring < Rings; ring++)
            {
                for (int segment = 0; segment < Segments; segment++)
                {
                    int top = start + ring * (Segments + 1) + segment;
                    int bottom = top + Segments + 1;
                    if (ring > 0)
                    {
                        triangles.Add(top); triangles.Add(top + 1); triangles.Add(bottom);
                    }
                    if (ring < Rings - 1)
                    {
                        triangles.Add(top + 1); triangles.Add(bottom + 1); triangles.Add(bottom);
                    }
                }
            }
        }

        private void AddBeam(Vector3 start, Vector3 end, float thickness, int material)
        {
            Vector3 direction = end - start;
            AddBox((start + end) * 0.5f, new Vector3(thickness, direction.magnitude, thickness),
                material, Quaternion.FromToRotation(Vector3.up, direction));
        }

        private void AddBox(Vector3 center, Vector3 size, int material)
        {
            AddBox(center, size, material, Quaternion.identity);
        }

        private void AddBox(Vector3 center, Vector3 size, int material, Quaternion rotation)
        {
            Vector3 half = size * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-half.x, -half.y, -half.z), new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, half.y, -half.z), new Vector3(-half.x, half.y, -half.z),
                new Vector3(-half.x, -half.y, half.z), new Vector3(half.x, -half.y, half.z),
                new Vector3(half.x, half.y, half.z), new Vector3(-half.x, half.y, half.z)
            };
            int[] faces = { 0, 3, 2, 1, 4, 5, 6, 7, 0, 4, 7, 3, 1, 2, 6, 5, 3, 7, 6, 2, 0, 1, 5, 4 };
            List<int> triangles = _triangles[material];
            for (int face = 0; face < faces.Length; face += 4)
            {
                int start = _vertices.Count;
                Vector3 normal = rotation * Vector3.Cross(corners[faces[face + 1]] - corners[faces[face]],
                    corners[faces[face + 2]] - corners[faces[face]]).normalized;
                for (int corner = 0; corner < 4; corner++)
                {
                    _vertices.Add(center + rotation * corners[faces[face + corner]]);
                    _normals.Add(normal);
                }
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }
    }
}
