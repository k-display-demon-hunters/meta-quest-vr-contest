using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CNNConvolution
{
    internal sealed class WorkbenchHologramBackdrop : IDisposable
    {
        private readonly Mesh _mesh;

        public WorkbenchHologramBackdrop(Transform parent, Material material)
        {
            GameObject backdrop = new GameObject("Curved hologram backdrop", typeof(MeshFilter), typeof(MeshRenderer));
            backdrop.transform.SetParent(parent, false);
            _mesh = CreateArc();
            backdrop.GetComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer renderer = backdrop.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Mesh CreateArc()
        {
            const int segments = 36;
            const float centerDepth = -3.03f;
            const float radius = 4.62f;
            const float halfAngle = 68f * Mathf.Deg2Rad;
            const float bottom = -1.13f;
            const float top = 1.88f;
            var vertices = new Vector3[(segments + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int index = 0; index <= segments; index++)
            {
                float fraction = index / (float)segments;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, fraction);
                float x = Mathf.Sin(angle) * radius;
                float z = centerDepth + Mathf.Cos(angle) * radius;
                int vertex = index * 2;
                vertices[vertex] = new Vector3(x, bottom, z);
                vertices[vertex + 1] = new Vector3(x, top, z);
                uv[vertex] = new Vector2(fraction, 0f);
                uv[vertex + 1] = new Vector2(fraction, 1f);
                if (index == segments) { continue; }
                int triangle = index * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex + 2;
                triangles[triangle + 4] = vertex + 1;
                triangles[triangle + 5] = vertex + 3;
            }

            Mesh mesh = new Mesh { name = "Convolution hologram arc" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Dispose() => UnityEngine.Object.Destroy(_mesh);
    }
}
