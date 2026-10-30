using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace UI
{
    // Geometry is in metres; the parent is the existing world-space UI canvas.
    public sealed class RgbVolumeController : MonoBehaviour
    {
        private GeometryBuilder _builder;
        private Geometry _view;
        private KernelInteractionController _interaction;
        private RgbLayerInteractionController _volume;
        private int _channel;
        private ConvolutionCalculation _calculation;
        private AnswerInteractionController _answers;
        private static readonly string[] Names = { "R", "G", "B" };

        public bool IsAligned => _interaction != null && _interaction.IsAligned;
        public bool HasPendingAction => _volume.HasPendingAction;
        public bool CompletePendingAction() => _volume.CompletePendingAction();
        internal void StopLayerInteraction() { _volume.StopInteraction(); }
        internal void ExplorePairs() { _volume.ExplorePairs(); }
        internal Vector3 InputCellPosition(int channel, int index) => _view.Layers[channel].localPosition + Cell(index);
        internal Vector3 KernelCellPosition(int index) => _view.Kernel.localPosition + Cell(index);
        internal Vector3 ResultCellPosition(int index) => _view.Result.localPosition + Cell(index);
        internal Vector3 AnswerPosition => _view.Slot.localPosition;
        internal void BeginProductDisplay(int channel, int index)
        {
            _interaction.LockAlignedKernel();
            _view.Flying.gameObject.SetActive(true);
            _view.WeightCopy.gameObject.SetActive(true);
            _view.FlyingLabel.text = _calculation.Input(channel, index).ToString();
            _view.WeightCopyLabel.text = "× (" + _calculation.Weight(channel, index) + ")";
            _view.CalculationPath.enabled = true;
            _view.CalculationPath.SetPosition(0, _view.Layers[channel].TransformPoint(Cell(index)));
            _view.CalculationPath.SetPosition(1, _view.Kernel.TransformPoint(Cell(index)));
            _view.CalculationPath.SetPosition(2, _view.Result.TransformPoint(Cell(index)));
        }
        internal void MoveOperands(Vector3 input, Vector3 weight)
        {
            _view.Flying.localPosition = input;
            _view.WeightCopy.localPosition = weight;
        }
        internal void MoveProduct(Vector3 position) { _view.Flying.localPosition = position; }
        internal void ShowProductEquation(int channel, int index)
        {
            _view.WeightCopy.gameObject.SetActive(false);
            _view.FlyingLabel.text = $"{_calculation.Input(channel, index)} × ({_calculation.Weight(channel, index)}) = {_calculation.Product(channel, index)}";
        }
        internal void CommitProduct(int index, int product)
        {
            _view.Products[index].text = product.ToString();
            _view.FlyingLabel.text = "(" + product + ")";
        }
        internal void CompleteProduct(int accumulated)
        {
            _view.SumLabel.text = "합: " + accumulated;
            _view.CalculationPath.enabled = false;
            _view.Flying.gameObject.SetActive(false);
        }
        internal void ArrangeWholeFilter(Transform filter)
        {
            _view.ColourPatch.gameObject.SetActive(false);
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].gameObject.SetActive(true);
                _view.Layers[c].localPosition = new Vector3(-0.38f, 0, 0.15f + c * 0.65f);
                _view.ParkedKernels[c].gameObject.SetActive(true);
                _view.ParkedKernels[c].SetParent(filter, false);
                _view.ParkedKernels[c].localPosition = new Vector3(0.08f, 0.055f, -0.3f + c * 0.65f);
            }
        }
        internal bool IsWholeFilterAligned()
        {
            for (int c = 0; c < 3; c++)
            {
                Vector3 offset = _view.Layers[c].InverseTransformPoint(_view.ParkedKernels[c].position);
                if (Mathf.Abs(offset.x) > 0.22f || Mathf.Abs(offset.y) > 0.20f || Mathf.Abs(offset.z) > 0.48f
                    || Quaternion.Angle(_view.ParkedKernels[c].rotation, _view.Layers[c].rotation) > 30f) { return false; }
            }
            return true;
        }
        internal void ReleaseWholeFilter()
        {
            foreach (Transform weight in _view.ParkedKernels) { weight.SetParent(transform, true); }
        }
        internal void ShowSpatialInstruction(string text) { _view.SpatialGuide.text = text; }



        public void HighlightSpatialPair(int index)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    _view.Values[c, i].fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                    _view.VolumeWeightLabels[c, i].fontStyle = i == index ? FontStyle.Bold : FontStyle.Normal;
                    _view.VolumeWeightLabels[c, i].color = i == index ? new Color(1, 0.85f, 0.3f) : Color.white;
                    _view.Values[c, i].transform.parent.localScale = Vector3.one * (i == index ? 0.00125f : 0.001f);
                }
            }
        }

        public void PrepareFilterDisplay()
        {
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(true);
        }

        public IEnumerator PullColour(int channel)
        {
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(true);
            yield return _volume.PullLayer(channel);
            _view.SpatialGuide.gameObject.SetActive(false);
        }


        public void Initialize(Font font)
        {
            transform.localScale = Vector3.one * 500f;
            transform.localRotation = Quaternion.Euler(0, -55, 0);
            transform.localPosition = new Vector3(0, 0, -180);
            _builder = new GeometryBuilder(transform, font);
            _view = _builder.Build();
            _interaction = new KernelInteractionController(transform, _view);
            _answers = new AnswerInteractionController(transform, _view);
            _volume = gameObject.AddComponent<RgbLayerInteractionController>();
            if (isActiveAndEnabled) { _volume.PairHighlighted += HighlightSpatialPair; }
            _volume.Initialize(_view.Layers, _view.ParkedKernels, _view.ColourPatch,
                _view.SpatialGuide, _builder.Material);
        }

        public void SetData(ConvolutionCalculation calculation)
        {
            _calculation = calculation;
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    _view.Values[c, i].text = _calculation.Input(c, i).ToString();
                    float strength = _calculation.Input(c, i) / 255f;
                    Color color = c == 0 ? new Color(strength, 0, 0) : c == 1 ? new Color(0, strength, 0) : new Color(0, 0, strength);
                    GeometryBuilder.Tint(_view.Values[c, i].transform.parent.parent.Find("Tile mesh").GetComponent<MeshRenderer>(), color);
                    _view.Values[c, i].color = c == 1 && strength > 0.65f ? Color.black : Color.white;
                }
                for (int i = 0; i < 9; i++) { _view.VolumeWeightLabels[c, i].text = _calculation.Weight(c, i).ToString(); }
            }
            for (int i = 0; i < 9; i++) { GeometryBuilder.Tint(_view.ColourTiles[i], new Color(_calculation.Input(0, i) / 255f, _calculation.Input(1, i) / 255f, _calculation.Input(2, i) / 255f)); }
        }

        public IEnumerator Observe(Vector3 source, bool extract = true)
        {
            HideAnswers();
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy.gameObject.SetActive(false);
            foreach (Transform slice in _view.ParkedKernels) { slice.gameObject.SetActive(false); }
            _view.SpatialGuide.gameObject.SetActive(extract);
            if (extract) { yield return _volume.Extract(source); }
            for (int c = 0; c < 3; c++) { _view.Layers[c].localPosition = Vector3.zero; }
            yield return SpreadColours(false);
        }

        public IEnumerator SpreadColours(bool combine)
        {
            _volume.StopInteraction();
            Vector3[] starts = new Vector3[3];
            _view.ColourPatch.gameObject.SetActive(false);
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _view.Layers[c].localPosition;
                _view.Layers[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector3 target = combine ? new Vector3(0, 0, 0.2f + c * 0.035f) : new Vector3((c - 1) * 0.65f, c * 0.06f, c * 0.6f);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], target, Mathf.SmoothStep(0, 1, time / 1.2f));
                }
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].localPosition = combine ? new Vector3(0, 0, 0.2f + c * 0.035f) : new Vector3((c - 1) * 0.65f, c * 0.06f, c * 0.6f);
            }
            if (combine)
            {
                foreach (Transform layer in _view.Layers) { layer.gameObject.SetActive(false); }
                _view.ColourPatch.localPosition = new Vector3(0, 0, 0.2f);
                _view.ColourPatch.gameObject.SetActive(true);
            }
            else { _volume.Explore(); }
        }

        public IEnumerator Focus(int channel)
        {
            _volume.StopInteraction();
            _view.SpatialGuide.gameObject.SetActive(false);
            _channel = channel;
            _view.ColourPatch.gameObject.SetActive(false);
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy.gameObject.SetActive(false);
            _interaction.PrepareKernel();
            _view.Kernel.gameObject.SetActive(true);
            _view.Result.gameObject.SetActive(true);
            _view.Slot.gameObject.SetActive(true);
            _view.SumLabel.text = "합: ?";
            _view.KernelTitle.text = Names[channel] + " 고정 가중치 · Grip";
            for (int i = 0; i < 9; i++)
            {
                _view.Weights[i].text = _calculation.Weight(channel, i).ToString();
                _view.Products[i].text = "?";
            }
            Vector3[] starts = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _view.Layers[c].localPosition;
            }
            for (float time = 0; time < 1; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time);
                for (int c = 0; c < 3; c++)
                {
                    _view.Layers[c].gameObject.SetActive(true);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], LayerPosition(c, channel), t);
                    _view.ParkedKernels[c].gameObject.SetActive(c != channel);
                    _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
                }
                _view.Kernel.localPosition = Vector3.Lerp(new Vector3(-0.38f, 0, 0.6f), KernelInteractionController.KernelHome, t);
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].localPosition = LayerPosition(c, channel);
                _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
            }
            _interaction.EnableKernel();
        }

        public void AlignKernel()
        {
            _interaction.AlignKernel();
        }


        public void ShowProducts(int channel, int count)
        {
            for (int i = 0; i < count; i++) { _view.Products[i].text = _calculation.Product(channel, i).ToString(); }
        }

        public void ShowAnswer(int value)
        {
            _view.SumLabel.text = value.ToString();
        }

        public void HighlightRow(int row)
        {
            for (int i = 0; i < 9; i++)
            {
                bool selected = i / 3 == row;
                _view.Weights[i].color = selected ? new Color(1, 0.83f, 0.3f) : Color.white;
                _view.Products[i].color = selected ? new Color(1, 0.83f, 0.3f) : Color.white;
                _view.Values[_channel, i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public void ShowAnswers(int[] options, Action<int> choose)
        {
            _view.SumLabel.text = "여기에 답 놓기";
            _answers.ShowAnswers(options, choose);
        }

        public void HideAnswers()
        {
            _answers.HideAnswers();
        }

        public IEnumerator CombineSums(int[] sums)
        {
            _interaction.DisableKernel();
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            Vector3[] starts = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                starts[c] = _view.Layers[c].localPosition;
                _view.ParkedKernels[c].gameObject.SetActive(true);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector3 target = new Vector3(-0.38f, 0, 0.12f + c * 0.6f);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], target, Mathf.SmoothStep(0, 1, time / 1.2f));
                    _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
                }
                yield return null;
            }
            _view.Flying.gameObject.SetActive(true);
            int total = 0;
            for (int c = 0; c < 3; c++)
            {
                _view.FlyingLabel.text = Names[c] + " 합 (" + sums[c] + ")";
                yield return Fly(_view.Layers[c].localPosition, _view.Slot.localPosition, 0.85f);
                total += sums[c];
                _view.SumLabel.text = total.ToString();
            }
            _view.FlyingLabel.text = "편향 0은 마지막에 한 번 더해요";
            _view.Flying.localPosition = new Vector3(0, 0.4f, -0.1f);
        }

        private IEnumerator Fly(Vector3 start, Vector3 end, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _view.Flying.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, time / duration));
                yield return null;
            }
            _view.Flying.localPosition = end;
        }

        private static Vector3 LayerPosition(int c, int focus) => c == focus ? new Vector3(-0.38f, 0, 0.1f) : new Vector3(-0.38f + (c + 1) * 0.22f, 0.1f + c * 0.08f, 0.65f + c * 0.55f);
        private static Vector3 Cell(int index) => GeometryBuilder.Cell(index);

        public void ReleaseInteractions()
        {
            if (_volume != null) { _volume.StopInteraction(); }
            _interaction?.ReleaseInteractions();
            _answers?.ReleaseInteractions();
        }

        private void LateUpdate()
        {
            if (_view == null) { return; }
            Camera viewer = Camera.main;
            foreach (KeyValuePair<Text, Text> pair in _view.SideLabels)
            {
                if (pair.Value.text != pair.Key.text) { pair.Value.text = pair.Key.text; }
                pair.Value.color = pair.Key.color;
                if (viewer != null)
                {
                    Vector3 towardViewer = (viewer.transform.position - pair.Value.transform.position).normalized;
                    pair.Value.enabled = Vector3.Dot(pair.Value.transform.forward, towardViewer) < -0.25f;
                }
            }
        }

        private void OnEnable()
        {
            if (_volume != null) { _volume.PairHighlighted += HighlightSpatialPair; }
        }

        private void OnDisable()
        {
            if (_volume != null) { _volume.PairHighlighted -= HighlightSpatialPair; }
            if (_view != null && _view.CalculationPath != null) { _view.CalculationPath.enabled = false; }
            ReleaseInteractions();
        }

        private void OnDestroy()
        {
            _interaction?.Dispose();
            _answers?.Dispose();
            _builder?.Dispose();
        }
        private sealed class GeometryBuilder : IDisposable
        {
            private static readonly string[] Names = { "R", "G", "B" };
            private readonly Transform _root;
            private readonly Font _font;
            private readonly Camera _camera;
            private readonly Geometry _view = new Geometry();
            private Material _material;

            public Material Material => _material;

            public GeometryBuilder(Transform root, Font font)
            {
                _root = root;
                _font = font;
                _camera = Camera.main;
            }

            public Geometry Build()
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

        }
        internal sealed class Geometry
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
