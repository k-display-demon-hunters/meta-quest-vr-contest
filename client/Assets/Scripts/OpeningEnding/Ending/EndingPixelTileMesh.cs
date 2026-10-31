using UnityEngine;
using UnityEngine.Rendering;

namespace OpeningEnding.Ending
{
    /// <summary>A reusable beveled tile surface with one mesh and two draw calls.</summary>
    internal sealed class EndingPixelTileMesh
    {
        private const int VerticesPerTile = 16;
        private readonly GameObject _root;
        private readonly Mesh _mesh;
        private readonly Vector3[] _vertices;
        private readonly Color[] _colors;
        private readonly int _count;

        public EndingPixelTileMesh(Transform parent, string name, int columns, int rows,
            Material front, Material rim, bool samplePixelCenters)
        {
            _count = columns * rows;
            _root = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            _root.transform.SetParent(parent, false);
            _mesh = new Mesh { name = name };
            _mesh.MarkDynamic();
            _vertices = new Vector3[_count * VerticesPerTile];
            _colors = new Color[_vertices.Length];
            Vector2[] uv = new Vector2[_vertices.Length];
            int[] fronts = new int[_count * 6];
            int[] sides = new int[_count * 54];
            for (int tile = 0; tile < _count; tile++)
            {
                int vertex = tile * VerticesPerTile;
                int x = tile % columns;
                int y = tile / columns;
                for (int corner = 0; corner < 4; corner++)
                {
                    bool right = corner >= 2;
                    bool top = corner == 1 || corner == 2;
                    uv[vertex + corner] = samplePixelCenters
                        ? new Vector2((x + .5f) / columns, 1f - (y + .5f) / rows)
                        : new Vector2((x + (right ? 1f : 0f)) / columns, 1f - (y + (top ? 0f : 1f)) / rows);
                }
                WriteQuad(fronts, tile * 6, vertex, vertex + 1, vertex + 2, vertex + 3);
                int triangle = tile * 54;
                for (int corner = 0; corner < 4; corner++)
                {
                    int next = (corner + 1) % 4;
                    WriteQuad(sides, triangle + corner * 6, vertex + 4 + corner, vertex + 8 + corner,
                        vertex + 8 + next, vertex + 4 + next);
                    WriteQuad(sides, triangle + 24 + corner * 6, vertex + 8 + corner, vertex + 12 + corner,
                        vertex + 12 + next, vertex + 8 + next);
                }
                WriteQuad(sides, triangle + 48, vertex + 15, vertex + 14, vertex + 13, vertex + 12);
            }
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.uv = uv;
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(fronts, 0);
            _mesh.SetTriangles(sides, 1);
            _mesh.bounds = new Bounds(new Vector3(0f, 3f, 4f), new Vector3(16f, 12f, 14f));
            _root.GetComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer renderer = _root.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { front, rim };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _root.SetActive(false);
        }

        public void SetVisible(bool visible) => _root.SetActive(visible);

        public void SetTile(int tile, Vector3 center, Quaternion rotation, Vector2 size,
            float depth, Color frontColor, Color rimColor, float bevel = .06f)
        {
            int start = tile * VerticesPerTile;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner >= 2 ? .5f : -.5f;
                float y = corner == 1 || corner == 2 ? .5f : -.5f;
                Vector3 face = new Vector3(x * size.x * (1f - bevel), y * size.y * (1f - bevel), -depth * .5f);
                Vector3 shoulder = new Vector3(x * size.x, y * size.y, -depth * .3f);
                Vector3 back = new Vector3(x * size.x, y * size.y, depth * .5f);
                _vertices[start + corner] = center + rotation * face;
                _vertices[start + 4 + corner] = _vertices[start + corner];
                _vertices[start + 8 + corner] = center + rotation * shoulder;
                _vertices[start + 12 + corner] = center + rotation * back;
                _colors[start + corner] = frontColor;
                _colors[start + 4 + corner] = rimColor * 1.15f;
                _colors[start + 8 + corner] = rimColor * .65f;
                _colors[start + 12 + corner] = rimColor * .25f;
            }
        }

        public void Upload()
        {
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
        }

        public void Dispose()
        {
            Object.Destroy(_mesh);
            Object.Destroy(_root);
        }

        private static void WriteQuad(int[] triangles, int offset, int a, int b, int c, int d)
        {
            triangles[offset] = a;
            triangles[offset + 1] = b;
            triangles[offset + 2] = c;
            triangles[offset + 3] = a;
            triangles[offset + 4] = c;
            triangles[offset + 5] = d;
        }
    }
}
