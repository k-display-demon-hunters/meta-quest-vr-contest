using System.Collections.Generic;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelDesignScene.ModelBuilder
{
    // One result per layer; colours describe structure, not inferred values.
    internal sealed class VectorBlockMesh
    {
        private const int Housing = 0;
        private const int Primary = 1;
        private const int Detail = 2;
        internal const float SpatialSpacing = 0.025f;
        // A low-poly sphere keeps one visible marker per cell even across 16 output planes.
        private static readonly Vector3[] DotNormals =
        {
            new Vector3(-1, 1.618034f, 0).normalized, new Vector3(1, 1.618034f, 0).normalized,
            new Vector3(-1, -1.618034f, 0).normalized, new Vector3(1, -1.618034f, 0).normalized,
            new Vector3(0, -1, 1.618034f).normalized, new Vector3(0, 1, 1.618034f).normalized,
            new Vector3(0, -1, -1.618034f).normalized, new Vector3(0, 1, -1.618034f).normalized,
            new Vector3(1.618034f, 0, -1).normalized, new Vector3(1.618034f, 0, 1).normalized,
            new Vector3(-1.618034f, 0, -1).normalized, new Vector3(-1.618034f, 0, 1).normalized
        };
        private static readonly int[] DotTriangles =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<int>[] _triangles = {
            new List<int>(), new List<int>(), new List<int>(), new List<int>()
        };

        internal static Mesh CreateOutput(Vector3Int shape, LayerType type, Vector3Int operation, bool supply)
        {
            VectorBlockMesh builder = new VectorBlockMesh();
            bool stacked = type == LayerType.Conv2D || type == LayerType.MaxPooling2D;
            bool spatial = stacked || type == LayerType.Input;
            int height = shape.x;
            int width = shape.y;
            int planes = stacked ? shape.z : 1;
            float frameWidth = spatial ? Mathf.Max(0.12f, width * SpatialSpacing + 0.04f) : 0.16f;
            float frameHeight = spatial ? Mathf.Max(0.12f, height * SpatialSpacing + 0.04f) : 0.7f;
            float centerY = -0.08f + frameHeight * 0.5f;
            int gaps = Mathf.Max(1, planes - 1);
            float depthStep = stacked ? Mathf.Min(0.064f, 0.32f / gaps) : 0f;
            float sideStep = stacked ? StackWidth(frameWidth, planes) / gaps : 0f;
            float halfStack = Mathf.Max(0, planes - 1) * 0.5f;
            float baseWidth = Mathf.Max(0.4f, frameWidth + 0.06f + sideStep * halfStack * 2f);
            float baseDepth = Mathf.Max(0.24f, depthStep * halfStack * 2f + 0.18f);
            int frameMaterial = supply ? Housing : Primary;
            builder.AddBox(new Vector3(0f, -0.105f, 0f), new Vector3(baseWidth, 0.03f, baseDepth), Housing);
            builder.AddBox(new Vector3(0f, -0.075f, -baseDepth * 0.5f), new Vector3(0.38f, 0.06f, 0.012f), Housing);
            for (int plane = 0; plane < Mathf.Max(1, planes); plane++)
            {
                float offset = plane - halfStack;
                Vector3 center = new Vector3(offset * sideStep, centerY, offset * depthStep);
                builder.AddGridFrame(center, frameWidth, frameHeight, 0.014f, frameMaterial);
                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        Vector3 cell = center + new Vector3((column - (width - 1) * 0.5f) * SpatialSpacing,
                            ((height - 1) * 0.5f - row) * SpatialSpacing, 0f);
                        if (stacked) { builder.AddStackDot(cell); }
                        else { builder.AddSphere(cell, 0.008f); }
                    }
                }
            }
            if (stacked && planes > 0)
            {
                Vector3 topLeft = new Vector3(-halfStack * sideStep - frameWidth * 0.5f + 0.02f,
                    centerY + frameHeight * 0.5f - 0.02f, -halfStack * depthStep);
                builder.AddOperationModel(type, operation, topLeft);
            }
            if (!spatial)
            {
                int elements = shape.z;
                int shown = Mathf.Min(8, elements);
                int slots = elements > 8 ? 9 : shown;
                for (int index = 0; index < shown; index++)
                {
                    int slot = elements > 8 && index >= 4 ? index + 1 : index;
                    Vector3 center = new Vector3(0f, centerY + ((slots - 1) * 0.5f - slot) * 0.065f, 0f);
                    if (supply && type == LayerType.Flatten)
                    {
                        builder.AddBox(center, new Vector3(0.086f, 0.039f, 0.019f), Primary);
                    }
                    else
                    {
                        builder.AddSphere(center, 0.022f);
                    }
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

        internal static float StackWidth(float frameWidth, int planes)
        {
            const float widthLimit = 0.79f; // Room 3 slots are 0.85 apart, including the base margin.
            return Mathf.Min(Mathf.Max(0, planes - 1) * 0.02f, Mathf.Min(0.07f, widthLimit - frameWidth));
        }

        private void AddGridFrame(Vector3 center, float width, float height, float thickness, int material)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                AddBox(center + new Vector3(side * width * 0.5f, 0f, 0f),
                    new Vector3(thickness, height, thickness), material);
                AddBox(center + new Vector3(0f, side * height * 0.5f, 0f),
                    new Vector3(width, thickness, thickness), material);
            }
        }

        private void AddOperationModel(LayerType type, Vector3Int operation, Vector3 topLeft)
        {
            const float modelScale = 1.3f;
            int firstVertex = _vertices.Count;
            bool convolution = type == LayerType.Conv2D;
            float width = operation.x * SpatialSpacing;
            float height = operation.y * SpatialSpacing;
            const float rail = 0.003f;
            int slices = convolution ? operation.z : 1;
            float sliceStep = Mathf.Min(0.008f, 0.021f / Mathf.Max(1, slices - 1));
            // Keep the back face in front of the nearest output plane.
            Vector3 center = topLeft + new Vector3(width * 0.5f, -height * 0.5f,
                -0.04f - (slices - 1) * sliceStep);
            for (int slice = slices - 1; slice >= 0; slice--)
            {
                Vector3 face = center + Vector3.forward * (slice * sliceStep);
                if (convolution)
                {
                    AddBox(face, new Vector3(width, height, sliceStep * 0.72f), Housing);
                }
                AddGridFrame(face, width, height, rail, Detail);
            }
            for (int column = 1; column < operation.x; column++)
            {
                AddBox(center + new Vector3(-width * 0.5f + column * SpatialSpacing, 0f, -sliceStep * 0.5f),
                    new Vector3(rail, height, rail), Detail);
            }
            for (int row = 1; row < operation.y; row++)
            {
                AddBox(center + new Vector3(0f, -height * 0.5f + row * SpatialSpacing, -sliceStep * 0.5f),
                    new Vector3(width, rail, rail), Detail);
            }
            for (int index = firstVertex; index < _vertices.Count; index++)
            {
                _vertices[index] = topLeft + (_vertices[index] - topLeft) * modelScale;
            }
        }

        private void AddStackDot(Vector3 center)
        {
            int start = _vertices.Count;
            foreach (Vector3 normal in DotNormals)
            {
                _vertices.Add(center + normal * 0.008f);
                _normals.Add(normal);
            }
            foreach (int index in DotTriangles)
            {
                _triangles[Primary].Add(start + index);
            }
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
            Mesh mesh = new Mesh
            {
                name = name,
                indexFormat = _vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
                subMeshCount = _triangles.Length
            };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            for (int index = 0; index < _triangles.Length; index++)
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
