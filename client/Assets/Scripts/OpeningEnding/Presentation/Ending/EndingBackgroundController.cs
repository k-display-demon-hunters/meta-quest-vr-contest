using UnityEngine;

namespace Cinematic
{
    public sealed class EndingBackgroundController : MonoBehaviour
    {
        private const int DustCount = 64;
        [SerializeField] private MeshFilter _dust;
        private Mesh _dustMesh;
        private readonly Vector3[] _dustOrigins = new Vector3[DustCount];
        private readonly Vector3[] _dustVertices = new Vector3[DustCount * 4];

        private void Awake()
        {
            var random = new System.Random(321);
            for (int i = 0; i < DustCount; i++)
            {
                _dustOrigins[i] = new Vector3(
                    (float)random.NextDouble() * 6f - 3f,
                    (float)random.NextDouble() * 2.7f + .15f,
                    (float)random.NextDouble() * 4.2f + 2.8f);
            }
            _dustMesh = MakeQuadMesh("Ending dust quads", _dustVertices);
            _dustMesh.MarkDynamic();
            _dust.sharedMesh = _dustMesh;
            TickDust(0f);
        }

        private void Update() => TickDust(Time.unscaledTime);
        private void OnDestroy() => Destroy(_dustMesh);

        private void TickDust(float time)
        {
            for (int i = 0; i < DustCount; i++)
            {
                float phase = i * 2.4f + time * .095f;
                Vector3 center = _dustOrigins[i] + new Vector3(Mathf.Sin(phase) * .11f, Mathf.Cos(phase * .83f) * .07f, 0f);
                Vector3 right = Vector3.right * (.002f + (i % 4) * .00065f);
                Vector3 up = Vector3.up * right.x;
                SetQuad(_dustVertices, i, center, right, up);
            }

            _dustMesh.vertices = _dustVertices;
            _dustMesh.bounds = new Bounds(new Vector3(0f, 1.5f, 5f), new Vector3(7f, 4f, 6f));
        }

        // Faces the stage front for dust, and faces upward when the up axis is the stage's forward axis.
        private static void SetQuad(Vector3[] vertices, int index, Vector3 center, Vector3 right, Vector3 up)
        {
            int vertex = index * 4;
            vertices[vertex] = center - right - up;
            vertices[vertex + 1] = center - right + up;
            vertices[vertex + 2] = center + right + up;
            vertices[vertex + 3] = center + right - up;
        }

        private static Mesh MakeQuadMesh(string name, Vector3[] vertices)
        {
            int[] triangles = new int[vertices.Length / 4 * 6];
            for (int i = 0; i < vertices.Length / 4; i++)
            {
                int vertex = i * 4;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }
            return new Mesh { name = name, vertices = vertices, triangles = triangles };
        }
    }
}
