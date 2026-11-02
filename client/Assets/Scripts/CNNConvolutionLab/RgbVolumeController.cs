using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Object;

namespace CNNConvolution
{
    // Geometry is in metres; the parent is the existing world-space UI canvas.
    public sealed class RgbVolumeController : MonoBehaviour
    {
        [SerializeField] private GameObject _workbenchVisuals;
        [SerializeField] private Transform _inputAnchor;
        [SerializeField] private Transform _kernelHome;
        [SerializeField] private Transform _kernelTarget;
        [SerializeField] private Transform _resultAnchor;
        [SerializeField] private Transform _sumAnchor;
        [SerializeField] private Transform _equationAnchor;
        [SerializeField] private Text _placementHint;
        private GeometryBuilder _builder;
        private Geometry _view;
        private KernelInteractionController _interaction;
        private RgbLayerInteractionController _volume;
        private int _channel;
        private ConvolutionCalculation _calculation;
        private AnswerInteractionController _answers;
        private static readonly string[] Names = { "R", "G", "B" };
        private static readonly Color CalculationAccent = new Color(1f, 0.86f, 0.57f);
        private static readonly Color PendingValueColor = new Color(0.46f, 0.53f, 0.57f);

        public bool IsAligned => _interaction != null && _interaction.IsAligned;
        public bool WaitingForKernel { get; private set; }
        public bool HasPendingAction => _volume.HasPendingAction;
        public bool CompletePendingAction() => _volume.CompletePendingAction();
        internal void StopLayerInteraction() { _volume.StopInteraction(); }
        internal void ExplorePairs() { _volume.ExplorePairs(); }
        internal Vector3 InputCellPosition(int channel, int index) => transform.InverseTransformPoint(_view.Values[channel, index].transform.position);
        internal Vector3 KernelCellPosition(int index) => transform.InverseTransformPoint(_view.Weights[index].transform.position);
        internal Vector3 ResultCellPosition(int index) => transform.InverseTransformPoint(_view.Products[index].transform.position);
        internal Vector3 AnswerPosition => transform.InverseTransformPoint(_view.SumLabel.transform.position);
        internal Vector3 EquationPosition => transform.InverseTransformPoint(_equationAnchor.position);
        internal void BeginGuidedCalculation()
        {
            _view.SumLabel.text = Names[_channel] + " 합  0";
        }
        internal void BeginProductDisplay(int channel, int index)
        {
            ClearCalculationFeedback();
            PlaceHighlight(_view.InputHighlight, _view.Layers[channel], Cell(index) + Vector3.back * 0.083f);
            PlaceHighlight(_view.WeightHighlight, _view.Kernel, Cell(index) + Vector3.back * 0.026f);
            ShowCalculationPath(_view.Values[channel, index].transform.position, _view.Weights[index].transform.position);
        }
        internal void ShowOperandCopies(int channel, int index)
        {
            MoveOperands(InputCellPosition(channel, index), KernelCellPosition(index));
            _view.Flying.gameObject.SetActive(true);
            _view.WeightCopy.gameObject.SetActive(true);
            _view.FlyingLabel.fontSize = 52;
            _view.FlyingLabel.color = CalculationAccent;
            _view.FlyingLabel.text = _calculation.Input(channel, index).ToString();
            _view.WeightCopyLabel.text = "× " + _calculation.Weight(channel, index);
            _view.CalculationPath.enabled = false;
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
            _view.FlyingLabel.fontSize = 60;
            _view.FlyingLabel.color = Color.white;
            _view.FlyingLabel.text = $"{_calculation.Input(channel, index)} × {_calculation.Weight(channel, index)} = {_calculation.Product(channel, index)}";
        }
        internal void ShowProductValue(int index, int product)
        {
            _view.FlyingLabel.text = product.ToString();
            _view.FlyingLabel.fontSize = 52;
            _view.FlyingLabel.color = CalculationAccent;
            PlaceHighlight(_view.ProductHighlight, _view.Result, Cell(index) + Vector3.back * 0.075f);
            ShowCalculationPath(_view.Flying.position, _view.Products[index].transform.position);
        }
        internal void CommitProduct(int index, int product)
        {
            RecordProduct(index, product);
            _view.Flying.gameObject.SetActive(false);
            _view.CalculationPath.enabled = false;
            _view.InputHighlight.gameObject.SetActive(false);
            _view.WeightHighlight.gameObject.SetActive(false);
        }
        private void RecordProduct(int index, int product)
        {
            _view.Products[index].text = product.ToString();
            _view.Products[index].color = Color.white;
            _view.ProductMarks[index].gameObject.SetActive(true);
        }
        internal void BeginAccumulation(int index, int product)
        {
            _view.Flying.localPosition = ResultCellPosition(index);
            _view.FlyingLabel.text = "+ " + product;
            _view.Flying.gameObject.SetActive(true);
            _view.SumHighlight.gameObject.SetActive(true);
            ShowCalculationPath(_view.Products[index].transform.position, _view.SumLabel.transform.position);
        }
        internal void ShowAccumulation(int previousSum, int product)
        {
            _view.Flying.gameObject.SetActive(false);
            _view.CalculationPath.enabled = false;
            _view.SumLabel.fontSize = 50;
            _view.SumLabel.text = $"{previousSum} + {product} = {previousSum + product}";
            _view.SumLabel.color = product == 0 ? Color.white : CalculationAccent;
        }
        internal void CompleteProduct(int accumulated)
        {
            _view.SumLabel.text = Names[_channel] + " 합  " + accumulated;
            ClearCalculationFeedback();
        }
        private void ShowCalculationPath(Vector3 from, Vector3 to)
        {
            _view.CalculationPath.SetPosition(0, from);
            _view.CalculationPath.SetPosition(1, to);
            _view.CalculationPath.enabled = true;
        }
        private static void PlaceHighlight(Transform highlight, Transform parent, Vector3 position)
        {
            highlight.SetParent(parent, false);
            highlight.localPosition = position;
            highlight.gameObject.SetActive(true);
        }
        private void ClearCalculationFeedback()
        {
            _view.CalculationPath.enabled = false;
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy.gameObject.SetActive(false);
            _view.InputHighlight.gameObject.SetActive(false);
            _view.WeightHighlight.gameObject.SetActive(false);
            _view.ProductHighlight.gameObject.SetActive(false);
            _view.SumHighlight.gameObject.SetActive(false);
            _view.SumLabel.fontSize = 60;
            _view.SumLabel.color = Color.white;
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
                    _view.Values[c, i].fontStyle = FontStyle.Bold;
                    _view.VolumeWeightLabels[c, i].fontStyle = FontStyle.Bold;
                    _view.VolumeWeightLabels[c, i].color = i == index ? new Color(1, 0.85f, 0.3f) : Color.white;
                    _view.Values[c, i].transform.parent.localScale = Vector3.one * (i == index ? 0.00125f : 0.001f);
                }
            }
        }

        public void PrepareFilterDisplay()
        {
            _workbenchVisuals.SetActive(false);
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(true);
        }

        public IEnumerator PullColour(int channel)
        {
            ClearCalculationFeedback();
            _workbenchVisuals.SetActive(true);
            _view.Kernel.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(false);
            _view.Slot.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(true);
            yield return _volume.PullLayer(channel);
            _view.SpatialGuide.gameObject.SetActive(false);
        }


        public void Initialize(Font font)
        {
            // The prefab owns the presentation transform and editable layout anchors.
            _builder = new GeometryBuilder(transform, font);
            _view = _builder.Build();
            _view.Result.SetPositionAndRotation(_resultAnchor.position, _resultAnchor.rotation);
            _view.Slot.SetPositionAndRotation(_sumAnchor.position, _sumAnchor.rotation);
            _view.DropTarget.SetPositionAndRotation(_kernelTarget.position, _kernelTarget.rotation);
            _interaction = new KernelInteractionController(transform, _view, _kernelHome, _kernelTarget, _placementHint);
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
                    float strength = 0.18f + _calculation.Input(c, i) / 3f * 0.82f;
                    Color color = c == 0 ? new Color(strength, 0, 0) : c == 1 ? new Color(0, strength, 0) : new Color(0, 0, strength);
                    GeometryBuilder.Tint(_view.InputFaces[c, i], color);
                    _view.Values[c, i].color = Color.white;
                }
                for (int i = 0; i < 9; i++) { _view.VolumeWeightLabels[c, i].text = _calculation.Weight(c, i).ToString(); }
            }
            for (int i = 0; i < 9; i++) { GeometryBuilder.Tint(_view.ColourTiles[i], new Color(_calculation.Input(0, i) / 3f, _calculation.Input(1, i) / 3f, _calculation.Input(2, i) / 3f)); }
        }

        public IEnumerator ArrangePractice()
        {
            ReleaseInteractions();
            PrepareFilterDisplay();
            HideAnswers();
            _view.SpatialGuide.gameObject.SetActive(false);
            _view.Flying.gameObject.SetActive(false);
            _view.WeightCopy.gameObject.SetActive(false);
            _view.ColourPatch.gameObject.SetActive(false);
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].gameObject.SetActive(true);
                _view.ParkedKernels[c].gameObject.SetActive(false);
            }
            for (float time = 0; time < 1.2f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 1.2f);
                for (int c = 0; c < 3; c++)
                {
                    Vector3 start = new Vector3((c - 1) * 0.65f, 0, c * 0.1f);
                    _view.Layers[c].localPosition = Vector3.Lerp(start, PracticeLayer(c), t);
                    _view.Layers[c].localScale = Vector3.one * Mathf.Lerp(1.7f, 1, t);
                }
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].localPosition = PracticeLayer(c);
                _view.Layers[c].localScale = Vector3.one;
            }
        }

        private static Vector3 PracticeLayer(int channel) => new Vector3(-0.55f + channel * 0.24f, channel * 0.1f, 0.15f + channel * 0.14f);

        public IEnumerator RevealWeights()
        {
            // A separate stack forms beside the input; no RGB input transforms into a weight.
            for (int c = 0; c < 3; c++)
            {
                Transform layer = _view.ParkedKernels[c];
                layer.gameObject.SetActive(true);
                Vector3 target = new Vector3(0.65f + c * 0.14f, c * 0.1f, 0.15f + c * 0.14f);
                for (float time = 0; time < 0.6f; time += Time.unscaledDeltaTime)
                {
                    float t = Mathf.SmoothStep(0, 1, time / 0.6f);
                    layer.localPosition = Vector3.Lerp(target + Vector3.up * 0.7f, target, t);
                    layer.localScale = Vector3.one * Mathf.Lerp(0.1f, 1, t);
                    yield return null;
                }
                layer.localPosition = target;
                layer.localScale = Vector3.one;
            }
        }

        public IEnumerator WaitForKernel()
        {
            WaitingForKernel = true;
            while (!IsAligned)
            {
                _interaction.UpdatePlacement(Time.unscaledDeltaTime);
                yield return null;
            }
            _interaction.LockAlignedKernel();
            WaitingForKernel = false;
            yield return new WaitForSecondsRealtime(0.45f);
            _interaction.HidePlacementFeedback();
            _view.KernelTitle.gameObject.SetActive(false);
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
            ClearCalculationFeedback();
            _workbenchVisuals.SetActive(true);
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
            _view.SumLabel.text = Names[channel] + " 합  —";
            _view.KernelTitle.text = Names[channel] + " 가중치";
            _view.KernelTitle.gameObject.SetActive(true);
            for (int i = 0; i < 9; i++)
            {
                _view.Weights[i].text = _calculation.Weight(channel, i).ToString();
                _view.Products[i].text = "·";
                _view.Products[i].color = PendingValueColor;
                _view.ProductMarks[i].gameObject.SetActive(false);
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
                    _view.Layers[c].gameObject.SetActive(c == channel);
                    _view.Layers[c].localPosition = Vector3.Lerp(starts[c], LayerPosition(c, channel), t);
                    _view.ParkedKernels[c].gameObject.SetActive(false);
                    _view.ParkedKernels[c].localPosition = _view.Layers[c].localPosition + new Vector3(0.08f, 0.055f, -0.3f);
                }
                _view.Kernel.localPosition = Vector3.Lerp(_inputAnchor.localPosition, _kernelHome.localPosition, t);
                yield return null;
            }
            for (int c = 0; c < 3; c++)
            {
                _view.Layers[c].localPosition = LayerPosition(c, channel);
                if (c == channel) { _view.Layers[c].rotation = _inputAnchor.rotation; }
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
            for (int i = 0; i < count; i++) { RecordProduct(i, _calculation.Product(channel, i)); }
        }

        public void ShowAnswer(int value)
        {
            _view.SumLabel.text = value.ToString();
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

        private Vector3 LayerPosition(int c, int focus) => c == focus ? _inputAnchor.localPosition : new Vector3(-0.38f + (c + 1) * 0.22f, 0.1f + c * 0.08f, 0.65f + c * 0.55f);
        private static Vector3 Cell(int index) => GeometryBuilder.Cell(index);

        public void ReleaseInteractions()
        {
            WaitingForKernel = false;
            if (_view != null)
            {
                ClearCalculationFeedback();
                _view.GripArrow.gameObject.SetActive(false);
                _view.DropTarget.gameObject.SetActive(false);
            }
            if (_volume != null) { _volume.StopInteraction(); }
            _interaction?.DisableKernel();
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
                _material.SetFloat("_Smoothness", 0.08f);
                _material.SetFloat("_SpecularHighlights", 0);
                _material.SetFloat("_EnvironmentReflections", 0);
                _material.EnableKeyword("_EMISSION");
                for (int c = 0; c < 3; c++)
                {
                    _view.Layers[c] = Root(Names[c] + " input slice");
                    Frame(_view.Layers[c], 0.62f, 0.56f, 0.07f, new Color(0.22f, 0.25f, 0.27f));
                    Caption(_view.Layers[c], Names[c] + " 색 성분", new Vector3(0, 0.32f, 0), 0.65f);
                    Text leftSide = Caption(_view.Layers[c], Names[c], new Vector3(-0.31f, 0.28f, 0), 0.16f);
                    leftSide.transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
                    Text rightSide = Caption(_view.Layers[c], Names[c], new Vector3(0.31f, 0.28f, 0), 0.16f);
                    rightSide.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
                    for (int i = 0; i < 9; i++)
                    {
                        Transform tile = Tile(_view.Layers[c], Cell(i), new Vector3(0.17f, 0.15f, 0.12f), Color.gray);
                        Tint(tile.Find("Tile mesh").GetComponent<MeshRenderer>(), new Color(0.08f, 0.10f, 0.12f));
                        Transform face = Tile(tile, new Vector3(0, 0, -0.063f), new Vector3(0.158f, 0.138f, 0.012f), Color.gray);
                        _view.InputFaces[c, i] = face.Find("Tile mesh").GetComponent<MeshRenderer>();
                        // A neutral number inset keeps zero and bright green equally readable.
                        Tile(tile, new Vector3(-0.025f, -0.015f, -0.073f), new Vector3(0.085f, 0.095f, 0.008f), new Color(0.055f, 0.065f, 0.08f));
                        _view.Values[c, i] = Caption(tile, "", new Vector3(-0.025f, -0.015f, -0.08f), 0.085f);
                        _view.Values[c, i].fontSize = 48;
                    }
                    _view.ParkedKernels[c] = Root(Names[c] + " fixed filter slice");
                    FilterFrame(_view.ParkedKernels[c]);
                    Caption(_view.ParkedKernels[c], Names[c] + " 가중치", new Vector3(0, 0.3f, 0), 0.6f);
                    for (int i = 0; i < 9; i++)
                    {
                        Transform tile = Tile(_view.ParkedKernels[c], Cell(i), new Vector3(0.075f, 0.06f, 0.025f), new Color(0.19f, 0.23f, 0.25f));
                        _view.VolumeWeightLabels[c, i] = Caption(tile, "", new Vector3(0, 0, -0.018f), 0.075f);
                        _view.VolumeWeightLabels[c, i].fontSize = 42;
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
                _view.GripAccents = FilterFrame(_view.Kernel);
                _view.KernelTitle = Caption(_view.Kernel, "고정 가중치", new Vector3(0, 0.32f, 0), 0.7f);
                _view.GripArrow = Root("Grip arrow");
                _view.GripArrow.SetParent(_view.Kernel, false);
                _view.GripArrow.localScale = Vector3.one * 0.45f;
                _view.GripArrow.localPosition = new Vector3(0, 0.45f, -0.04f);
                Tile(_view.GripArrow, Vector3.zero, new Vector3(0.035f, 0.18f, 0.025f), Color.cyan);
                for (int side = -1; side <= 1; side += 2)
                {
                    Transform wing = Tile(_view.GripArrow, new Vector3(side * 0.045f, -0.07f, 0), new Vector3(0.035f, 0.13f, 0.025f), Color.cyan);
                    wing.localRotation = Quaternion.Euler(0, 0, -side * 45);
                }
                _view.GripArrow.gameObject.SetActive(false);
                _view.DropTarget = Root("Weight placement outline");
                BuildPlacementGuide();
                _view.DropTarget.gameObject.SetActive(false);
                for (int i = 0; i < 9; i++)
                {
                    Transform tile = Tile(_view.Kernel, Cell(i), new Vector3(0.075f, 0.06f, 0.025f), new Color(0.19f, 0.23f, 0.25f));
                    _view.Weights[i] = Caption(tile, "", new Vector3(0, 0, -0.018f), 0.075f);
                    _view.Weights[i].fontSize = 42;
                }
                _view.Result = Root("Products at matching positions");
                _view.Result.localPosition = new Vector3(0.45f, 0, 0.1f);
                Frame(_view.Result, 0.64f, 0.57f, 0.08f, new Color(0.28f, 0.31f, 0.32f));
                Tile(_view.Result, new Vector3(0, 0, 0.065f), new Vector3(0.60f, 0.53f, 0.035f), new Color(0.055f, 0.065f, 0.075f));
                Caption(_view.Result, "같은 위치의 곱", new Vector3(0, 0.32f, 0), 0.7f);
                for (int i = 0; i < 9; i++)
                {
                    Transform productTile = Tile(_view.Result, Cell(i), new Vector3(0.17f, 0.15f, 0.12f), new Color(0.16f, 0.18f, 0.22f));
                    _view.Products[i] = Caption(productTile, "·", new Vector3(0, 0, -0.066f), 0.17f);
                    _view.Products[i].color = PendingValueColor;
                    Transform mark = Root("Recorded product " + i);
                    mark.SetParent(productTile, false);
                    mark.localPosition = new Vector3(0.058f, -0.05f, -0.075f);
                    Transform shortStroke = Tile(mark, new Vector3(-0.007f, -0.003f, 0),
                        new Vector3(0.005f, 0.016f, 0.006f), CalculationAccent);
                    shortStroke.localRotation = Quaternion.Euler(0, 0, 40);
                    Transform longStroke = Tile(mark, new Vector3(0.005f, 0.003f, 0),
                        new Vector3(0.005f, 0.03f, 0.006f), CalculationAccent);
                    longStroke.localRotation = Quaternion.Euler(0, 0, -40);
                    mark.gameObject.SetActive(false);
                    _view.ProductMarks[i] = mark;
                }
                _view.Slot = Root("Answer destination");
                _view.Slot.localPosition = new Vector3(0.8f, -0.45f, 0.1f);
                Tile(_view.Slot, Vector3.zero, new Vector3(0.64f, 0.16f, 0.045f), new Color(0.055f, 0.065f, 0.075f));
                Frame(_view.Slot, 0.67f, 0.19f, 0.05f, new Color(0.47f, 0.38f, 0.24f));
                _view.SumLabel = Caption(_view.Slot, "합: —", new Vector3(0, 0, -0.03f), 0.61f);
                _view.InputHighlight = Highlight("Active input cell", 0.178f, 0.158f);
                _view.WeightHighlight = Highlight("Active weight cell", 0.092f, 0.078f);
                _view.ProductHighlight = Highlight("Active product cell", 0.178f, 0.158f);
                _view.SumHighlight = Highlight("Active accumulated sum", 0.70f, 0.22f);
                _view.SumHighlight.SetParent(_view.Slot, false);
                _view.SumHighlight.localPosition = new Vector3(0, 0, -0.035f);
                _view.Flying = Root("Animated calculation copy");
                _view.FlyingLabel = Caption(_view.Flying, "", Vector3.zero, 0.9f);
                _view.Flying.gameObject.SetActive(false);
                _view.WeightCopy = Root("Animated weight copy");
                _view.WeightCopyLabel = Caption(_view.WeightCopy, "", Vector3.zero, 0.3f);
                _view.WeightCopyLabel.fontSize = 52;
                _view.WeightCopyLabel.color = CalculationAccent;
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
                _view.CalculationPath.positionCount = 2;
                _view.CalculationPath.startWidth = _view.CalculationPath.endWidth = 0.006f;
                MaterialPropertyBlock pathColor = new MaterialPropertyBlock();
                pathColor.SetColor("_BaseColor", CalculationAccent);
                pathColor.SetColor("_EmissionColor", CalculationAccent * 0.25f);
                _view.CalculationPath.SetPropertyBlock(pathColor);
                _view.CalculationPath.enabled = false;
                return _view;
            }

            public static Vector3 Cell(int index)
            {
                return new Vector3((index % 3 - 1) * 0.19f, (1 - index / 3) * 0.17f, 0);
            }

            private Transform Highlight(string name, float width, float height)
            {
                Transform highlight = Root(name);
                Frame(highlight, width, height, 0.006f, CalculationAccent, 0.005f);
                highlight.gameObject.SetActive(false);
                return highlight;
            }

            private void Frame(Transform parent, float width, float height, float depth, Color color, float edge = 0.018f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Tile(parent, new Vector3(side * width * 0.5f, 0, 0), new Vector3(edge, height + edge, depth), color);
                    Tile(parent, new Vector3(0, side * height * 0.5f, 0), new Vector3(width, edge, depth), color);
                }
            }

            private MeshRenderer[] FilterFrame(Transform parent)
            {
                Color metal = new Color(0.47f, 0.38f, 0.24f);
                Frame(parent, 0.59f, 0.52f, 0.03f, metal);
                for (int side = -1; side <= 1; side += 2)
                {
                    Tile(parent, new Vector3(side * 0.095f, -0.30f, 0), new Vector3(0.025f, 0.08f, 0.035f), metal);
                    Tile(parent, new Vector3(side * 0.095f, 0, 0.016f), new Vector3(0.008f, 0.50f, 0.01f), metal);
                }
                Tile(parent, new Vector3(0, -0.35f, 0), new Vector3(0.25f, 0.055f, 0.055f), new Color(0.08f, 0.10f, 0.11f));
                MeshRenderer[] accents = new MeshRenderer[5];
                for (int i = -2; i <= 2; i++)
                {
                    Transform groove = Tile(parent, new Vector3(i * 0.035f, -0.35f, -0.03f), new Vector3(0.008f, 0.045f, 0.008f), metal);
                    accents[i + 2] = groove.Find("Tile mesh").GetComponent<MeshRenderer>();
                }
                return accents;
            }

            private void BuildPlacementGuide()
            {
                _view.PlacementDashed = Root("Waiting placement silhouette");
                _view.PlacementDashed.SetParent(_view.DropTarget, false);
                _view.PlacementSolid = Root("Ready placement silhouette");
                _view.PlacementSolid.SetParent(_view.DropTarget, false);
                _view.PlacementCheck = Root("Placement complete check");
                _view.PlacementCheck.SetParent(_view.DropTarget, false);
                Color waiting = new Color(0.35f, 0.78f, 0.85f);
                Color ready = new Color(1f, 0.86f, 0.57f);
                // A fixed silhouette shows the actual destination; it never pulses in size.
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int segment = -2; segment <= 2; segment++)
                    {
                        Tile(_view.PlacementDashed, new Vector3(segment * 0.13f, side * 0.295f, -0.035f),
                            new Vector3(0.075f, 0.012f, 0.012f), waiting);
                        Tile(_view.PlacementDashed, new Vector3(side * 0.33f, segment * 0.115f, -0.035f),
                            new Vector3(0.012f, 0.065f, 0.012f), waiting);
                    }
                }
                Frame(_view.PlacementSolid, 0.66f, 0.59f, 0.014f, ready);
                _view.PlacementSolid.localPosition = new Vector3(0, 0, -0.035f);
                Transform shortStroke = Tile(_view.PlacementCheck, new Vector3(0.40f, -0.28f, -0.05f),
                    new Vector3(0.022f, 0.075f, 0.015f), ready);
                shortStroke.localRotation = Quaternion.Euler(0, 0, 40);
                Transform longStroke = Tile(_view.PlacementCheck, new Vector3(0.455f, -0.25f, -0.05f),
                    new Vector3(0.022f, 0.15f, 0.015f), ready);
                longStroke.localRotation = Quaternion.Euler(0, 0, -40);
                _view.PlacementSolid.gameObject.SetActive(false);
                _view.PlacementCheck.gameObject.SetActive(false);
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
                properties.SetColor("_EmissionColor", color == Color.cyan ? Color.cyan * 0.6f : Color.black);
                color.a = 1f;
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
                label.fontSize = 60;
                label.fontStyle = FontStyle.Bold;
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
                        copy.resizeTextMinSize = 40;
                        copy.resizeTextMaxSize = 52;
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
            public readonly MeshRenderer[,] InputFaces = new MeshRenderer[3, 9];
            public readonly Text[,] VolumeWeightLabels = new Text[3, 9];
            public readonly Text[] Weights = new Text[9];
            public readonly Text[] Products = new Text[9];
            public readonly Transform[] ProductMarks = new Transform[9];
            public readonly Transform[] Answers = new Transform[3];
            public readonly Text[] AnswerLabels = new Text[3];
            public readonly List<KeyValuePair<Text, Text>> SideLabels = new List<KeyValuePair<Text, Text>>();
            public readonly Transform[] ParkedKernels = new Transform[3];
            public readonly MeshRenderer[] ColourTiles = new MeshRenderer[9];
            public LineRenderer CalculationPath;
            public Transform InputHighlight;
            public Transform WeightHighlight;
            public Transform ProductHighlight;
            public Transform SumHighlight;
            public Transform Kernel;
            public Transform GripArrow;
            public Transform DropTarget;
            public Transform PlacementDashed;
            public Transform PlacementSolid;
            public Transform PlacementCheck;
            public MeshRenderer[] GripAccents;
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
