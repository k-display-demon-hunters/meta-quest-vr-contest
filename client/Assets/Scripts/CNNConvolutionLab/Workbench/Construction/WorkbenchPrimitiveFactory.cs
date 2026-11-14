using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace CNNConvolution
{
    internal sealed class WorkbenchPrimitiveFactory : IDisposable
    {
        private readonly Transform _root;
        private readonly Font _font;
        private readonly Camera _camera;
        private readonly Material _material;

        public WorkbenchPrimitiveFactory(Transform root, Font font)
        {
            _root = root;
            _font = font;
            _camera = Camera.main;
            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _material.SetFloat("_Smoothness", 0.08f);
            _material.SetFloat("_SpecularHighlights", 0);
            _material.SetFloat("_EnvironmentReflections", 0);
            _material.EnableKeyword("_EMISSION");
        }

        public Vector3 LocalPoint(Vector3 world) => _root.InverseTransformPoint(world);
        public Vector3 WorldPoint(Vector3 local) => _root.TransformPoint(local);

        public static Vector3 GridCellPosition(int index, int width)
        {
            int centre = width / 2;
            return new Vector3((index % width - centre) * 0.19f, (centre - index / width) * 0.17f, 0);
        }

        public LineRenderer CreatePath(string name, Color color, float width)
        {
            GameObject path = new GameObject(name);
            path.transform.SetParent(_root, false);
            LineRenderer line = path.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            MaterialPropertyBlock pathColor = new MaterialPropertyBlock();
            pathColor.SetColor("_BaseColor", color);
            pathColor.SetColor("_EmissionColor", color * 0.25f);
            line.SetPropertyBlock(pathColor);
            line.enabled = false;
            return line;
        }

        public void Dispose() => Destroy(_material);

        public Transform Root(string name)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(_root, false);
            return root;
        }

        public Transform Tile(Transform parent, Vector3 position, Vector3 size, Color color)
        {
            GameObject tile = new GameObject("Number tile");
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = position;
            // Keep label and collider coordinates independent from mesh dimensions.
            Transform mesh = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            mesh.name = "Tile mesh";
            mesh.SetParent(tile.transform, false);
            MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            mesh.localScale = size;
            Tint(renderer, color);
            Collider collider = mesh.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            return tile.transform;
        }

        public static void Tint(MeshRenderer renderer, Color color)
        {
            if (renderer == null) { return; }
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_EmissionColor", color == Color.cyan ? Color.cyan * 0.6f : Color.black);
            color.a = 1f;
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }

        public Text Caption(Transform parent, string value, Vector3 position, float width)
        {
            GameObject canvasObject = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = position;
            canvasObject.transform.localScale = Vector3.one * 0.001f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;
            Text label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(canvasObject.transform, false);
            label.rectTransform.sizeDelta = new Vector2(width * 1000, 100);
            label.font = _font;
            label.fontSize = 60;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        public void Frame(Transform parent, float width, float height, float depth, Color color, float edge = 0.018f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Tile(parent, new Vector3(side * width * 0.5f, 0, 0), new Vector3(edge, height + edge, depth), color);
                Tile(parent, new Vector3(0, side * height * 0.5f, 0), new Vector3(width, edge, depth), color);
            }
        }

        public Transform Highlight(string name, float width, float height)
        {
            Transform highlight = Root(name);
            Frame(highlight, width, height, 0.006f, WorkbenchColors.CalculationAccent, 0.005f);
            highlight.gameObject.SetActive(false);
            return highlight;
        }
    }
}
