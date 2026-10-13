using System;
using System.Collections.Generic;
using System.Globalization;
using Networking;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ModelDesignScene
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ModelResultsController : MonoBehaviour
    {
        [Serializable]
        private sealed class SampleImage
        {
            [SerializeField] private Texture2D _image;
            [SerializeField] private int _label;

            public Texture2D Image => _image;
            public int Label => _label;
            public string Id => _image.name;
        }

        private const int SamplesPerPage = 10;
        private static readonly Color SurfaceColor = new Color(0.065f, 0.085f, 0.12f);
        private static readonly Color MutedColor = new Color(0.69f, 0.75f, 0.83f);
        private static readonly Color TrainingColor = new Color(0.27f, 0.81f, 0.91f);
        private static readonly Color ValidationColor = new Color(1f, 0.72f, 0.29f);
        private static readonly Color ButtonColor = new Color(0.16f, 0.21f, 0.29f);

        [SerializeField] private Font _font;
        [SerializeField] private SampleImage[] _samples = Array.Empty<SampleImage>();

        private readonly List<SampleImage> _availableSamples = new List<SampleImage>();
        private readonly Button[] _sampleButtons = new Button[SamplesPerPage];
        private readonly RawImage[] _sampleThumbnails = new RawImage[SamplesPerPage];
        private readonly Text[] _sampleLabels = new Text[SamplesPerPage];
        private readonly Text[] _probabilityLabels = new Text[10];
        private readonly RectTransform[] _probabilityBars = new RectTransform[10];
        private Text _stateText;
        private Text _errorText;
        private Text _modelChangedText;
        private Text _epochText;
        private Text _trainingMetrics;
        private Text _validationMetrics;
        private Text _evaluationMetrics;
        private Text _accuracyAxis;
        private Text _lossAxis;
        private Text _sampleHint;
        private Text _pageText;
        private Text _selectedSampleText;
        private Text _predictionText;
        private RawImage _selectedPreview;
        private RectTransform _epochBar;
        private ModelMetricGraph _accuracyGraph;
        private ModelMetricGraph _lossGraph;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private SampleImage _selectedSample;
        private int _page;
        private bool _canInfer;
        private bool _hasTrainedModel;
        private bool _isAwaitingInference;

        public event Action<string> OnInferenceRequested;

        private void Awake()
        {
            BuildInterface();
            foreach (SampleImage sample in _samples ?? Array.Empty<SampleImage>())
            {
                if (sample != null && sample.Image != null)
                {
                    _availableSamples.Add(sample);
                }
            }
            ResetSessionUI();
        }

        public void ResetTraining()
        {
            _accuracyGraph.Clear();
            _lossGraph.Clear();
            _epochText.text = "에포크 — / — · 서버의 진행 정보를 기다리는 중";
            SetBarWidth(_epochBar, 0);
            _trainingMetrics.text = "학습  정확도 —   손실 —";
            _validationMetrics.text = "검증  정확도 —   손실 —";
            _evaluationMetrics.text = "최종 테스트  정확도 —   손실 —";
            _accuracyAxis.text = "0–100% · 에포크 —";
            _lossAxis.text = "손실 범위 — · 에포크 —";
            _errorText.text = string.Empty;
            _canInfer = false;
            _hasTrainedModel = false;
            ClearPrediction();
            RefreshSamples();
        }

        public void ResetSessionUI()
        {
            ResetTraining();
            _selectedSample = null;
            _selectedPreview.texture = null;
            _selectedPreview.enabled = false;
            _selectedSampleText.text = "선택한 이미지 —\n정답 —";
            _stateText.text = "대기 · 컨베이어 왼쪽에서 학습을 시작하세요.";
            SetModelChanged(false);
            _page = 0;
            RefreshSamples();
        }

        public void ShowProgress(TrainingProgress progress)
        {
            if (progress == null)
            {
                return;
            }
            ShowEpoch(progress.Epoch, progress.Epochs, progress.Logs);
        }

        public void ShowCompleted(TrainingResult result)
        {
            if (result == null)
            {
                return;
            }
            // The server sends the final epoch only in training_completed, including one-epoch runs.
            ShowEpoch(result.Epoch, result.Epoch, result.Logs);
            _evaluationMetrics.text = "최종 테스트  정확도 " + FormatMetric(result.Evaluation, "accuracy", true)
                + "   손실 " + FormatMetric(result.Evaluation, "loss", false);
            _stateText.text = "학습 완료 · MNIST 샘플을 선택해 예측을 확인하세요.";
            _errorText.text = string.Empty;
        }

        public void ShowInference(InferenceResult result)
        {
            if (result == null || _selectedSample == null
                || !string.Equals(result.ImageId, _selectedSample.Id, StringComparison.Ordinal))
            {
                return;
            }
            if (result.Output == null || result.Output.Count != 10)
            {
                ShowError("숫자 0~9의 예측 결과를 모두 받지 못했습니다.");
                return;
            }
            int predicted = 0;
            for (int i = 0; i < result.Output.Count; i++)
            {
                if (result.Output[i] > result.Output[predicted])
                {
                    predicted = i;
                }
                // Preserve the server's Softmax output. Only the bar's drawing bounds are limited.
                _probabilityLabels[i].text = i + "  " + FormatPercentage(result.Output[i]);
                SetBarWidth(_probabilityBars[i], 232 * Mathf.Clamp01((float)result.Output[i]));
            }
            bool knownLabel = _selectedSample.Label >= 0 && _selectedSample.Label <= 9;
            _predictionText.text = "예측 " + predicted + (knownLabel
                ? (predicted == _selectedSample.Label ? " · 정답" : " · 오답") : string.Empty);
            _isAwaitingInference = false;
            _errorText.text = string.Empty;
        }

        public void ShowState(ModelExecutionState state, bool hasTrainedModel, bool canInfer)
        {
            _hasTrainedModel = hasTrainedModel;
            _canInfer = canInfer && hasTrainedModel;
            switch (state)
            {
                case ModelExecutionState.Connecting:
                    _stateText.text = "학습 서버에 연결하는 중…";
                    break;
                case ModelExecutionState.StartingTraining:
                    _stateText.text = "모델을 전달했습니다. 학습 시작을 기다리는 중…";
                    break;
                case ModelExecutionState.Training:
                    _stateText.text = "학습 중 · 수신한 에포크의 지표를 표시합니다.";
                    break;
                case ModelExecutionState.CancellingTraining:
                    _stateText.text = "학습 취소를 요청했습니다. 서버 응답을 기다리는 중…";
                    break;
                case ModelExecutionState.Completed:
                    _stateText.text = "학습 완료 · MNIST 샘플을 선택해 예측을 확인하세요.";
                    break;
                case ModelExecutionState.Inferring:
                    _stateText.text = "선택한 이미지의 예측을 기다리는 중…";
                    break;
                case ModelExecutionState.Cancelled:
                    _stateText.text = "학습이 취소되었습니다. 다시 학습을 시작할 수 있습니다.";
                    break;
                case ModelExecutionState.Failed:
                    _stateText.text = "요청을 완료하지 못했습니다. 아래 안내를 확인하세요.";
                    break;
                default:
                    _stateText.text = hasTrainedModel ? "학습 모델 준비됨 · 샘플을 선택하세요."
                        : "대기 · 컨베이어 왼쪽에서 학습을 시작하세요.";
                    break;
            }
            RefreshSamples();
        }

        public void ShowError(string message)
        {
            _errorText.text = string.IsNullOrWhiteSpace(message) ? "요청을 완료하지 못했습니다." : message;
            if (_isAwaitingInference)
            {
                _isAwaitingInference = false;
                _predictionText.text = "예측 결과 없음";
            }
        }

        public void ShowCancelled()
        {
            _canInfer = false;
            _hasTrainedModel = false;
            _stateText.text = "학습 취소됨 · 그래프에는 취소 전 수신한 지표만 남아 있습니다.";
            _errorText.text = string.Empty;
            ClearPrediction();
            RefreshSamples();
        }

        public void SetModelChanged(bool changed)
        {
            _modelChangedText.text = changed
                ? "구성이 변경되었습니다. 표시된 결과는 마지막 학습을 시작했을 때의 모델 기준입니다."
                : string.Empty;
        }

        private void ShowEpoch(int epoch, int totalEpochs, IReadOnlyDictionary<string, double> logs)
        {
            _epochText.text = "에포크 " + epoch + " / " + totalEpochs;
            SetBarWidth(_epochBar, totalEpochs > 0 ? 1140 * Mathf.Clamp01((float)epoch / totalEpochs) : 0);
            _trainingMetrics.text = "학습  정확도 " + FormatMetric(logs, "accuracy", true)
                + "   손실 " + FormatMetric(logs, "loss", false);
            _validationMetrics.text = "검증  정확도 " + FormatMetric(logs, "val_accuracy", true)
                + "   손실 " + FormatMetric(logs, "val_loss", false);
            _accuracyGraph.AddPoint(epoch, totalEpochs, ReadMetric(logs, "accuracy"), ReadMetric(logs, "val_accuracy"));
            _lossGraph.AddPoint(epoch, totalEpochs, ReadMetric(logs, "loss"), ReadMetric(logs, "val_loss"));
            _accuracyAxis.text = "0–100% · 가로축 에포크 0–" + totalEpochs;
            _lossAxis.text = FormatNumber(_lossGraph.Minimum) + "–" + FormatNumber(_lossGraph.Maximum)
                + " · 가로축 에포크 0–" + totalEpochs;
        }

        private void BuildInterface()
        {
            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(1200, 1050);
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            TrackedDeviceGraphicRaycaster raycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            raycaster.enabled = true;
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.dynamicPixelsPerUnit = 4;
            Box(root, "Surface", 0, 0, 1200, 1050, SurfaceColor);
            Label(root, "Title", "모델 학습 결과", 30, 20, 900, 46, 34);
            _stateText = Label(root, "State", string.Empty, 30, 78, 1140, 32, 24);
            _errorText = Label(root, "Error", string.Empty, 30, 115, 1140, 48, 19);
            _errorText.color = new Color(1, 0.55f, 0.49f);
            _errorText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _errorText.resizeTextForBestFit = true;
            _errorText.resizeTextMinSize = 14;
            _errorText.resizeTextMaxSize = 19;
            _modelChangedText = Label(root, "Model changed", string.Empty, 30, 168, 1140, 30, 19);
            _modelChangedText.color = ValidationColor;
            _epochText = Label(root, "Epoch", string.Empty, 30, 204, 1140, 28, 22);
            Image progressTrack = Box(root, "Epoch progress", 30, 242, 1140, 10, ButtonColor);
            _epochBar = Box(progressTrack.transform, "Progress", 0, 0, 0, 10, TrainingColor).rectTransform;
            _trainingMetrics = Label(root, "Training metrics", string.Empty, 30, 270, 555, 36, 24);
            _trainingMetrics.color = TrainingColor;
            _validationMetrics = Label(root, "Validation metrics", string.Empty, 615, 270, 555, 36, 24);
            _validationMetrics.color = ValidationColor;
            _evaluationMetrics = Label(root, "Final test metrics", string.Empty, 30, 320, 1140, 40, 27);
            Label(root, "Accuracy title", "정확도", 30, 376, 280, 28, 23);
            Label(root, "Loss title", "손실", 618, 376, 280, 28, 23);
            Label(root, "Training legend", "━ 학습", 877, 376, 130, 28, 21).color = TrainingColor;
            Label(root, "Validation legend", "━ 검증", 1020, 376, 150, 28, 21).color = ValidationColor;
            _accuracyGraph = CreateGraph(root, "Accuracy chart", 30, true);
            _lossGraph = CreateGraph(root, "Loss chart", 618, false);
            _accuracyAxis = Label(root, "Accuracy axis", string.Empty, 30, 582, 555, 28, 18);
            _lossAxis = Label(root, "Loss axis", string.Empty, 618, 582, 552, 28, 18);
            _accuracyAxis.color = _lossAxis.color = MutedColor;
            Box(root, "Section divider", 30, 625, 1140, 2, ButtonColor);
            BuildSamples(root);
            BuildPrediction(root);
        }

        private void BuildSamples(RectTransform root)
        {
            Label(root, "Sample title", "MNIST 샘플", 30, 644, 550, 32, 25);
            for (int i = 0; i < SamplesPerPage; i++)
            {
                int slot = i;
                float x = 30 + i % 5 * 112;
                float y = 689 + i / 5 * 126;
                Image surface = Box(root, "Sample " + i, x, y, 100, 112, ButtonColor);
                Button button = CreateButton(surface, () => SelectSample(slot));
                _sampleButtons[i] = button;
                _sampleThumbnails[i] = Rect(surface.transform, "Image", 16, 8, 68, 68).gameObject.AddComponent<RawImage>();
                _sampleThumbnails[i].raycastTarget = false;
                _sampleLabels[i] = Label(surface.transform, "Label", string.Empty, 4, 82, 92, 25, 18);
                _sampleLabels[i].alignment = TextAnchor.MiddleCenter;
            }
            _previousPageButton = TextButton(root, "이전", 30, 950, 100, () => ChangePage(-1));
            _pageText = Label(root, "Page", string.Empty, 140, 950, 332, 36, 19);
            _pageText.alignment = TextAnchor.MiddleCenter;
            _nextPageButton = TextButton(root, "다음", 478, 950, 100, () => ChangePage(1));
            _sampleHint = Label(root, "Sample hint", string.Empty, 30, 993, 565, 32, 18);
            _sampleHint.color = MutedColor;
        }

        private void BuildPrediction(RectTransform root)
        {
            Label(root, "Prediction title", "선택 이미지 · 예측", 640, 644, 530, 32, 25);
            Box(root, "Preview background", 640, 694, 128, 128, Color.black);
            _selectedPreview = Rect(root, "Preview", 640, 694, 128, 128).gameObject.AddComponent<RawImage>();
            _selectedPreview.raycastTarget = false;
            _selectedSampleText = Label(root, "Selected sample", string.Empty, 788, 694, 382, 65, 22);
            _selectedSampleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _selectedSampleText.resizeTextForBestFit = true;
            _selectedSampleText.resizeTextMinSize = 16;
            _selectedSampleText.resizeTextMaxSize = 22;
            _predictionText = Label(root, "Prediction", string.Empty, 788, 774, 382, 40, 29);
            _predictionText.color = TrainingColor;
            Label(root, "Probability title", "숫자별 확률", 640, 833, 530, 28, 19).color = MutedColor;
            for (int i = 0; i < 10; i++)
            {
                float x = 640 + i / 5 * 282;
                float y = 868 + i % 5 * 32;
                _probabilityLabels[i] = Label(root, "Probability " + i, string.Empty, x, y, 248, 22, 18);
                Image track = Box(root, "Probability track " + i, x, y + 23, 232, 4, ButtonColor);
                _probabilityBars[i] = Box(track.transform, "Value", 0, 0, 0, 4, TrainingColor).rectTransform;
            }
        }

        private ModelMetricGraph CreateGraph(Transform parent, string name, float x, bool accuracy)
        {
            RectTransform rect = Rect(parent, name, x, 414, 552, 160);
            ModelMetricGraph graph = rect.gameObject.AddComponent<ModelMetricGraph>();
            graph.Initialize(accuracy, TrainingColor, ValidationColor);
            return graph;
        }

        private void RefreshSamples()
        {
            for (int i = 0; i < SamplesPerPage; i++)
            {
                int index = _page * SamplesPerPage + i;
                bool available = index < _availableSamples.Count;
                _sampleButtons[i].gameObject.SetActive(available);
                if (!available)
                {
                    continue;
                }
                SampleImage sample = _availableSamples[index];
                _sampleButtons[i].interactable = _canInfer;
                _sampleButtons[i].image.color = sample == _selectedSample ? new Color(0.14f, 0.43f, 0.51f) : ButtonColor;
                _sampleThumbnails[i].texture = sample.Image;
                _sampleLabels[i].text = "정답 " + FormatLabel(sample.Label);
            }
            int pages = Mathf.CeilToInt(_availableSamples.Count / (float)SamplesPerPage);
            _previousPageButton.interactable = _page > 0;
            _nextPageButton.interactable = _page + 1 < pages;
            _pageText.text = pages > 0 ? (_page + 1) + " / " + pages : "샘플 없음";
            _sampleHint.text = _availableSamples.Count == 0 ? "등록된 샘플 이미지가 없습니다."
                : _canInfer ? "이미지를 선택하면 학습된 모델로 예측합니다."
                : _hasTrainedModel ? "현재 요청이 끝나면 다음 이미지를 선택하세요."
                : "학습 완료 후 샘플 예측을 사용할 수 있습니다.";
        }

        private void SelectSample(int slot)
        {
            int index = _page * SamplesPerPage + slot;
            if (!_canInfer || index < 0 || index >= _availableSamples.Count || !isActiveAndEnabled)
            {
                return;
            }
            _selectedSample = _availableSamples[index];
            _selectedPreview.texture = _selectedSample.Image;
            _selectedPreview.enabled = true;
            _selectedSampleText.text = "이미지 " + _selectedSample.Id + "\n정답 " + FormatLabel(_selectedSample.Label);
            _errorText.text = string.Empty;
            ClearPrediction();
            _predictionText.text = "예측 대기 중…";
            _isAwaitingInference = true;
            _canInfer = false;
            RefreshSamples();
            OnInferenceRequested?.Invoke(_selectedSample.Id);
        }

        private void ChangePage(int offset)
        {
            int pages = Mathf.CeilToInt(_availableSamples.Count / (float)SamplesPerPage);
            _page = Mathf.Clamp(_page + offset, 0, Mathf.Max(0, pages - 1));
            RefreshSamples();
        }

        private void ClearPrediction()
        {
            _isAwaitingInference = false;
            _predictionText.text = "예측 —";
            for (int i = 0; i < 10; i++)
            {
                _probabilityLabels[i].text = i + "  —";
                SetBarWidth(_probabilityBars[i], 0);
            }
        }

        private static double? ReadMetric(IReadOnlyDictionary<string, double> metrics, string key)
        {
            if (metrics != null && metrics.TryGetValue(key, out double value)
                && !double.IsNaN(value) && !double.IsInfinity(value))
            {
                return value;
            }
            return null;
        }

        private static string FormatMetric(IReadOnlyDictionary<string, double> metrics, string key, bool percentage)
        {
            double? value = ReadMetric(metrics, key);
            return value.HasValue ? (percentage ? FormatPercentage(value.Value) : FormatNumber(value.Value)) : "—";
        }

        private static string FormatPercentage(double value)
        {
            return (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatNumber(double value)
        {
            double magnitude = Math.Abs(value);
            string format = magnitude > 0 && (magnitude < 0.0001 || magnitude >= 10000) ? "G5" : "0.####";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static string FormatLabel(int label)
        {
            return label >= 0 && label <= 9 ? label.ToString(CultureInfo.InvariantCulture) : "—";
        }

        private static void SetBarWidth(RectTransform bar, float width)
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Image Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size)
        {
            Text text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.text = value;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button TextButton(Transform parent, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            Image surface = Box(parent, label, x, y, width, 36, ButtonColor);
            Button button = CreateButton(surface, action);
            Text text = Label(surface.transform, "Label", label, 0, 0, width, 36, 19);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static Button CreateButton(Image surface, UnityEngine.Events.UnityAction action)
        {
            surface.raycastTarget = true;
            Button button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(action);
            return button;
        }
    }
}
