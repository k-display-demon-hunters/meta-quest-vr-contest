using System;
using CNNTraining;
using System.Collections.Generic;
using ModelDesign;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ModelResultsController : MonoBehaviour
    {
        [Serializable]
        public sealed class SampleImage
        {
            [SerializeField] private Texture2D _image;
            [SerializeField] private int _label;

            public Texture2D Image => _image;
            public int Label => _label;
            public string Id => _image.name;
        }

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
        [SerializeField] private SampleImage[] _samples = Array.Empty<SampleImage>();

        [Header("Status colors")]
        [SerializeField] private Color _readyColor = new Color(0.25f, 0.84f, 0.69f);
        [SerializeField] private Color _problemColor = new Color(1f, 0.7f, 0.36f);
        [SerializeField] private Color _neutralColor = new Color(0.39f, 0.73f, 0.95f);

        private bool _hasIssues;
        private bool _isDragging;
        private bool _isBusy;
        private int _page;
        private Button _nextStep;
        private Text _nextStepLabel;
        public event Action OnLessonSkipped;

        public event Action OnStartRequested;
        public event Action OnStopRequested;
        public event Action<string> OnInferenceRequested;

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
            _inference.Initialize(_samples);
            _inference.OnInferenceRequested += HandleInferenceRequested;
            _inference.OnError += ShowError;
            _nextStep = Instantiate(_startTrainingButton, transform);
            _nextStep.name = "Next learning step";
            _nextStep.onClick = new Button.ButtonClickedEvent();
            _nextStep.onClick.AddListener(AdvanceStep);
            RectTransform nextRect = (RectTransform)_nextStep.transform;
            nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = new Vector2(0.5f, 0.5f);
            nextRect.anchoredPosition = new Vector2(0f, -630f);
            nextRect.sizeDelta = new Vector2(900f, 70f);
            _nextStepLabel = _nextStep.GetComponentInChildren<Text>();
            _nextStepLabel.rectTransform.anchorMin = Vector2.zero;
            _nextStepLabel.rectTransform.anchorMax = Vector2.one;
            _nextStepLabel.rectTransform.offsetMin = _nextStepLabel.rectTransform.offsetMax = Vector2.zero;
            UI.SceneUIStyle.Apply(_nextStep);
            ResetSessionUI();
        }

        public void ShowModelValidation(IReadOnlyList<ValidationIssue> issues,
            IReadOnlyList<LayerDefinition> slotLayers, bool isDragging, bool focus = false)
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
            _validation.ShowValidation(issues, slotLayers, isDragging);
            _statusText.text = isDragging ? "블록 배치 중"
                : _hasIssues ? "수정 필요 · " + issues.Count + "곳"
                : showIntro ? "모델 만들기 안내" : "블록 순서 확인 완료";
            _statusAccent.color = isDragging || showIntro ? _neutralColor : _hasIssues ? _problemColor : _readyColor;
            RefreshStartButton();
            if (focus || (_hasIssues && !isDragging && !wasInvalid && !_isBusy))
            {
                ShowValidationPage();
            }
        }

        public void ResetTraining()
        {
            _metrics.ResetTraining();
            DismissError();
            _inference.ResetTraining();
            ShowTrainingPage();
        }

        public void ResetSessionUI()
        {
            _metrics.ResetTraining();
            _inference.ResetTraining();
            _inference.ResetSession();
            _isBusy = false;
            _isDragging = false;
            _hasIssues = false;
            _statusText.text = "모델 만들기 안내";
            _statusAccent.color = _neutralColor;
            _validation.ShowValidation(Array.Empty<ValidationIssue>(), Array.Empty<LayerDefinition>(), false);
            _stopTrainingButton.interactable = false;
            _stateText.text = "블록을 조립하고 순서를 확인한 뒤 학습을 시작하세요.";
            SetModelChanged(false);
            DismissError();
            RefreshStartButton();
            ShowValidationPage();
        }

        public void ShowProgress(TrainingProgress progress)
        {
            _metrics.ShowProgress(progress);
        }

        public void ShowCompleted(TrainingResult result)
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

        public void ShowInference(InferenceResult result)
        {
            if (_inference.ShowInference(result))
            {
                DismissError();
                ShowPredictionPage();
            }
        }

        public void ShowState(ModelExecutionState state, bool hasTrainedModel, bool canInfer, bool canCancel)
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

        public void ShowError(string message)
        {
            _errorText.text = string.IsNullOrWhiteSpace(message) ? "요청을 완료하지 못했습니다." : message;
            _errorPanel.SetActive(true);
            _errorScroll.StopMovement();
            _errorScroll.content.anchoredPosition = Vector2.zero;
            _inference.ClearPendingPrediction();
        }

        public void ShowCancelled()
        {
            _stateText.text = "학습 취소됨 · 그래프에는 취소 전 완료된 에포크만 남아 있습니다.";
            DismissError();
            _inference.ResetTraining();
        }

        public void SetModelChanged(bool changed)
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
            _page = page;
            if (_nextStepLabel != null)
            {
                _nextStepLabel.text = page == 0 ? "조립 단계 건너뛰고 학습 화면으로"
                    : page == 1 ? "다음으로 · 숫자 예측 화면" : "현재 단계 마치기 / 건너뛰기";
            }
            for (int index = 0; index < _pages.Length; index++)
            {
                _pages[index].SetActive(index == page);
                _tabAccents[index].enabled = index == page;
            }
        }

        private void AdvanceStep()
        {
            if (!isActiveAndEnabled) { return; }
            if (_isDragging)
            {
                _stateText.text = "잡은 블록을 놓은 뒤 다음으로 진행해 주세요.";
                return;
            }
            DismissError();
            if (_page < _pages.Length - 1)
            {
                ShowPage(_page + 1);
                if (!_isBusy)
                {
                    _stateText.text = "화면 단계를 넘겼습니다. 학습·예측 결과는 실제 실행한 경우에만 표시됩니다.";
                }
                return;
            }
            if (_isBusy)
            {
                _stateText.text = "계산 중입니다. 완료를 기다리거나 학습 중지 후 단계를 마쳐 주세요.";
                return;
            }
            OnLessonSkipped?.Invoke();
            _stateText.text = "현재 단계를 마쳤습니다. 수행하지 않은 학습·예측은 완료 처리하지 않습니다. 바닥 안내선을 따라 출구로 이동하세요.";
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
