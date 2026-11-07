using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
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
        [SerializeField] private Transform _sumEquationAnchor;
        [SerializeField] private Text _placementHint;
        private GeometryBuilder _builder;
        private Geometry _view;
        private KernelInteractionController _interaction;
        private RgbLayerInteractionController _volume;
        private int _channel;
        private ConvolutionCalculation _calculation;
        private bool _outputMode;
        private bool _positionQuiz;
        public event Action<int> OutputSelected;
        private static readonly string[] Names = { "R", "G", "B" };
        private static readonly Color CalculationAccent = new Color(1f, 0.86f, 0.57f);
        private static readonly Color PendingValueColor = new Color(0.46f, 0.53f, 0.57f);

        public bool IsAligned => _interaction != null && _interaction.IsAligned;
        public bool WaitingForKernel { get; private set; }
        public bool HasPendingAction => _volume.HasPendingAction;
        public bool CompletePendingAction() => _volume.CompletePendingAction();
        internal void StopLayerInteraction() { _volume.StopInteraction(); }
        internal void ExplorePairs() { _volume.ExplorePairs(); }
        internal Vector3 InputCellPosition(int channel, int index) => transform.InverseTransformPoint(_view.Values[channel, _calculation.InputIndex(index)].transform.position);
        internal Vector3 KernelCellPosition(int index) => transform.InverseTransformPoint(_view.Weights[index].transform.position);
        internal Vector3 ResultCellPosition(int index) => transform.InverseTransformPoint(_view.Products[index].transform.position);
        internal Vector3 AnswerPosition => transform.InverseTransformPoint(_view.SumLabel.transform.position);
        internal Vector3 EquationPosition => transform.InverseTransformPoint(_view.Equation.transform.position);
        internal void BeginGuidedCalculation()
        {
            _view.SumLabel.text = Names[_channel] + " 합  0";
        }
        internal void BeginProductDisplay(int channel, int index)
        {
            ClearCalculationFeedback();
            _view.Equation.text = "";
            PlaceHighlight(_view.InputHighlight, _view.Layers[channel], GeometryBuilder.InputCell(_calculation.InputIndex(index)) + Vector3.back * 0.083f);
            PlaceHighlight(_view.WeightHighlight, _view.Kernel, Cell(index) + new Vector3(0.052f, 0.052f, -0.026f));
            ShowCalculationPath(_view.Values[channel, _calculation.InputIndex(index)].transform.position, _view.Weights[index].transform.position);
        }
        internal void ShowOperandCopies(int channel, int index)
        {
            MoveOperands(InputCellPosition(channel, index), KernelCellPosition(index));
            _view.Flying.gameObject.SetActive(true);
            _view.WeightCopy.gameObject.SetActive(true);
            _view.FlyingLabel.fontSize = 52;
            _view.FlyingLabel.color = ChannelColor(channel);
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
            _view.Flying.gameObject.SetActive(false);
            _view.Equation.color = ChannelColor(channel);
            _view.Equation.text = $"{_calculation.Input(channel, index)} × {_calculation.Weight(channel, index)} = {_calculation.Product(channel, index)}";
        }
        internal void ShowProductValue(int index, int product)
        {
            _view.Flying.gameObject.SetActive(true);
            _view.FlyingLabel.text = product.ToString();
            _view.FlyingLabel.fontSize = 52;
            _view.FlyingLabel.color = ChannelColor(_channel);
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
            foreach (Transform copy in _view.SumCopies) { copy.gameObject.SetActive(false); }
            foreach (Text product in _view.Products) { product.enabled = true; }
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
            // The main instruction panel owns this prompt; avoid a second label across the stacked layers.
            _view.SpatialGuide.gameObject.SetActive(false);
            yield return _volume.PullLayer(channel);
            _view.SpatialGuide.gameObject.SetActive(false);
        }


        public static Color ChannelColor(int channel)
        {
            return channel == 0 ? new Color(1f, 0.40f, 0.42f)
                : channel == 1 ? new Color(0.41f, 0.93f, 0.65f)
                : new Color(0.38f, 0.68f, 1f);
        }

        public void PrepareSlidingPractice()
        {
            ReleaseInteractions();
            _calculation.SetWindow(0, 0);
            _outputMode = false;
            _workbenchVisuals.SetActive(true);
            _view.ColourPatch.gameObject.SetActive(false);
            _view.SpatialGuide.gameObject.SetActive(false);
            _view.Result.gameObject.SetActive(true);
            _view.Slot.gameObject.SetActive(true);
            _view.EquationPlate.gameObject.SetActive(true);
            _view.SumEquationPlate.gameObject.SetActive(true);
            _view.Kernel.gameObject.SetActive(true);
            _view.ResultTitle.text = "같은 위치의 곱";
            _view.KernelTitle.text = "3×3 필터";
            for (int channel = 0; channel < 3; channel++)
            {
                _view.Layers[channel].SetPositionAndRotation(_inputAnchor.position, _inputAnchor.rotation);
                _view.Layers[channel].localScale = Vector3.one;
                _view.ParkedKernels[channel].gameObject.SetActive(false);
            }
            ClearEquations();
            ShowSlidingChannel(0, true);
            _view.DropTarget.SetPositionAndRotation(_kernelTarget.position, _kernelTarget.rotation);
            _interaction.EnableKernel();
        }

        public void ClearEquations()
        {
            ClearCalculationFeedback();
            _view.Equation.text = "";
            _view.SumEquation.text = "";
            _view.SumLabel.text = "합계 —";
        }

        public void ShowSlidingChannel(int channel, bool clearProducts)
        {
            ClearCalculationFeedback();
            _channel = channel;
            _view.Equation.text = "";
            _view.SumEquation.text = "";
            _view.SumLabel.text = "합계 —";
            for (int other = 0; other < 3; other++)
            {
                _view.Layers[other].gameObject.SetActive(other == channel);
            }
            for (int index = 0; index < 9; index++)
            {
                _view.Weights[index].text = _calculation.Weight(channel, index).ToString();
                if (clearProducts && !_outputMode)
                {
                    _view.Products[index].text = "·";
                    _view.Products[index].color = PendingValueColor;
                    _view.ProductMarks[index].gameObject.SetActive(false);
                }
            }
        }

        public void ShowChannelSum(int channel, bool productsOnly)
        {
            _view.SumLabel.text = "합계 " + _calculation.Sum(channel);
            string color = ColorUtility.ToHtmlStringRGB(ChannelColor(channel));
            string equation = $"<color=#{color}>{Names[channel]}</color> ";
            for (int index = 0; index < 9; index += 4)
            {
                if (index > 0) { equation += " + "; }
                equation += $"<color=#{color}>{_calculation.Input(channel, index)}</color>";
                if (!productsOnly) { equation += $"×<color=#{color}>1</color>"; }
            }
            _view.SumEquation.text = equation + " = " + _calculation.Sum(channel);
            _view.SumEquation.color = CalculationAccent;
        }

        public void ShowRgbSum()
        {
            _view.SumEquation.color = Color.white;
            _view.SumEquation.text = $"<color=#FF666B>R {_calculation.Sum(0)}</color> + "
                + $"<color=#69EDA6>G {_calculation.Sum(1)}</color> + "
                + $"<color=#61ADFF>B {_calculation.Sum(2)}</color> = "
                + _calculation.Output(_calculation.Row, _calculation.Column);
            _view.SumLabel.text = "출력 " + _calculation.Output(_calculation.Row, _calculation.Column);
        }

        public IEnumerator GatherFirstChannelSum()
        {
            Vector3 centre = ResultCellPosition(4) + Vector3.back * 0.025f;
            Vector3[] starts = new Vector3[9];
            for (int index = 0; index < 9; index++)
            {
                starts[index] = ResultCellPosition(index) + Vector3.back * 0.025f;
                _view.SumCopies[index].localPosition = starts[index];
                _view.SumCopyLabels[index].text = _calculation.Product(0, index).ToString();
                _view.SumCopyLabels[index].color = ChannelColor(0);
                _view.SumCopies[index].gameObject.SetActive(true);
                _view.Products[index].color = new Color(0.43f, 0.48f, 0.52f);
            }
            for (float time = 0; time < 0.65f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 0.65f);
                for (int index = 0; index < 9; index++)
                {
                    _view.SumCopies[index].localPosition = Vector3.Lerp(starts[index], centre, t);
                }
                yield return null;
            }
            foreach (Transform copy in _view.SumCopies) { copy.gameObject.SetActive(false); }
            _view.Flying.localPosition = centre;
            _view.FlyingLabel.fontSize = 68;
            _view.FlyingLabel.color = CalculationAccent;
            _view.FlyingLabel.text = _calculation.Sum(0).ToString();
            _view.Flying.gameObject.SetActive(true);
            _view.Products[4].enabled = false;
            yield return new WaitForSecondsRealtime(0.45f);
            yield return Fly(centre, AnswerPosition, 0.6f);
            _view.Flying.gameObject.SetActive(false);
            _view.Products[4].enabled = true;
            foreach (Text product in _view.Products) { product.color = Color.white; }
            _view.SumLabel.text = "합계 " + _calculation.Sum(0);
        }

        public void BeginOutputMap()
        {
            _outputMode = true;
            ClearCalculationFeedback();
            _view.ResultTitle.text = "합성곱 출력 · 3×3";
            for (int index = 0; index < 9; index++)
            {
                _view.Products[index].text = "·";
                _view.Products[index].color = PendingValueColor;
                _view.ProductMarks[index].gameObject.SetActive(false);
            }
        }

        public IEnumerator MoveWindow(int row, int column)
        {
            ClearEquations();
            EndPositionQuiz();
            Vector3 start = _view.Kernel.position;
            Vector3 target = _inputAnchor.TransformPoint(new Vector3((column - 1) * 0.19f, (1 - row) * 0.17f, -0.10f));
            for (float time = 0; time < 0.45f; time += Time.unscaledDeltaTime)
            {
                _view.Kernel.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, time / 0.45f));
                yield return null;
            }
            _view.Kernel.SetPositionAndRotation(target, _inputAnchor.rotation);
            _calculation.SetWindow(row, column);
        }

        public IEnumerator RecordWindowOutput()
        {
            int index = _calculation.Row * 3 + _calculation.Column;
            int value = _calculation.Output(_calculation.Row, _calculation.Column);
            Vector3 source = transform.InverseTransformPoint(_view.Kernel.position) + Vector3.back * 0.06f;
            _view.Flying.localPosition = source;
            _view.FlyingLabel.text = value.ToString();
            _view.FlyingLabel.fontSize = 64;
            _view.FlyingLabel.color = CalculationAccent;
            _view.Flying.gameObject.SetActive(true);
            PlaceHighlight(_view.ProductHighlight, _view.Result, Cell(index) + Vector3.back * 0.075f);
            yield return Fly(source, ResultCellPosition(index), 0.55f);
            RecordProduct(index, value);
            _view.Flying.gameObject.SetActive(false);
            _view.ProductHighlight.gameObject.SetActive(false);
        }

        public void BeginPositionQuiz()
        {
            _positionQuiz = true;
            for (int index = 0; index < 9; index++)
            {
                _view.OutputButtons[index].interactable = true;
                _view.OutputTargets[index].raycastTarget = true;
                _view.OutputTargets[index].color = new Color(0.15f, 0.24f, 0.29f, 0.22f);
            }
        }

        public void EndPositionQuiz()
        {
            _positionQuiz = false;
            for (int index = 0; index < 9; index++)
            {
                _view.OutputButtons[index].interactable = false;
                _view.OutputTargets[index].raycastTarget = false;
                _view.OutputTargets[index].color = Color.clear;
            }
        }

        private void SelectOutput(int index)
        {
            if (_positionQuiz) { OutputSelected?.Invoke(index); }
        }

        public void Initialize(Font font)
        {
            // The prefab owns the presentation transform and editable layout anchors.
            _builder = new GeometryBuilder(transform, font);
            _view = _builder.Build();
            _view.Result.SetPositionAndRotation(_resultAnchor.position, _resultAnchor.rotation);
            _view.Slot.SetPositionAndRotation(_sumAnchor.position, _sumAnchor.rotation);
            _view.EquationPlate.SetPositionAndRotation(_equationAnchor.position, _equationAnchor.rotation);
            _view.SumEquationPlate.SetPositionAndRotation(_sumEquationAnchor.position, _sumEquationAnchor.rotation);
            _view.DropTarget.SetPositionAndRotation(_kernelTarget.position, _kernelTarget.rotation);
            _interaction = new KernelInteractionController(transform, _view, _kernelHome, _kernelTarget, _placementHint);
            _volume = gameObject.AddComponent<RgbLayerInteractionController>();
            if (isActiveAndEnabled) { _volume.PairHighlighted += HighlightSpatialPair; }
            _volume.Initialize(_view.Layers, _view.ParkedKernels, _view.ColourPatch,
                _view.SpatialGuide, _builder.Material);
            for (int index = 0; index < 9; index++)
            {
                int selected = index;
                _view.OutputButtons[index].onClick.AddListener(() => SelectOutput(selected));
            }
        }

        public void SetData(ConvolutionCalculation calculation)
        {
            _calculation = calculation;
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 25; i++)
                {
                    _view.Values[c, i].text = _calculation.GridInput(c, i).ToString();
                    float strength = 0.30f + _calculation.GridInput(c, i) / 3f * 0.35f;
                    Color color = Color.Lerp(new Color(0.05f, 0.075f, 0.09f), ChannelColor(c), strength);
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
                    layer.localPosition = Vector3.Lerp(target + Vector3.up * 0.16f, target, t);
                    layer.localScale = Vector3.one * Mathf.Lerp(0.85f, 1, t);
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
            Vector3 kernelStart = _view.ParkedKernels[channel].localPosition;
            _interaction.PrepareKernel();
            _view.Kernel.localPosition = kernelStart;
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
                _view.Kernel.localPosition = Vector3.Lerp(kernelStart, _kernelHome.localPosition, t);
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
            _view.Slot.gameObject.SetActive(true);
            _view.SumLabel.text = value.ToString();
        }

        internal void HideAnswer() { _view.Slot.gameObject.SetActive(false); }

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
                EndPositionQuiz();
                ClearCalculationFeedback();
                _view.GripArrow.gameObject.SetActive(false);
                _view.DropTarget.gameObject.SetActive(false);
                _view.Products[4].enabled = true;
            }
            if (_volume != null) { _volume.StopInteraction(); }
            _interaction?.DisableKernel();
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
                    Frame(_view.Layers[c], 0.97f, 0.86f, 0.055f, new Color(0.22f, 0.28f, 0.31f), 0.012f);
                    Tile(_view.Layers[c], new Vector3(0, 0, 0.07f), new Vector3(0.97f, 0.86f, 0.05f), new Color(0.035f, 0.045f, 0.055f));
                    Text heading = Caption(_view.Layers[c], Names[c] + " 입력 · 5×5", new Vector3(0, 0.49f, 0), 1.0f);
                    heading.color = ChannelColor(c);
                    heading.fontSize = 44;
                    Text leftSide = Caption(_view.Layers[c], Names[c], new Vector3(-0.48f, 0.40f, 0), 0.16f);
                    leftSide.transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
                    Text rightSide = Caption(_view.Layers[c], Names[c], new Vector3(0.48f, 0.40f, 0), 0.16f);
                    rightSide.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
                    for (int i = 0; i < 25; i++)
                    {
                        Transform tile = Tile(_view.Layers[c], InputCell(i), new Vector3(0.17f, 0.15f, 0.12f), Color.gray);
                        Tint(tile.Find("Tile mesh").GetComponent<MeshRenderer>(), new Color(0.08f, 0.10f, 0.12f));
                        Transform face = Tile(tile, new Vector3(0, 0, -0.063f), new Vector3(0.158f, 0.138f, 0.012f), Color.gray);
                        _view.InputFaces[c, i] = face.Find("Tile mesh").GetComponent<MeshRenderer>();
                        // A neutral number inset keeps zero and bright green equally readable.
                        Tile(tile, new Vector3(0, -0.008f, -0.073f), new Vector3(0.095f, 0.105f, 0.008f), new Color(0.025f, 0.035f, 0.045f));
                        _view.Values[c, i] = Caption(tile, "", new Vector3(0, -0.008f, -0.08f), 0.095f);
                        _view.Values[c, i].fontSize = 48;
                    }
                    _view.ParkedKernels[c] = Root(Names[c] + " fixed filter slice");
                    FilterFrame(_view.ParkedKernels[c]);
                    // Only the parked stack has a backing; the movable filter must reveal the input beneath it.
                    Tile(_view.ParkedKernels[c], new Vector3(0, 0, 0.03f), new Vector3(0.69f, 0.65f, 0.02f), new Color(0.055f, 0.065f, 0.075f));
                    Caption(_view.ParkedKernels[c], Names[c] + " 가중치", new Vector3(0, 0.4f, 0), 0.6f);
                    for (int i = 0; i < 9; i++)
                    {
                        Transform tile = Tile(_view.ParkedKernels[c], Cell(i), new Vector3(0.17f, 0.15f, 0.025f), new Color(0.10f, 0.13f, 0.15f));
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
                _view.KernelTitle = Caption(_view.Kernel, "3×3 필터", new Vector3(0, -0.36f, 0), 0.7f);
                _view.KernelTitle.fontSize = 40;
                _view.GripArrow = Root("Grip arrow");
                _view.GripArrow.SetParent(_view.Kernel, false);
                _view.GripArrow.localScale = Vector3.one * 0.45f;
                _view.GripArrow.localPosition = new Vector3(0, 0.41f, -0.04f);
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
                    Transform tile = Tile(_view.Kernel, Cell(i) + new Vector3(0.052f, 0.052f, 0), new Vector3(0.045f, 0.045f, 0.025f), new Color(0.11f, 0.15f, 0.17f));
                    _view.Weights[i] = Caption(tile, "", new Vector3(0, 0, -0.018f), 0.045f);
                    _view.Weights[i].fontSize = 32;
                }
                _view.Result = Root("Products at matching positions");
                _view.Result.localPosition = new Vector3(0.45f, 0, 0.1f);
                Frame(_view.Result, 0.64f, 0.57f, 0.08f, new Color(0.28f, 0.31f, 0.32f));
                Tile(_view.Result, new Vector3(0, 0, 0.065f), new Vector3(0.60f, 0.53f, 0.035f), new Color(0.055f, 0.065f, 0.075f));
                _view.ResultTitle = Caption(_view.Result, "같은 위치의 곱", new Vector3(0, 0.34f, 0), 0.95f);
                _view.ResultTitle.fontSize = 42;
                for (int i = 0; i < 9; i++)
                {
                    Transform productTile = Tile(_view.Result, Cell(i), new Vector3(0.17f, 0.15f, 0.12f), new Color(0.16f, 0.18f, 0.22f));
                    _view.Products[i] = Caption(productTile, "·", new Vector3(0, 0, -0.066f), 0.17f);
                    _view.Products[i].color = PendingValueColor;
                    GameObject hitSurface = _view.Products[i].transform.parent.gameObject;
                    RectTransform hitRect = (RectTransform)hitSurface.transform;
                    hitRect.sizeDelta = new Vector2(170, 150);
                    Image target = hitSurface.AddComponent<Image>();
                    target.color = Color.clear;
                    target.raycastTarget = false;
                    hitSurface.AddComponent<GraphicRaycaster>();
                    hitSurface.AddComponent<TrackedDeviceGraphicRaycaster>();
                    Button button = hitSurface.AddComponent<Button>();
                    button.targetGraphic = target;
                    ColorBlock colors = button.colors;
                    colors.normalColor = new Color(0.10f, 0.21f, 0.25f, 0.2f);
                    colors.highlightedColor = new Color(0.95f, 0.75f, 0.40f, 0.65f);
                    colors.pressedColor = new Color(1f, 0.85f, 0.57f, 0.8f);
                    colors.selectedColor = colors.highlightedColor;
                    colors.disabledColor = Color.clear;
                    button.colors = colors;
                    button.interactable = false;
                    _view.OutputTargets[i] = target;
                    _view.OutputButtons[i] = button;
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
                _view.SumLabel.fontSize = 42;
                _view.EquationPlate = FormulaPlate("One cell multiplication plate", 0.9f);
                _view.Equation = Caption(_view.EquationPlate, "", new Vector3(0, 0, -0.027f), 0.86f);
                _view.Equation.fontSize = 48;
                _view.SumEquationPlate = FormulaPlate("Channel and RGB sum plate", 1.12f);
                _view.SumEquation = Caption(_view.SumEquationPlate, "", new Vector3(0, 0, -0.027f), 1.08f);
                _view.SumEquation.fontSize = 42;
                _view.InputHighlight = Highlight("Active input cell", 0.178f, 0.158f);
                _view.WeightHighlight = Highlight("Active weight cell", 0.049f, 0.049f);
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
                for (int index = 0; index < 9; index++)
                {
                    _view.SumCopies[index] = Root("Product sum copy " + index);
                    _view.SumCopyLabels[index] = Caption(_view.SumCopies[index], "", Vector3.zero, 0.17f);
                    _view.SumCopyLabels[index].fontSize = 52;
                    _view.SumCopies[index].gameObject.SetActive(false);
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

            public static Vector3 InputCell(int index)
            {
                return new Vector3((index % 5 - 2) * 0.19f, (2 - index / 5) * 0.17f, 0);
            }

            private Transform FormulaPlate(string name, float width)
            {
                Transform plate = Root(name);
                Tile(plate, Vector3.zero, new Vector3(width, 0.14f, 0.04f), new Color(0.025f, 0.035f, 0.045f));
                Frame(plate, width, 0.14f, 0.043f, new Color(0.44f, 0.37f, 0.25f), 0.008f);
                return plate;
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
                // Leave the input numerals visible when the offset filter lies over them.
                Frame(parent, 0.57f, 0.51f, 0.026f, metal, 0.012f);
                for (int side = -1; side <= 1; side += 2)
                {
                    Tile(parent, new Vector3(side * 0.075f, 0.282f, 0), new Vector3(0.018f, 0.048f, 0.035f), metal);
                }
                Tile(parent, new Vector3(0, 0.32f, 0), new Vector3(0.22f, 0.05f, 0.05f), new Color(0.08f, 0.10f, 0.11f));
                MeshRenderer[] accents = new MeshRenderer[5];
                for (int i = -2; i <= 2; i++)
                {
                    Transform groove = Tile(parent, new Vector3(i * 0.035f, 0.32f, -0.028f), new Vector3(0.006f, 0.035f, 0.008f), metal);
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
                        Tile(_view.PlacementDashed, new Vector3(segment * 0.11f, side * 0.27f, -0.035f),
                            new Vector3(0.060f, 0.008f, 0.010f), waiting);
                        Tile(_view.PlacementDashed, new Vector3(side * 0.30f, segment * 0.10f, -0.035f),
                            new Vector3(0.008f, 0.055f, 0.010f), waiting);
                    }
                }
                Frame(_view.PlacementSolid, 0.60f, 0.54f, 0.014f, ready, 0.008f);
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
                if (parent.name == "Number tile" && parent.Find("Tile mesh").localScale.z >= 0.08f)
                {
                    // Only thick input/result blocks have room for readable side numerals.
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
            public readonly Text[,] Values = new Text[3, 25];
            public readonly MeshRenderer[,] InputFaces = new MeshRenderer[3, 25];
            public readonly Text[,] VolumeWeightLabels = new Text[3, 9];
            public readonly Text[] Weights = new Text[9];
            public readonly Text[] Products = new Text[9];
            public readonly Transform[] ProductMarks = new Transform[9];
            public readonly Button[] OutputButtons = new Button[9];
            public readonly Image[] OutputTargets = new Image[9];
            public readonly Transform[] SumCopies = new Transform[9];
            public readonly Text[] SumCopyLabels = new Text[9];
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
            public Text ResultTitle;
            public Transform EquationPlate;
            public Transform SumEquationPlate;
            public Text Equation;
            public Text SumEquation;
            public Text KernelTitle;
            public Text SpatialGuide;
        }
    }
}
