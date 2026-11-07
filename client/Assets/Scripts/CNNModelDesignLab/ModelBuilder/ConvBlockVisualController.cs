using System;
using System.Collections.Generic;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ModelDesignScene.ModelBuilder
{
    // A plane is one filter's kernel for one input channel: one sphere per weight.
    // Colours describe the structure, not the values of the trained weights.
    internal sealed class ConvBlockVisualController : IDisposable
    {
        private const float FrameSize = 1f;
        private const float Bottom = -0.12f;
        private const float RailWidth = 0.022f;
        private const float StackDepth = 0.24f;

        private readonly Transform _root;
        private readonly List<MeshFilter> _planes = new List<MeshFilter>();
        private readonly Material[] _materials;
        private Mesh _mesh;
        private int _height;
        private int _width;
        private int _filters;
        private int _channels = -1;

        internal ConvBlockVisualController(MeshFilter surface, BoxCollider collider, Text label)
        {
            _root = surface.transform;
            _root.localRotation = Quaternion.Euler(0f, 90f, 0f);
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            Material[] originals = renderer.sharedMaterials;
            _materials = new[] { new Material(originals[0]), new Material(originals[1]) };
            _materials[0].SetColor("_BaseColor", new Color(0.06f, 0.62f, 0.57f));
            _materials[1].SetColor("_BaseColor", new Color(0.08f, 0.37f, 0.85f));
            renderer.sharedMaterials = _materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            _planes.Add(surface);

            collider.center = new Vector3(StackDepth * 0.5f, Bottom + FrameSize * 0.5f, -0.02f);
            collider.size = new Vector3(StackDepth + 0.16f, FrameSize + 0.04f, FrameSize + 0.08f);
            Transform caption = label.canvas.transform;
            caption.localPosition = new Vector3(-0.06f, Bottom + FrameSize + 0.12f, 0f);
            caption.localRotation = _root.localRotation;
            caption.localScale = Vector3.one * 0.002f;
        }

        internal void Refresh(LayerDefinition layer, int inputChannels)
        {
            if (_height == layer.Height && _width == layer.Width &&
                _filters == layer.Filters && _channels == inputChannels)
            {
                return;
            }

            _height = layer.Height;
            _width = layer.Width;
            _filters = layer.Filters;
            _channels = inputChannels;
            Mesh previousMesh = _mesh;

            // Invalid spatial input has a frame only; do not invent input channels.
            int count = Mathf.Max(1, layer.Filters * inputChannels);
            float depthSpacing = count > 1 ? StackDepth / (count - 1) : FrameSize;
            _mesh = CreateKernelMesh(_height, _width, inputChannels > 0, depthSpacing);
            while (_planes.Count < count)
            {
                GameObject plane = new GameObject("Conv kernel", typeof(MeshFilter), typeof(MeshRenderer));
                plane.transform.SetParent(_root, false);
                MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = _materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                _planes.Add(plane.GetComponent<MeshFilter>());
            }
            for (int index = 0; index < _planes.Count; index++)
            {
                MeshFilter plane = _planes[index];
                if (index >= count)
                {
                    plane.gameObject.SetActive(false);
                    continue;
                }
                plane.gameObject.SetActive(true);
                plane.sharedMesh = _mesh;
                if (index > 0)
                {
                    float depth = (float)index / (count - 1);
                    plane.transform.localPosition = new Vector3(0.04f * depth, 0f, StackDepth * depth);
                }
                int channelCount = Mathf.Max(1, inputChannels);
                plane.name = $"Conv filter {index / channelCount + 1} / input channel {index % channelCount + 1}";
            }
            if (previousMesh != null)
            {
                UnityEngine.Object.Destroy(previousMesh);
            }
        }

        internal static Mesh CreateSlotMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            // The frame faces along the model's X axis, so its foot runs along Z.
            AddBox(vertices, normals, triangles, new Vector3(-0.07f, 0f, -0.02f), new Vector3(0.014f, 0.012f, 1.1f));
            AddBox(vertices, normals, triangles, new Vector3(0.31f, 0f, -0.02f), new Vector3(0.014f, 0.012f, 1.1f));
            AddBox(vertices, normals, triangles, new Vector3(0.12f, 0f, -0.57f), new Vector3(0.394f, 0.012f, 0.014f));
            AddBox(vertices, normals, triangles, new Vector3(0.12f, 0f, 0.53f), new Vector3(0.394f, 0.012f, 0.014f));
            Mesh mesh = new Mesh { name = "Conv floor socket" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateKernelMesh(int height, int width, bool showWeights, float depthSpacing)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> rails = new List<int>();
            List<int> spheres = new List<int>();
            float centerY = Bottom + FrameSize * 0.5f;
            AddBox(vertices, normals, rails, new Vector3(-0.5f + RailWidth * 0.5f, centerY, 0f),
                new Vector3(RailWidth, FrameSize, RailWidth));
            AddBox(vertices, normals, rails, new Vector3(0.5f - RailWidth * 0.5f, centerY, 0f),
                new Vector3(RailWidth, FrameSize, RailWidth));
            AddBox(vertices, normals, rails, new Vector3(0f, Bottom + RailWidth * 0.5f, 0f),
                new Vector3(FrameSize - 2f * RailWidth, RailWidth, RailWidth));
            AddBox(vertices, normals, rails, new Vector3(0f, Bottom + FrameSize - RailWidth * 0.5f, 0f),
                new Vector3(FrameSize - 2f * RailWidth, RailWidth, RailWidth));
            float spacing = 0.76f / Mathf.Max(height, width);
            // Keep neighbouring channel slices from intersecting one another.
            float radius = Mathf.Min(0.065f, Mathf.Min(spacing * 0.28f, depthSpacing * 0.4f));
            if (showWeights)
            {
                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        Vector3 center = new Vector3((column - (width - 1) * 0.5f) * spacing,
                            centerY + ((height - 1) * 0.5f - row) * spacing, 0f);
                        AddSphere(vertices, normals, spheres, center, radius);
                    }
                }
            }
            // One shared mesh per kernel shape, not one GameObject per weight.
            Mesh mesh = new Mesh { name = $"Conv {height}x{width} weights", subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(rails, 0);
            mesh.SetTriangles(spheres, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSphere(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
            Vector3 center, float radius)
        {
            const int Rings = 6;
            const int Segments = 8;
            int start = vertices.Count;
            for (int ring = 0; ring <= Rings; ring++)
            {
                float latitude = Mathf.PI * ring / Rings;
                for (int segment = 0; segment <= Segments; segment++)
                {
                    float longitude = 2f * Mathf.PI * segment / Segments;
                    Vector3 normal = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude),
                        Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                    vertices.Add(center + normal * radius);
                    normals.Add(normal);
                }
            }
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

        private static void AddBox(List<Vector3> vertices, List<Vector3> normals, List<int> triangles,
            Vector3 center, Vector3 size)
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
            for (int face = 0; face < faces.Length; face += 4)
            {
                int start = vertices.Count;
                Vector3 normal = Vector3.Cross(corners[faces[face + 1]] - corners[faces[face]],
                    corners[faces[face + 2]] - corners[faces[face]]).normalized;
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices.Add(center + corners[faces[face + corner]]);
                    normals.Add(normal);
                }
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_mesh);
            foreach (Material material in _materials)
            {
                UnityEngine.Object.Destroy(material);
            }
        }
    }
}
