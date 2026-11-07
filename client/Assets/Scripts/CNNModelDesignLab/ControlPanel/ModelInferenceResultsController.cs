using System;
using System.Collections.Generic;
using System.Globalization;
using ModelDesignScene.Dataset;
using ModelDesignScene.Model;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    public sealed class ModelInferenceResultsController : MonoBehaviour
    {
        private const int ClassCount = LayerLimits.OutputClassCount;

        [SerializeField] private Button[] _sampleButtons = Array.Empty<Button>();
        [SerializeField] private RawImage[] _sampleThumbnails = Array.Empty<RawImage>();
        [SerializeField] private Text[] _sampleLabels = Array.Empty<Text>();
        [SerializeField] private Text[] _probabilityLabels = Array.Empty<Text>();
        [SerializeField] private Image[] _probabilityBars = Array.Empty<Image>();
        [SerializeField] private Text _sampleHint;
        [SerializeField] private Text _pageText;
        [SerializeField] private Text _selectedSampleText;
        [SerializeField] private Text _predictionText;
        [SerializeField] private Text _predictionHint;
        [SerializeField] private RawImage _selectedPreview;
        [SerializeField] private Button _previousPageButton;
        [SerializeField] private Button _nextPageButton;
        [SerializeField] private Color _sampleColor = new Color(0.12f, 0.18f, 0.24f);
        [SerializeField] private Color _selectedSampleColor = new Color(0.14f, 0.43f, 0.51f);

        private readonly List<DatasetSample> _availableSamples = new List<DatasetSample>();
        private UnityAction[] _sampleListeners;
        private DatasetSample _selectedSample;
        private int _page;
        private bool _canInfer;
        private bool _hasTrainedModel;
        private bool _isAwaitingInference;
        private bool _initialized;

        internal event Action<string> OnInferenceRequested;
        internal event Action<string> OnError;

        internal void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            _sampleListeners = new UnityAction[_sampleButtons.Length];
            for (int index = 0; index < _sampleButtons.Length; index++)
            {
                int slot = index;
                _sampleListeners[index] = () => SelectSample(slot);
                _sampleButtons[index].onClick.AddListener(_sampleListeners[index]);
            }
            _previousPageButton.onClick.AddListener(ShowPreviousPage);
            _nextPageButton.onClick.AddListener(ShowNextPage);
            ResetTraining();
            ResetSession();
        }

        internal void SetSamples(IReadOnlyList<DatasetSample> samples)
        {
            _availableSamples.Clear();
            _availableSamples.AddRange(samples);
            ResetSession();
        }

        internal void ResetTraining()
        {
            _canInfer = false;
            _hasTrainedModel = false;
            ClearPrediction();
            RefreshSamples();
        }

        internal void ResetSession()
        {
            _selectedSample = null;
            _selectedPreview.texture = null;
            _selectedPreview.enabled = false;
            _selectedSampleText.text = "선택한 이미지 —\n정답 —";
            _page = 0;
            ClearPrediction();
            RefreshSamples();
        }

        internal void SetAvailability(bool hasTrainedModel, bool canInfer)
        {
            _hasTrainedModel = hasTrainedModel;
            _canInfer = canInfer && hasTrainedModel;
            RefreshSamples();
        }

        internal void ClearPendingPrediction()
        {
            if (_isAwaitingInference)
            {
                ClearPrediction();
                _predictionText.text = "예측 결과 없음";
                _predictionHint.text = "결과를 받지 못했습니다. 요청 상태를 확인한 뒤 이미지를 다시 선택하세요.";
            }
        }

        internal bool ShowInference(InferenceResult result)
        {
            if (result == null || _selectedSample == null
                || !string.Equals(result.ImageId, _selectedSample.Id, StringComparison.Ordinal))
            {
                return false;
            }
            if (result.Output == null || result.Output.Count != ClassCount)
            {
                RejectPrediction("숫자 0~3의 예측 결과를 모두 받지 못했습니다.");
                return false;
            }
            // Validate the complete response before changing any chart or prediction label.
            for (int index = 0; index < ClassCount; index++)
            {
                if (double.IsNaN(result.Output[index]) || double.IsInfinity(result.Output[index]))
                {
                    RejectPrediction("예측 결과에 유효하지 않은 숫자가 포함되어 있습니다. 다시 예측해 주세요.");
                    return false;
                }
            }
            int predicted = 0;
            for (int index = 0; index < ClassCount; index++)
            {
                if (result.Output[index] > result.Output[predicted])
                {
                    predicted = index;
                }
                // Preserve Softmax values from the model; only the drawing range is clamped.
                _probabilityLabels[index].text = index + "  " + FormatPercentage(result.Output[index]);
                SetProgress(_probabilityBars[index], (float)Math.Max(0, Math.Min(1, result.Output[index])));
            }
            bool knownLabel = _selectedSample.Label >= 0 && _selectedSample.Label < ClassCount;
            _predictionText.text = "예측 " + predicted + (knownLabel
                ? (predicted == _selectedSample.Label ? " · 정답" : " · 오답") : string.Empty);
            ShowPredictionHint(result, predicted, knownLabel);
            _isAwaitingInference = false;
            return true;
        }

        private void ShowPredictionHint(InferenceResult result, int predicted, bool knownLabel)
        {
            string confidence = FormatPercentage(result.Output[predicted]);
            if (!knownLabel)
            {
                _predictionHint.text = "가장 큰 출력은 숫자 " + predicted + "의 " + confidence
                    + "입니다. 이 이미지의 정답 정보가 없어 맞혔는지는 비교할 수 없습니다.";
                return;
            }
            if (predicted == _selectedSample.Label)
            {
                _predictionHint.text = "정답 " + predicted + "에 가장 큰 확률 " + confidence
                    + "를 주었습니다. 다른 샘플도 확인해 모델의 전체 정확도와 함께 비교하세요.";
                return;
            }
            string correctConfidence = FormatPercentage(result.Output[_selectedSample.Label]);
            _predictionHint.text = "정답 " + _selectedSample.Label + "에 " + correctConfidence + ", 예측 " + predicted
                + "에 " + confidence + "를 주었습니다.\n필터 수 또는 뉴런 수를 한 항목씩 바꾸고 재학습해 비교해 보세요.";
        }

        private void RejectPrediction(string message)
        {
            ClearPrediction();
            _predictionText.text = "예측 결과 확인 필요";
            _predictionHint.text = message;
            OnError?.Invoke(message);
        }

        private void RefreshSamples()
        {
            int samplesPerPage = _sampleButtons.Length;
            for (int index = 0; index < samplesPerPage; index++)
            {
                int sampleIndex = _page * samplesPerPage + index;
                bool available = sampleIndex < _availableSamples.Count;
                _sampleButtons[index].gameObject.SetActive(available);
                if (!available)
                {
                    continue;
                }
                DatasetSample sample = _availableSamples[sampleIndex];
                _sampleButtons[index].interactable = _canInfer;
                _sampleButtons[index].image.color = sample == _selectedSample ? _selectedSampleColor : _sampleColor;
                _sampleThumbnails[index].texture = sample.Image;
                _sampleLabels[index].text = "정답 " + FormatLabel(sample.Label);
            }
            int pages = GetPageCount();
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
            int index = _page * _sampleButtons.Length + slot;
            if (!_canInfer || index < 0 || index >= _availableSamples.Count || !isActiveAndEnabled)
            {
                return;
            }
            _selectedSample = _availableSamples[index];
            _selectedPreview.texture = _selectedSample.Image;
            _selectedPreview.enabled = true;
            _selectedSampleText.text = "이미지 " + _selectedSample.Id + "\n정답 " + FormatLabel(_selectedSample.Label);
            ClearPrediction();
            _predictionText.text = "예측 대기 중…";
            _predictionHint.text = "선택한 이미지에 대한 숫자별 확률을 기다리고 있습니다.";
            _isAwaitingInference = true;
            _canInfer = false;
            RefreshSamples();
            OnInferenceRequested?.Invoke(_selectedSample.Id);
        }

        private int GetPageCount()
        {
            return _sampleButtons.Length > 0
                ? Mathf.CeilToInt(_availableSamples.Count / (float)_sampleButtons.Length) : 0;
        }

        private void ShowPreviousPage()
        {
            ChangePage(-1);
        }

        private void ShowNextPage()
        {
            ChangePage(1);
        }

        private void ChangePage(int offset)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            _page = Mathf.Clamp(_page + offset, 0, Mathf.Max(0, GetPageCount() - 1));
            RefreshSamples();
        }

        private void ClearPrediction()
        {
            _isAwaitingInference = false;
            _predictionText.text = "예측 —";
            _predictionHint.text = "샘플을 선택하면 예측과 정답을 비교할 수 있습니다.";
            for (int index = 0; index < ClassCount; index++)
            {
                _probabilityLabels[index].text = index + "  —";
                SetProgress(_probabilityBars[index], 0f);
            }
        }

        private static void SetProgress(Image bar, float value)
        {
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }

        private static string FormatPercentage(double value)
        {
            return (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatLabel(int label)
        {
            return label >= 0 && label < ClassCount ? label.ToString(CultureInfo.InvariantCulture) : "—";
        }

        private void OnDestroy()
        {
            if (!_initialized)
            {
                return;
            }
            for (int index = 0; index < _sampleListeners.Length; index++)
            {
                if (_sampleButtons[index] != null)
                {
                    _sampleButtons[index].onClick.RemoveListener(_sampleListeners[index]);
                }
            }
            if (_previousPageButton != null)
            {
                _previousPageButton.onClick.RemoveListener(ShowPreviousPage);
            }
            if (_nextPageButton != null)
            {
                _nextPageButton.onClick.RemoveListener(ShowNextPage);
            }
        }
    }
}
