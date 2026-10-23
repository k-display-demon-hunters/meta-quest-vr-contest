using System;
using System.Collections.Generic;
using System.Globalization;
using CNNTraining;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene
{
    internal sealed class ModelInferenceResultsController
    {
        private const int SamplesPerPage = 10;
        private readonly ModelResultsUIBuilder _ui;
        private readonly Func<bool> _canInteract;
        private readonly List<ModelResultsController.SampleImage> _availableSamples = new List<ModelResultsController.SampleImage>();
        private readonly Button[] _sampleButtons = new Button[SamplesPerPage];
        private readonly RawImage[] _sampleThumbnails = new RawImage[SamplesPerPage];
        private readonly Text[] _sampleLabels = new Text[SamplesPerPage];
        private readonly Text[] _probabilityLabels = new Text[10];
        private readonly RectTransform[] _probabilityBars = new RectTransform[10];
        private Text _sampleHint;
        private Text _pageText;
        private Text _selectedSampleText;
        private Text _predictionText;
        private RawImage _selectedPreview;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private ModelResultsController.SampleImage _selectedSample;
        private int _page;
        private bool _canInfer;
        private bool _hasTrainedModel;
        private bool _isAwaitingInference;

        public event Action<string> OnInferenceRequested;
        public event Action<string> OnError;

        public ModelInferenceResultsController(RectTransform root, ModelResultsUIBuilder ui,
            ModelResultsController.SampleImage[] samples, Func<bool> canInteract)
        {
            _ui = ui;
            _canInteract = canInteract;
            BuildSamples(root);
            BuildPrediction(root);
            foreach (ModelResultsController.SampleImage sample in samples ?? Array.Empty<ModelResultsController.SampleImage>())
            {
                if (sample != null && sample.Image != null)
                {
                    _availableSamples.Add(sample);
                }
            }
        }

        public void ResetTraining()
        {
            _canInfer = false;
            _hasTrainedModel = false;
            ClearPrediction();
            RefreshSamples();
        }

        public void ResetSession()
        {
            _selectedSample = null;
            _selectedPreview.texture = null;
            _selectedPreview.enabled = false;
            _selectedSampleText.text = "선택한 이미지 —\n정답 —";
            _page = 0;
            RefreshSamples();
        }

        public void SetAvailability(bool hasTrainedModel, bool canInfer)
        {
            _hasTrainedModel = hasTrainedModel;
            _canInfer = canInfer && hasTrainedModel;
            RefreshSamples();
        }

        public void ClearPendingPrediction()
        {
            if (_isAwaitingInference)
            {
                _isAwaitingInference = false;
                _predictionText.text = "예측 결과 없음";
            }
        }

        public bool ShowInference(InferenceResult result)
        {
            if (result == null || _selectedSample == null
                || !string.Equals(result.ImageId, _selectedSample.Id, StringComparison.Ordinal))
            {
                return false;
            }
            if (result.Output == null || result.Output.Count != 10)
            {
                OnError?.Invoke("숫자 0~9의 예측 결과를 모두 받지 못했습니다.");
                return false;
            }
            int predicted = 0;
            for (int i = 0; i < result.Output.Count; i++)
            {
                if (result.Output[i] > result.Output[predicted])
                {
                    predicted = i;
                }
                // Preserve the model's Softmax output. Only the bar's drawing bounds are limited.
                _probabilityLabels[i].text = i + "  " + FormatPercentage(result.Output[i]);
                SetBarWidth(_probabilityBars[i], 232 * Mathf.Clamp01((float)result.Output[i]));
            }
            bool knownLabel = _selectedSample.Label >= 0 && _selectedSample.Label <= 9;
            _predictionText.text = "예측 " + predicted + (knownLabel
                ? (predicted == _selectedSample.Label ? " · 정답" : " · 오답") : string.Empty);
            _isAwaitingInference = false;
            return true;
        }

        private void BuildSamples(RectTransform root)
        {
            _ui.Label(root, "Sample title", "MNIST 샘플", 30, 644, 550, 32, 25);
            for (int i = 0; i < SamplesPerPage; i++)
            {
                int slot = i;
                float x = 30 + i % 5 * 112;
                float y = 689 + i / 5 * 126;
                Image surface = _ui.Box(root, "Sample " + i, x, y, 100, 112, ModelResultsUIBuilder.ButtonColor);
                Button button = _ui.CreateButton(surface, () => SelectSample(slot));
                _sampleButtons[i] = button;
                _sampleThumbnails[i] = _ui.Rect(surface.transform, "Image", 16, 8, 68, 68).gameObject.AddComponent<RawImage>();
                _sampleThumbnails[i].raycastTarget = false;
                _sampleLabels[i] = _ui.Label(surface.transform, "Label", string.Empty, 4, 82, 92, 25, 18);
                _sampleLabels[i].alignment = TextAnchor.MiddleCenter;
            }
            _previousPageButton = _ui.TextButton(root, "이전", 30, 950, 100, () => ChangePage(-1));
            _pageText = _ui.Label(root, "Page", string.Empty, 140, 950, 332, 36, 19);
            _pageText.alignment = TextAnchor.MiddleCenter;
            _nextPageButton = _ui.TextButton(root, "다음", 478, 950, 100, () => ChangePage(1));
            _sampleHint = _ui.Label(root, "Sample hint", string.Empty, 30, 993, 565, 32, 18);
            _sampleHint.color = ModelResultsUIBuilder.MutedColor;
        }

        private void BuildPrediction(RectTransform root)
        {
            _ui.Label(root, "Prediction title", "선택 이미지 · 예측", 640, 644, 530, 32, 25);
            _ui.Box(root, "Preview background", 640, 694, 128, 128, Color.black);
            _selectedPreview = _ui.Rect(root, "Preview", 640, 694, 128, 128).gameObject.AddComponent<RawImage>();
            _selectedPreview.raycastTarget = false;
            _selectedSampleText = _ui.Label(root, "Selected sample", string.Empty, 788, 694, 382, 65, 22);
            _selectedSampleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _selectedSampleText.resizeTextForBestFit = true;
            _selectedSampleText.resizeTextMinSize = 16;
            _selectedSampleText.resizeTextMaxSize = 22;
            _predictionText = _ui.Label(root, "Prediction", string.Empty, 788, 774, 382, 40, 29);
            _predictionText.color = ModelResultsUIBuilder.TrainingColor;
            _ui.Label(root, "Probability title", "숫자별 확률", 640, 833, 530, 28, 19).color = ModelResultsUIBuilder.MutedColor;
            for (int i = 0; i < 10; i++)
            {
                float x = 640 + i / 5 * 282;
                float y = 868 + i % 5 * 32;
                _probabilityLabels[i] = _ui.Label(root, "Probability " + i, string.Empty, x, y, 248, 22, 18);
                Image track = _ui.Box(root, "Probability track " + i, x, y + 23, 232, 4, ModelResultsUIBuilder.ButtonColor);
                _probabilityBars[i] = _ui.Box(track.transform, "Value", 0, 0, 0, 4, ModelResultsUIBuilder.TrainingColor).rectTransform;
            }
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
                ModelResultsController.SampleImage sample = _availableSamples[index];
                _sampleButtons[i].interactable = _canInfer;
                _sampleButtons[i].image.color = sample == _selectedSample ? new Color(0.14f, 0.43f, 0.51f) : ModelResultsUIBuilder.ButtonColor;
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
            if (!_canInfer || index < 0 || index >= _availableSamples.Count || !_canInteract())
            {
                return;
            }
            _selectedSample = _availableSamples[index];
            _selectedPreview.texture = _selectedSample.Image;
            _selectedPreview.enabled = true;
            _selectedSampleText.text = "이미지 " + _selectedSample.Id + "\n정답 " + FormatLabel(_selectedSample.Label);
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

        private static string FormatPercentage(double value)
        {
            return (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatLabel(int label)
        {
            return label >= 0 && label <= 9 ? label.ToString(CultureInfo.InvariantCulture) : "—";
        }

        private static void SetBarWidth(RectTransform bar, float width)
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }
}
