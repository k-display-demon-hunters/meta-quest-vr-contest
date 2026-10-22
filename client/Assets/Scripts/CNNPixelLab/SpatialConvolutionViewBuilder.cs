using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace UI
{
    internal sealed class SpatialConvolutionViewBuilder : IDisposable
    {
        private static readonly string[] Names = { "R", "G", "B" };
        private readonly Transform _root;
        private readonly Font _font;
        private readonly Camera _camera;
        private readonly View _view = new View();
        private Material _material;

        public Material Material => _material;

        public SpatialConvolutionViewBuilder(Transform root, Font font)
        {
            _root = root;
            _font = font;
            _camera = Camera.main;
        }

        public View Build()
        {
            _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _material.SetFloat("_Surface", 1);
            _material.SetFloat("_Blend", 0);
            _material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetFloat("_ZWrite", 1);
            _material.SetFloat("_Smoothness", 0.08f);
            _material.SetFloat("_SpecularHighlights", 0);
            _material.SetFloat("_EnvironmentReflections", 0);
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.SetOverrideTag("RenderType", "Transparent");
            _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c] = Root(Names[c] + " input slice");
                Caption(_view.Layers[c], Names[c] + " 색 성분", new Vector3(0, 0.32f, 0), 0.65f);
                Text leftSide = Caption(_view.Layers[c], Names[c], new Vector3(-0.31f, 0.28f, 0), 0.16f);
                leftSide.transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
                Text rightSide = Caption(_view.Layers[c], Names[c], new Vector3(0.31f, 0.28f, 0), 0.16f);
                rightSide.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
                for (int i = 0; i < 9; i++)
                {
                    Transform tile = Tile(_view.Layers[c], Cell(i), new Vector3(0.17f, 0.15f, 0.12f), Color.gray);
                    _view.Values[c, i] = Caption(tile, "", new Vector3(0, 0, -0.066f), 0.17f);
                }
                _view.ParkedKernels[c] = Root(Names[c] + " fixed filter slice");
                Caption(_view.ParkedKernels[c], Names[c] + " 가중치", new Vector3(0, 0.3f, 0), 0.6f);
                for (int i = 0; i < 9; i++)
                {
                    Transform tile = Tile(_view.ParkedKernels[c], Cell(i), new Vector3(0.09f, 0.065f, 0.08f), new Color(0.3f, 0.31f, 0.34f));
                    _view.VolumeWeightLabels[c, i] = Caption(tile, "", new Vector3(0, 0, -0.066f), 0.12f);
                }
            }
            _view.ColourPatch = Root("Reconstructed colour patch");
            for (int i = 0; i < 9; i++)
            {
                Transform tile = Tile(_view.ColourPatch, Cell(i), new Vector3(0.17f, 0.15f, 0.12f), Color.white);
                _view.ColourTiles[i] = tile.Find("Tile mesh").GetComponent<MeshRenderer>();
            }
            _view.ColourPatch.gameObject.SetActive(false);
            _view.Kernel = Root("Fixed weight slice");
            _view.KernelTitle = Caption(_view.Kernel, "고정 가중치", new Vector3(0, 0.32f, 0), 0.7f);
            for (int i = 0; i < 9; i++)
            {
                Transform tile = Tile(_view.Kernel, Cell(i), new Vector3(0.09f, 0.065f, 0.08f), new Color(0.3f, 0.31f, 0.34f));
                _view.Weights[i] = Caption(tile, "", new Vector3(0, 0, -0.066f), 0.12f);
            }
            _view.Result = Root("Products at matching positions");
            _view.Result.localPosition = new Vector3(0.65f, 0, -0.75f);
            Caption(_view.Result, "같은 위치의 곱", new Vector3(0, 0.32f, 0), 0.7f);
            for (int i = 0; i < 9; i++)
            {
                Transform productTile = Tile(_view.Result, Cell(i), new Vector3(0.17f, 0.15f, 0.12f), new Color(0.16f, 0.18f, 0.22f));
                _view.Products[i] = Caption(productTile, "?", new Vector3(0, 0, -0.066f), 0.17f);
            }
            _view.Slot = Root("Answer destination");
            _view.Slot.localPosition = new Vector3(0.25f, -0.45f, -1.05f);
            Tile(_view.Slot, Vector3.zero, new Vector3(0.36f, 0.14f, 0.035f), new Color(0.26f, 0.29f, 0.34f));
            _view.SumLabel = Caption(_view.Slot, "합: ?", new Vector3(0, 0, -0.025f), 0.34f);
            _view.Flying = Root("Animated calculation copy");
            _view.FlyingLabel = Caption(_view.Flying, "", Vector3.zero, 0.9f);
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy = Root("Animated weight copy");
            _view.WeightCopyLabel = Caption(_view.WeightCopy, "", Vector3.zero, 0.3f);
            _view.WeightCopy.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                Transform answer = Root("Numeric answer " + i);
                _view.Answers[i] = answer;
                Tile(answer, Vector3.zero, new Vector3(0.3f, 0.13f, 0.04f), new Color(0.3f, 0.34f, 0.4f));
                _view.AnswerLabels[i] = Caption(answer, "", new Vector3(0, 0, -0.025f), 0.29f);
            }
            _view.SpatialGuide = Caption(_root, "", new Vector3(0, -0.55f, -0.15f), 1.9f);
            _view.SpatialGuide.transform.parent.localRotation = Quaternion.Euler(0, 55, 0);
            _view.SpatialGuide.rectTransform.sizeDelta = new Vector2(1900, 180);
            GameObject path = new GameObject("Active multiplication correspondence");
            path.transform.SetParent(_root, false);
            _view.CalculationPath = path.AddComponent<LineRenderer>();
            _view.CalculationPath.sharedMaterial = _material;
            _view.CalculationPath.useWorldSpace = true;
            _view.CalculationPath.positionCount = 3;
            _view.CalculationPath.startWidth = _view.CalculationPath.endWidth = 0.006f;
            _view.CalculationPath.enabled = false;
            return _view;
        }

        public static Vector3 Cell(int index)
        {
            return new Vector3((index % 3 - 1) * 0.19f, (1 - index / 3) * 0.17f, 0);
        }

        private Transform Root(string name)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(_root, false);
            return root;
        }

        private Transform Tile(Transform parent, Vector3 position, Vector3 size, Color color)
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
            color.a = 0.9f;
            properties.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(properties);
        }

        private Text Caption(Transform parent, string value, Vector3 position, float width)
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
            label.fontSize = 48;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = value;
            if (parent.name == "Number tile")
            {
                // Fixed side faces make walking around the block informative without billboarding.
                for (int side = -1; side <= 1; side += 2)
                {
                    Text copy = Instantiate(label, parent);
                    copy.name = "Side value";
                    copy.transform.localScale = Vector3.one * 0.001f;
                    float halfWidth = parent.Find("Tile mesh").localScale.x * 0.5f;
                    copy.transform.localPosition = new Vector3(side * (halfWidth + 0.002f), 0, 0);
                    copy.transform.localRotation = Quaternion.Euler(0, -side * 90, 0);
                    copy.rectTransform.sizeDelta = new Vector2(115, 100);
                    copy.resizeTextForBestFit = true;
                    copy.resizeTextMinSize = 18;
                    copy.resizeTextMaxSize = 42;
                    // Each side needs its own world-space Canvas outside the front label canvas.
                    Canvas sideCanvas = copy.gameObject.AddComponent<Canvas>();
                    sideCanvas.renderMode = RenderMode.WorldSpace;
                    _view.SideLabels.Add(new KeyValuePair<Text, Text>(label, copy));
                }
            }
            return label;
        }

        public void Dispose()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        internal sealed class View
        {
            public readonly Transform[] Layers = new Transform[3];
            public readonly Text[,] Values = new Text[3, 9];
            public readonly Text[,] VolumeWeightLabels = new Text[3, 9];
            public readonly Text[] Weights = new Text[9];
            public readonly Text[] Products = new Text[9];
            public readonly Transform[] Answers = new Transform[3];
            public readonly Text[] AnswerLabels = new Text[3];
            public readonly List<KeyValuePair<Text, Text>> SideLabels = new List<KeyValuePair<Text, Text>>();
            public readonly Transform[] ParkedKernels = new Transform[3];
            public readonly MeshRenderer[] ColourTiles = new MeshRenderer[9];
            public LineRenderer CalculationPath;
            public Transform Kernel;
            public Transform ColourPatch;
            public Transform Result;
            public Transform Slot;
            public Transform Flying;
            public Text FlyingLabel;
            public Transform WeightCopy;
            public Text WeightCopyLabel;
            public Text SumLabel;
            public Text KernelTitle;
            public Text SpatialGuide;
        }
    }
}
