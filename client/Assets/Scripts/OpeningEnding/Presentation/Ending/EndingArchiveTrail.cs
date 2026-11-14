using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cinematic
{
    internal sealed class EndingArchiveTrail
    {
        private const float RetreatDuration = 1.2f;
        private const float CrossfadeDuration = .4f;
        private const float FinalFadeDuration = .8f;
        private const int DustCount = 64;
        private static readonly Vector2[] FinaleOffsets =
        {
            new Vector2(-1.08f, .26f), new Vector2(-1.08f, -.26f),
            new Vector2(1.08f, .26f), new Vector2(1.08f, -.26f)
        };
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        private readonly Transform _root;
        private readonly Vector3 _front;
        private readonly Vector3 _photoSize;
        private readonly Vector3 _sideLabelPosition;
        private readonly Panel[] _panels;
        private readonly Mesh _gridMesh, _dustMesh;
        private readonly Material _gridMaterial, _dustMaterial;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private readonly Vector3[] _dustOrigins = new Vector3[DustCount];
        private readonly Vector3[] _dustVertices = new Vector3[DustCount * 4];

        private sealed class Panel
        {
            internal Transform Root;
            internal MeshRenderer Photo;
            internal Text Year;
            internal Material Material;
        }

        internal EndingArchiveTrail(Transform stage, Transform front, Text currentYear, Material transparentMaterial, int panelCount)
        {
            _root = CreateObject("Ending archive trail", stage);
            _front = front.localPosition;
            _photoSize = front.localScale;
            _sideLabelPosition = new Vector3(_photoSize.x * .5f + .1f, _photoSize.y * .5f + .035f, -.012f);
            _panels = new Panel[panelCount];
            Mesh photoMesh = front.GetComponent<MeshFilter>().sharedMesh;
            for (int i = 0; i < panelCount; i++)
            {
                Material material = new Material(transparentMaterial)
                {
                    renderQueue = (int)RenderQueue.Transparent - panelCount + i,
                    name = "Ending history photo"
                };
                Transform root = CreateObject("Retired photo " + (i + 1), _root);
                Transform photo = CreateObject("Archive still", root);
                photo.localScale = front.localScale;
                MeshRenderer renderer = AttachMesh(photo, photoMesh, material);
                Text year = CreateYear(root, currentYear, _sideLabelPosition);
                _panels[i] = new Panel { Root = root, Photo = renderer, Year = year, Material = material };
                root.gameObject.SetActive(false);
            }

            _gridMaterial = new Material(transparentMaterial) { renderQueue = (int)RenderQueue.Transparent };
            _gridMaterial.SetColor(BaseColor, new Color(.18f, .78f, .72f, .08f));
            _gridMesh = BuildGrid();
            AttachMesh(CreateObject("Ending floor grid", _root), _gridMesh, _gridMaterial);

            _dustMaterial = new Material(transparentMaterial) { renderQueue = (int)RenderQueue.Transparent };
            _dustMaterial.SetColor(BaseColor, new Color(.62f, .83f, .86f, .12f));
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
            AttachMesh(CreateObject("Ending dust", _root), _dustMesh, _dustMaterial);
            TickDust(0f);
        }

        internal void SetPhoto(int index, Texture2D still, Rect crop, string year)
        {
            Panel panel = _panels[index];
            panel.Material.SetTexture(BaseMap, still);
            panel.Material.SetTextureScale("_BaseMap", crop.size);
            panel.Material.SetTextureOffset("_BaseMap", crop.position);
            panel.Year.text = year;
            panel.Root.name = "Retired photo " + year;
        }

        internal void Evaluate(float time, float[] starts, float outroStart, float quietStart)
        {
            float fade = 1f - Mathf.SmoothStep(0f, 1f, (time - quietStart + FinalFadeDuration) / FinalFadeDuration);
            float finale = Mathf.SmoothStep(0f, 1f, (time - outroStart - .3f) / 1.4f);
            for (int i = 0; i < _panels.Length; i++)
            {
                Panel panel = _panels[i];
                int firstDeparture = i + 1;
                float departure = firstDeparture < starts.Length ? starts[firstDeparture] : outroStart;
                float elapsed = time - departure;
                bool visible = elapsed > 0f && fade > 0f;
                panel.Root.gameObject.SetActive(visible);
                if (!visible) { continue; }

                float firstStep = Mathf.SmoothStep(0f, 1f, elapsed / RetreatDuration);
                float depth = firstStep;
                for (int step = firstDeparture + 1; step <= starts.Length; step++)
                {
                    float stepStart = step < starts.Length ? starts[step] : outroStart;
                    depth += Mathf.SmoothStep(0f, 1f, (time - stepStart) / RetreatDuration);
                }

                Vector3 retreatPosition = _front + new Vector3(.35f * depth, .15f * depth, .6f * depth);
                bool centerpiece = i == _panels.Length - 1;
                Vector2 offset = centerpiece ? Vector2.zero : FinaleOffsets[i % FinaleOffsets.Length];
                Vector3 finalePosition = _front + new Vector3(offset.x, offset.y + .16f, centerpiece ? .23f : .48f);
                float finaleScale = centerpiece ? .58f : .31f;
                panel.Root.localPosition = Vector3.Lerp(retreatPosition, finalePosition, finale);
                panel.Photo.transform.localScale = new Vector3(
                    _photoSize.x * Mathf.Lerp(1f, finaleScale, finale),
                    _photoSize.y * Mathf.Lerp(1f, finaleScale, finale), _photoSize.z);

                Transform yearCanvas = panel.Year.transform.parent;
                Vector3 finaleLabelPosition = new Vector3(0f, -_photoSize.y * finaleScale * .5f - .045f, -.02f);
                yearCanvas.localPosition = Vector3.Lerp(_sideLabelPosition, finaleLabelPosition, finale);

                // The foreground shader already carries the outgoing frame during its 0.4 s blend.
                float reveal = firstDeparture < starts.Length
                    ? Mathf.SmoothStep(0f, 1f, elapsed / CrossfadeDuration) : 1f;
                float opacity = Mathf.Lerp(Mathf.Lerp(1f, .35f, firstStep), centerpiece ? 1f : .78f, finale);
                opacity *= reveal * fade;
                _properties.SetColor(BaseColor, new Color(1f, 1f, 1f, opacity));
                panel.Photo.SetPropertyBlock(_properties);
                float labelAlpha = Mathf.Lerp(.7f * firstStep, .92f, finale) * reveal * fade;
                panel.Year.color = new Color(.84f, .93f, .95f, labelAlpha);
            }
        }

        internal void TickDust(float time)
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

        internal void HidePhotos()
        {
            foreach (Panel panel in _panels) { panel.Root.gameObject.SetActive(false); }
        }

        internal void Dispose()
        {
            foreach (Panel panel in _panels) { Object.Destroy(panel.Material); }
            Object.Destroy(_gridMaterial);
            Object.Destroy(_dustMaterial);
            Object.Destroy(_gridMesh);
            Object.Destroy(_dustMesh);
            Object.Destroy(_root.gameObject);
        }

        private static Text CreateYear(Transform parent, Text style, Vector3 position)
        {
            var canvas = new GameObject("Retired year label", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.gameObject.layer = parent.gameObject.layer;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;
            RectTransform canvasRect = (RectTransform)canvas.transform;
            canvasRect.SetParent(parent, false);
            canvasRect.localPosition = position;
            canvasRect.localScale = style.transform.parent.localScale;
            canvasRect.sizeDelta = new Vector2(160f, 48f);

            var label = new GameObject("Year", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.gameObject.layer = parent.gameObject.layer;
            label.rectTransform.SetParent(canvasRect, false);
            label.rectTransform.sizeDelta = canvasRect.sizeDelta;
            label.font = style.font;
            label.fontStyle = style.fontStyle;
            label.fontSize = Mathf.RoundToInt(style.fontSize * .5f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private static Transform CreateObject(string name, Transform parent)
        {
            var child = new GameObject(name) { layer = parent.gameObject.layer };
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static MeshRenderer AttachMesh(Transform target, Mesh mesh, Material material)
        {
            target.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = target.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Mesh BuildGrid()
        {
            const int linesPerAxis = 13;
            const float spacing = .5f;
            const float halfLength = (linesPerAxis - 1) * spacing * .5f;
            const float halfWidth = .0025f;
            Vector3[] vertices = new Vector3[linesPerAxis * 8];
            Vector3 center = new Vector3(0f, .01f, 3f);
            for (int i = 0; i < linesPerAxis; i++)
            {
                float offset = i * spacing - halfLength;
                SetQuad(vertices, i, center + Vector3.right * offset,
                    Vector3.right * halfWidth, Vector3.forward * halfLength);
                SetQuad(vertices, linesPerAxis + i, center + Vector3.forward * offset,
                    Vector3.right * halfLength, Vector3.forward * halfWidth);
            }
            return MakeQuadMesh("Ending floor grid quads", vertices);
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
