using System;
using System.Collections.Generic;
using ModelDesignScene.Dataset;
using ModelDesignScene.Model;
using ModelDesignScene.Execution;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ModelControlPanelController : MonoBehaviour
    {
        [Header("Saved screen")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _stateText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Image _statusAccent;
        [SerializeField] private GameObject _modelChangedNotice;
        [SerializeField] private GameObject[] _pages;
        [SerializeField] private Image[] _tabAccents;
        [SerializeField] private Button _validationTab;
        [SerializeField] private Button _trainingTab;
        [SerializeField] private Button _predictionTab;

        [Header("Actions and request errors")]
        [SerializeField] private Button _startTrainingButton;
        [SerializeField] private Button _stopTrainingButton;
        [SerializeField] private Text _startTrainingLabel;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private Text _errorText;
        [SerializeField] private ScrollRect _errorScroll;
        [SerializeField] private Button _dismissErrorButton;
        [SerializeField] private Button _errorValidationButton;

        [Header("Screen sections")]
        [SerializeField] private ModelValidationController _validation;
        [SerializeField] private ModelTrainingMetricsController _metrics;
        [SerializeField] private ModelInferenceResultsController _inference;

        [Header("Status colors")]
        [SerializeField] private Color _readyColor = new Color(0.25f, 0.84f, 0.69f);
        [SerializeField] private Color _problemColor = new Color(1f, 0.7f, 0.36f);
        [SerializeField] private Color _neutralColor = new Color(0.39f, 0.73f, 0.95f);

        private bool _hasIssues;
        private bool _isDragging;
        private bool _isBusy;

        internal event Action OnStartRequested;
        internal event Action OnStopRequested;
        internal event Action<string> OnInferenceRequested;

        private void Awake()
        {
            _canvas.worldCamera = Camera.main;
            _validationTab.onClick.AddListener(ShowValidationPage);
            _trainingTab.onClick.AddListener(ShowTrainingPage);
            _predictionTab.onClick.AddListener(ShowPredictionPage);
            _startTrainingButton.onClick.AddListener(HandleStartRequested);
            _stopTrainingButton.onClick.AddListener(HandleStopRequested);
            _dismissErrorButton.onClick.AddListener(DismissError);
            _errorValidationButton.onClick.AddListener(InspectModel);
            _metrics.Initialize();
            _inference.Initialize();
            _inference.OnInferenceRequested += HandleInferenceRequested;
            _inference.OnError += ShowError;
            ResetSessionUI();
        }

        internal void SetSamples(IReadOnlyList<DatasetSample> samples)
        {
            _inference.SetSamples(samples);
        }

        internal void ShowModelValidation(IReadOnlyList<ValidationIssue> issues,
            IReadOnlyList<LayerDefinition> slotLayers, bool isDragging, bool hasValidationResult, bool focus = false)
        {
            bool wasInvalid = _hasIssues && !_isDragging;
            _hasIssues = issues.Count > 0;
            _isDragging = isDragging;
            bool hasBlocks = false;
            foreach (LayerDefinition layer in slotLayers)
            {
                if (layer != null)
                {
                    hasBlocks = true;
                    break;
                }
            }
            bool showIntro = !isDragging && !_hasIssues && !hasBlocks;
            _validation.ShowValidation(issues, slotLayers, isDragging, hasValidationResult);
            _statusText.text = isDragging ? "블록 배치 중"
                : _hasIssues ? "수정 필요 · " + issues.Count + "곳"
                : showIntro ? "모델 만들기 안내" : hasValidationResult ? "실행 전 검사 완료" : "실행 전 검사 대기";
            _statusAccent.color = isDragging || showIntro || !hasValidationResult ? _neutralColor : _hasIssues ? _problemColor : _readyColor;
            RefreshStartButton();
            if (focus || (_hasIssues && !isDragging && !wasInvalid && !_isBusy))
            {
                ShowValidationPage();
            }
        }

        internal void ResetTraining()
        {
            _metrics.ResetTraining();
            DismissError();
            _inference.ResetTraining();
            ShowTrainingPage();
        }

        internal void ResetSessionUI()
        {
            _metrics.ResetTraining();
            _inference.ResetTraining();
            _inference.ResetSession();
            _isBusy = false;
            _isDragging = false;
            _hasIssues = false;
            _statusText.text = "모델 만들기 안내";
            _statusAccent.color = _neutralColor;
            _validation.ShowValidation(Array.Empty<ValidationIssue>(), Array.Empty<LayerDefinition>(), false, false);
            _stopTrainingButton.interactable = false;
            _stateText.text = "블록을 조립하고 순서를 확인한 뒤 학습을 시작하세요.";
            SetModelChanged(false);
            DismissError();
            RefreshStartButton();
            ShowValidationPage();
        }

        internal void ShowProgress(TrainingProgress progress)
        {
            _metrics.ShowProgress(progress);
        }

        internal void ShowCompleted(TrainingResult result)
        {
            if (result == null)
            {
                return;
            }
            _metrics.ShowCompleted(result);
            _stateText.text = "학습 완료 · 숫자 예측 화면에서 새로운 이미지를 확인하세요.";
            DismissError();
            ShowTrainingPage();
        }

        internal void ShowInference(InferenceResult result)
        {
            if (_inference.ShowInference(result))
            {
                DismissError();
                ShowPredictionPage();
            }
        }

        internal void ShowState(ModelExecutionState state, bool hasTrainedModel, bool canInfer, bool canCancel)
        {
            _isBusy = state == ModelExecutionState.StartingTraining
                || state == ModelExecutionState.Training || state == ModelExecutionState.CancellingTraining
                || state == ModelExecutionState.Inferring;
            switch (state)
            {
                case ModelExecutionState.StartingTraining:
                    _stateText.text = "모델 학습을 준비하는 중…";
                    break;
                case ModelExecutionState.Training:
                    _stateText.text = "학습 중 · 완료된 에포크의 지표를 표시합니다.";
                    break;
                case ModelExecutionState.CancellingTraining:
                    _stateText.text = "학습을 취소하는 중…";
                    break;
                case ModelExecutionState.Completed:
                    _stateText.text = "학습 완료 · 숫자 예측 화면에서 새로운 이미지를 확인하세요.";
                    break;
                case ModelExecutionState.Inferring:
                    _stateText.text = "선택한 이미지의 예측을 기다립니다…";
                    break;
                case ModelExecutionState.Cancelled:
                    _stateText.text = "학습을 중지했습니다. 구성을 바꾸거나 다시 시작할 수 있습니다.";
                    break;
                case ModelExecutionState.Failed:
                    _stateText.text = "요청 실패 · 화면에 표시된 원인과 안내를 확인하세요.";
                    break;
                default:
                    _stateText.text = hasTrainedModel ? "학습된 모델이 있습니다. 이미지를 선택해 예측해 보세요."
                        : "블록을 조립하고 순서를 확인한 뒤 학습을 시작하세요.";
                    break;
            }
            _stopTrainingButton.interactable = canCancel;
            _inference.SetAvailability(hasTrainedModel, canInfer);
            RefreshStartButton();
        }

        internal void ShowError(string message)
        {
            _errorText.text = string.IsNullOrWhiteSpace(message) ? "요청을 완료하지 못했습니다." : message;
            _errorPanel.SetActive(true);
            _errorScroll.StopMovement();
            _errorScroll.content.anchoredPosition = Vector2.zero;
            _inference.ClearPendingPrediction();
        }

        internal void ShowCancelled()
        {
            _stateText.text = "학습 취소됨 · 그래프에는 취소 전 완료된 에포크만 남아 있습니다.";
            DismissError();
            _inference.ResetTraining();
        }

        internal void SetModelChanged(bool changed)
        {
            _modelChangedNotice.SetActive(changed);
        }

        private void RefreshStartButton()
        {
            _startTrainingButton.interactable = !_isBusy && !_isDragging;
            _startTrainingLabel.text = _isDragging ? "블록을 먼저 놓아 주세요" : _hasIssues ? "구성 문제 확인하기" : "학습 시작";
        }

        private void HandleStartRequested()
        {
            if (isActiveAndEnabled && !_isBusy && !_isDragging)
            {
                DismissError();
                OnStartRequested?.Invoke();
            }
        }

        private void HandleStopRequested()
        {
            if (isActiveAndEnabled)
            {
                OnStopRequested?.Invoke();
            }
        }

        private void HandleInferenceRequested(string imageId)
        {
            DismissError();
            OnInferenceRequested?.Invoke(imageId);
        }

        private void ShowValidationPage() { ShowPage(0); }
        private void ShowTrainingPage() { ShowPage(1); }
        private void ShowPredictionPage() { ShowPage(2); }
        private void DismissError() { _errorPanel.SetActive(false); }

        private void InspectModel()
        {
            DismissError();
            ShowValidationPage();
        }

        private void ShowPage(int page)
        {
            for (int index = 0; index < _pages.Length; index++)
            {
                _pages[index].SetActive(index == page);
                _tabAccents[index].enabled = index == page;
            }
        }

        private void OnDestroy()
        {
            _validationTab.onClick.RemoveListener(ShowValidationPage);
            _trainingTab.onClick.RemoveListener(ShowTrainingPage);
            _predictionTab.onClick.RemoveListener(ShowPredictionPage);
            _startTrainingButton.onClick.RemoveListener(HandleStartRequested);
            _stopTrainingButton.onClick.RemoveListener(HandleStopRequested);
            _dismissErrorButton.onClick.RemoveListener(DismissError);
            _errorValidationButton.onClick.RemoveListener(InspectModel);
            _inference.OnInferenceRequested -= HandleInferenceRequested;
            _inference.OnError -= ShowError;
        }
    }
}
