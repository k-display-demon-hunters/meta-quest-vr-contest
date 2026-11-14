using System.Collections.Generic;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ModelDesignScene.Hologram
{
    public sealed class CnnHologramController : MonoBehaviour
    {
        private const float RiseSeconds = 1f;
        private const float LayerDelay = 0.15f;
        private const float LineSeconds = 0.6f;
        private const float FadeSeconds = 0.6f;
        private static readonly int Elapsed = Shader.PropertyToID("_Elapsed");
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");
        private static readonly int Reveal = Shader.PropertyToID("_Reveal");

        [SerializeField] private Material _glow;
        [SerializeField] private Font _font;

        private GameObject _visual;
        private Mesh _points, _lines;
        private MeshRenderer _pointRenderer, _lineRenderer;
        private MaterialPropertyBlock _properties;
        private CanvasGroup _labels;
        private Text _changedLabel;
        private float _started, _fadeStarted, _riseEnds, _fadeFrom = 1f;
        private int _junctions;
        private CnnHologramLayout _layout;

        internal float FlowReadyAt => _started + _riseEnds + LineSeconds;

        internal (Vector3 Center, Vector2 Extents, bool Trainable)[] ReadFlowFrames()
        {
            if (_layout == null) { return System.Array.Empty<(Vector3, Vector2, bool)>(); }
            var frames = new (Vector3, Vector2, bool)[_layout.Layers.Count];
            for (int index = 0; index < frames.Length; index++)
            {
                CnnHologramLayout.Layer layer = _layout.Layers[index];
                var bounds = new Bounds(layer.Center, Vector3.zero);
                for (int node = layer.First; node < layer.First + layer.Count; node++)
                {
                    bounds.Encapsulate(_layout.Nodes[node].Position);
                }
                Vector3 front = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.045f);
                Vector2 extents = new Vector2(Mathf.Max(0.045f, bounds.extents.x + 0.018f),
                    Mathf.Max(0.045f, bounds.extents.y + 0.018f));
                frames[index] = (_visual.transform.TransformPoint(front), extents,
                    layer.Name is "CONV" or "DENSE" or "OUTPUT");
            }
            return frames;
        }
        private bool _completed, _changed, _fading;

        internal void Show(ModelExecutionDefinition snapshot,
            IReadOnlyList<(Vector3Int Grid, int Elements, Vector3 Origin)> sizes)
        {
            Clear();
            var layout = _layout = new CnnHologramLayout(snapshot, sizes, transform);
            _visual = new GameObject("CnnHologram");
            Transform root = _visual.transform;
            root.SetParent(transform, false);
            // Preserve metre-based UI sizing in both direct Play and the scaled Factory content.
            root.localScale = Vector3.one / transform.lossyScale.x;
            _properties = new MaterialPropertyBlock();
            _points = BuildPoints(layout, root);
            _lines = BuildLines(layout);
            _pointRenderer = AddMesh(root, "Layer nodes", _points);
            _lineRenderer = AddMesh(root, "Sampled connections", _lines);
            _pointRenderer.sortingOrder = _lineRenderer.sortingOrder + 1;
            BuildLabels(root, layout);
            _junctions = layout.Layers.Count - 1;
            _riseEnds = RiseSeconds + _junctions * LayerDelay;
            _started = Time.unscaledTime;
            Apply(0f, 1f);
        }

        internal void Complete() => _completed = true;

        internal void SetAssemblyMatches(bool matches)
        {
            _changed = !matches;
            if (_changedLabel != null) { _changedLabel.gameObject.SetActive(_completed && _changed); }
        }

        internal void Fade()
        {
            if (_visual == null || _fading) { return; }
            _fadeFrom = _completed && _changed ? 0.4f : 1f;
            _fading = true;
            _fadeStarted = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            if (_visual == null) { return; }
            float now = Time.unscaledTime;
            float opacity = _completed && _changed ? 0.4f : 1f;
            if (_fading)
            {
                float fade = Mathf.Clamp01((now - _fadeStarted) / FadeSeconds);
                if (fade >= 1f) { Clear(); return; }
                opacity = _fadeFrom * (1f - Mathf.SmoothStep(0f, 1f, fade));
            }
            Apply(now - _started, opacity);
        }

        private void Apply(float time, float opacity)
        {
            if (_completed) { time = Mathf.Max(time, _riseEnds + LineSeconds); }
            float reveal = Mathf.Clamp01((time - _riseEnds) / LineSeconds);
            _properties.SetFloat(Elapsed, time);
            _properties.SetFloat(Opacity, opacity);
            _properties.SetFloat(Reveal, reveal * _junctions);
            _pointRenderer.SetPropertyBlock(_properties);
            _lineRenderer.SetPropertyBlock(_properties);
            _lineRenderer.enabled = reveal > 0f && _lines.vertexCount > 0;
            _labels.alpha = Mathf.Clamp01((time - _riseEnds + 0.2f) / 0.35f) * opacity;
        }

        private MeshRenderer AddMesh(Transform root, string name, Mesh mesh)
        {
            GameObject visual = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            visual.transform.SetParent(root, false);
            visual.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _glow;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private static Mesh BuildPoints(CnnHologramLayout layout, Transform root)
        {
            int count = layout.Nodes.Count;
            var centers = new Vector3[count * 4];
            var colors = new Color32[count * 4];
            var corners = new List<Vector3>(count * 4);
            var starts = new List<Vector4>(count * 4);
            var indices = new int[count * 6];
            var offsets = new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1) };
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            for (int layerIndex = 0; layerIndex < layout.Layers.Count; layerIndex++)
            {
                CnnHologramLayout.Layer layer = layout.Layers[layerIndex];
                Vector3 origin = root.InverseTransformPoint(layer.Origin);
                bounds.Encapsulate(origin);
                for (int index = layer.First; index < layer.First + layer.Count; index++)
                {
                    var node = layout.Nodes[index];
                    bounds.Encapsulate(node.Position);
                    int vertex = index * 4;
                    Vector3 source = origin + (node.Position - layer.Center) * 0.18f;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        centers[vertex + corner] = node.Position;
                        colors[vertex + corner] = node.Color;
                        corners.Add(new Vector3(offsets[corner].x, offsets[corner].y, node.Radius));
                        starts.Add(new Vector4(source.x, source.y, source.z, layerIndex * LayerDelay));
                    }
                    int triangle = index * 6;
                    indices[triangle] = vertex;
                    indices[triangle + 1] = vertex + 1;
                    indices[triangle + 2] = vertex + 2;
                    indices[triangle + 3] = vertex;
                    indices[triangle + 4] = vertex + 2;
                    indices[triangle + 5] = vertex + 3;
                }
            }
            bounds.Expand(0.6f);
            var mesh = new Mesh { name = "CNN billboard nodes" };
            mesh.vertices = centers;
            mesh.colors32 = colors;
            mesh.SetUVs(0, corners);
            mesh.SetUVs(1, starts);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = bounds;
            return mesh;
        }

        private static Mesh BuildLines(CnnHologramLayout layout)
        {
            int count = layout.Lines.Count * 2;
            var vertices = new Vector3[count];
            var colors = new Color32[count];
            var metadata = new List<Vector4>(count);
            var indices = new int[count];
            for (int index = 0; index < layout.Lines.Count; index++)
            {
                var line = layout.Lines[index];
                int vertex = index * 2;
                vertices[vertex] = layout.Nodes[line.Start].Position;
                vertices[vertex + 1] = layout.Nodes[line.End].Position;
                colors[vertex] = colors[vertex + 1] = new Color32(28, 111, 127, 11);
                metadata.Add(new Vector4(0f, 0f, 0f, line.Stage));
                metadata.Add(metadata[vertex]);
                indices[vertex] = vertex;
                indices[vertex + 1] = vertex + 1;
            }
            var mesh = new Mesh { name = "CNN sampled topology" };
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.uv = new Vector2[count];
            mesh.SetUVs(1, metadata);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            return mesh;
        }

        private void BuildLabels(Transform root, CnnHologramLayout layout)
        {
            var panel = new GameObject("Hologram labels", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            panel.transform.SetParent(root, false);
            ((RectTransform)panel.transform).sizeDelta = new Vector2(2600f, 1400f);
            panel.transform.localScale = Vector3.one * 0.001f;
            panel.transform.localPosition = Vector3.back * 0.315f;
            panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _labels = panel.GetComponent<CanvasGroup>();
            _labels.blocksRaycasts = false;
            _labels.interactable = false;

            BuildTitleCard(panel.transform, layout.Top);

            foreach (CnnHologramLayout.Layer layer in layout.Layers)
            {
                if (!layer.IsSpatial && layer.Elements > layer.Count)
                {
                    AddLabel(panel.transform, "⋮", new Vector2(layer.Center.x, 0f), 0.058f, 0.08f, layer.Color);
                }
                if (!layer.IsOutput) { continue; }
                for (int index = 0; index < layer.Count; index++)
                {
                    var node = layout.Nodes[layer.First + index];
                    AddLabel(panel.transform, index.ToString(), new Vector2(node.Position.x + 0.087f, node.Position.y),
                        0.060f, 0.10f, layer.Color);
                }
            }
        }

        private void BuildTitleCard(Transform parent, float top)
        {
            var card = new GameObject("Hologram title card", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(parent, false);
            Image background = card.GetComponent<Image>();
            background.rectTransform.pivot = new Vector2(0.5f, 0f);
            background.rectTransform.anchoredPosition = Vector2.up * ((top + 0.025f) * 1000f);
            background.color = new Color(0.073f, 0.082f, 0.085f, 0.96f);
            background.raycastTarget = false;

            VerticalLayoutGroup stack = card.GetComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(30, 30, 18, 18);
            stack.spacing = 6f;
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = stack.childForceExpandHeight = false;

            ContentSizeFitter fit = card.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Color light = new Color32(234, 246, 255, 255);
            CreateLabel(card.transform, "내 CNN 모델", 72, light);
            CreateLabel(card.transform, "점과 연결선은 일부만 표시해요\n점의 밝기는 실제 계산값과 무관해요", 40, light);
            _changedLabel = CreateLabel(card.transform, "구성이 바뀌었어요 · 다시 학습하면 반영돼요", 40, light);
            _changedLabel.gameObject.SetActive(false);
        }

        private Text AddLabel(Transform parent, string value, Vector2 position, float size, float width, Color color)
        {
            Text text = CreateLabel(parent, value, Mathf.RoundToInt(size * 1000f), color);
            text.rectTransform.anchoredPosition = position * 1000f;
            text.rectTransform.sizeDelta = new Vector2(width * 1000f, size * 2350f);
            return text;
        }

        private Text CreateLabel(Transform parent, string value, int fontSize, Color color)
        {
            var entry = new GameObject("Hologram text", typeof(RectTransform), typeof(Text));
            entry.transform.SetParent(parent, false);
            Text text = entry.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        internal void Clear()
        {
            if (_visual != null)
            {
                _visual.SetActive(false);
                Destroy(_visual);
                Destroy(_points);
                Destroy(_lines);
                _visual = null;
            }
            _completed = _fading = _changed = false;
            _changedLabel = null;
            _layout = null;
        }

        private void OnDisable() => Clear();
    }
}
