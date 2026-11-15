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
        [SerializeField] private Text _headerTitle;
        [SerializeField] private Button _exampleButton;
        [SerializeField] private Text _stateText;
        [SerializeField] private Text _statusAccent;
        [SerializeField] private GameObject _modelChangedNotice;
        [SerializeField] private GameObject[] _pages;
        [SerializeField] private Image[] _tabBackgrounds;
        [SerializeField] private Image[] _tabBorders;
        [SerializeField] private Text[] _tabLabels;
        [SerializeField] private Button _validationTab;
        [SerializeField] private Button _trainingTab;
        [SerializeField] private Button _predictionTab;

        [Header("Actions and request errors")]
        [SerializeField] private Button _realityButton;
        [SerializeField] private Button _startTrainingButton;
        [SerializeField] private Text _stopTrainingLabel;
        [SerializeField] private Image[] _exampleBorder;
        [SerializeField] private Image[] _realityBorder;
        [SerializeField] private Image[] _startTrainingBorder;
        [SerializeField] private Image[] _stopTrainingBorder;
        [SerializeField] private Text _exampleLabel;
        [SerializeField] private Text _realityLabel;
        [SerializeField] private Button _stopTrainingButton;
        [SerializeField] private Text _startTrainingLabel;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private Image[] _errorBorder;
        [SerializeField] private Text _errorTitle;
        [SerializeField] private Text _errorText;
        [SerializeField] private Text _errorCode;
        [SerializeField] private Button _errorActionButton;
        [SerializeField] private Text _errorActionLabel;
        [SerializeField] private Button _dismissErrorButton;

        [Header("Screen sections")]
        [SerializeField] private ModelValidationController _validation;
        [SerializeField] private ModelAssemblyController _assembly;
        [SerializeField] private Text _exampleReason;
        [SerializeField] private Text _realityReason;
        [SerializeField] private ModelRealityEntryGuideController _realityGuide;
        [SerializeField] private ModelTrainingMetricsController _metrics;
        [SerializeField] private ModelInferenceResultsController _inference;

        [Header("Status colors")]
        private static readonly Color Yellow = new Color32(242, 201, 76, 255);
        private static readonly Color TextBright = new Color32(235, 242, 242, 255);
        private static readonly Color TextMuted = new Color32(170, 185, 192, 255);
        private static readonly Color TextDisabled = new Color32(130, 146, 154, 255);
        private static readonly Color Amber = new Color32(255, 178, 74, 255);
        private static readonly Color ReadyGreen = new Color32(64, 214, 176, 255);
        private static readonly Color PanelFill = new Color32(31, 37, 40, 255);
        private static readonly Color SelectedTabFill = new Color32(77, 55, 28, 255);
        private static readonly Color AvailableTabBorder = new Color32(74, 82, 84, 255);
        private static readonly Color UnavailableTabBorder = new Color32(47, 54, 58, 255);
        private static readonly Color ActionText = new Color32(26, 18, 6, 255);

        private enum ErrorCard { None, Training, Cancelling, SampleInference, Reality, Dataset }

        private ErrorCard _activeError;
        // Execution reports Failed before its error message, so retain the request's card here.
        private ErrorCard _executionError = ErrorCard.Training;
        private int _currentPage;
        private bool _hasIssues;
        private bool _isDragging;
        private bool _isBusy;
        private bool _hasTrainedModel;
        private bool _showingTrainingResult;
        private bool _highlightReality;
        private bool _experienceCompleted;
        private ModelExecutionState _state;

        internal event Action OnExampleRequested;
        internal event Action OnRealityRequested;
        internal event Action OnStartRequested;
        internal event Action OnStopRequested;
        internal event Action<string> OnInferenceRequested;

        private void Awake()
        {
            _canvas.worldCamera = Camera.main;
            _validationTab.onClick.AddListener(ShowValidationPage);
            _trainingTab.onClick.AddListener(ShowTrainingPage);
            _predictionTab.onClick.AddListener(ShowPredictionPage);
            _exampleButton.onClick.AddListener(HandleExampleRequested);
            _realityButton.onClick.AddListener(HandleRealityRequested);
            _startTrainingButton.onClick.AddListener(HandleStartRequested);
            _stopTrainingButton.onClick.AddListener(HandleStopRequested);
            _dismissErrorButton.onClick.AddListener(DismissError);
            _errorActionButton.onClick.AddListener(HandleErrorAction);
            _metrics.Initialize();
            _inference.Initialize();
            _inference.OnInferenceRequested += HandleInferenceRequested;
            _inference.OnError += ShowSampleFailure;
            ResetSessionUI();
        }

        private void HandleExampleRequested()
        {
            DismissError();
            ShowValidationPage();
            OnExampleRequested?.Invoke();
        }

        internal void SetExampleAvailable(bool available)
        {
            _exampleButton.interactable = available;
            SetSecondaryStyle(_exampleBorder, _exampleLabel, available && !_highlightReality);
            RefreshSecondaryReasons();
        }

        private void HandleRealityRequested()
        {
            if (!isActiveAndEnabled || !_realityButton.interactable) { return; }
            DismissError();
            OnRealityRequested?.Invoke();
        }

        internal void SetRealityAvailable(bool available)
        {
            _realityButton.interactable = available;
            bool highlighted = available && _highlightReality;
            _realityButton.image.color = highlighted ? Amber : Color.clear;
            if (highlighted)
            {
                foreach (Image edge in _realityBorder) { edge.color = Amber; }
                _realityLabel.color = ActionText;
            }
            else
            {
                SetSecondaryStyle(_realityBorder, _realityLabel, available);
            }
            RefreshSecondaryReasons();
            RefreshRealityGuide();
            if (_activeError == ErrorCard.Reality) { _errorActionButton.interactable = available; }
        }

        internal void SetTrainingResultPresentation(bool showing, bool highlightReality)
        {
            _showingTrainingResult = showing;
            _highlightReality = highlightReality;
            RefreshPlacementGuideVisibility();
            _validationTab.interactable = !showing;
            _trainingTab.interactable = !showing;
            _predictionTab.interactable = !showing;
            _inference.SetAvailability(_hasTrainedModel, !showing && !_isBusy && _hasTrainedModel);

            RefreshStartButton();
            RefreshTabStyles();
            RefreshRealityGuide();
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
            _assembly.ShowAssembly(slotLayers, isDragging);
            RefreshHeader();
            _validation.ShowValidation(issues, slotLayers, isDragging, hasValidationResult);
            RefreshPlacementGuideVisibility();
            if (!_isBusy && (_state == ModelExecutionState.Idle || _hasIssues || wasInvalid))
            {
                ShowAssemblyStatus();
            }
            RefreshSecondaryReasons();
            RefreshStartButton();
            RefreshRealityGuide();
            if (focus || (_hasIssues && !isDragging && !wasInvalid && !_isBusy))
            {
                ShowValidationPage();
            }
        }

        internal void ResetTraining()
        {
            _experienceCompleted = false;
            _metrics.ResetTraining();
            DismissError();
            _inference.ResetTraining();
            ShowTrainingPage();
        }

        internal void ResetSessionUI()
        {
            _experienceCompleted = false;
            _metrics.ResetTraining();
            _inference.ResetTraining();
            _inference.ResetSession();
            _isBusy = false;
            _isDragging = false;
            _hasIssues = false;
            _assembly.ShowPlacementGuide(PlacementGuideState.Hidden, -1);
            _assembly.ShowAssembly(Array.Empty<LayerDefinition>(), false);
            _assembly.ShowOutputSizes(Array.Empty<string>());
            _state = ModelExecutionState.Idle;
            _hasTrainedModel = false;
            SetTrainingResultPresentation(false, false);
            SetExampleAvailable(false);
            SetRealityAvailable(false);
            _validation.ShowValidation(Array.Empty<ValidationIssue>(), Array.Empty<LayerDefinition>(), false, false);
            _stopTrainingButton.interactable = false;
            ShowTrainingAction(false);
            ShowAssemblyStatus();
            SetModelChanged(false);
            DismissError();
            RefreshStartButton();
            ShowValidationPage();
        }

        internal void ShowProgress(TrainingProgress progress)
        {
            _metrics.ShowProgress(progress);
            if (_state == ModelExecutionState.Training && progress != null)
            {
                _stateText.text = $"학습 중 · 에포크 {progress.Epoch}/{progress.Epochs}";
            }
        }

        internal void ShowCompleted(TrainingResult result)
        {
            if (result == null)
            {
                return;
            }
            _metrics.ShowCompleted(result);
            _stateText.text = "학습 완료";
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
            _state = state;
            _hasTrainedModel = hasTrainedModel;
            _isBusy = state == ModelExecutionState.StartingTraining
                || state == ModelExecutionState.Training || state == ModelExecutionState.CancellingTraining
                || state == ModelExecutionState.Inferring;
            RefreshPlacementGuideVisibility();
            if (_activeError == ErrorCard.Cancelling && state != ModelExecutionState.CancellingTraining)
            {
                HideErrorCard();
            }
            switch (state)
            {
                case ModelExecutionState.StartingTraining:
                    _executionError = ErrorCard.Training;
                    _stateText.text = "학습 준비 중";
                    break;
                case ModelExecutionState.Training:
                    _stateText.text = "학습 중";
                    break;
                case ModelExecutionState.CancellingTraining:
                    _stateText.text = "학습 멈추는 중";
                    ShowErrorCard(ErrorCard.Cancelling);
                    break;
                case ModelExecutionState.Completed:
                    _stateText.text = _experienceCompleted ? "체험 완료" : "학습 완료";
                    break;
                case ModelExecutionState.Inferring:
                    _executionError = ErrorCard.SampleInference;
                    _stateText.text = "예측 중";
                    break;
                case ModelExecutionState.Cancelled:
                    _stateText.text = "학습 취소됨";
                    break;
                case ModelExecutionState.Failed:
                    _stateText.text = "학습 멈춤";
                    break;
                default:
                    ShowAssemblyStatus();
                    break;
            }
            _stopTrainingButton.interactable = canCancel;
            ShowTrainingAction(canCancel || state == ModelExecutionState.CancellingTraining);
            if (state != ModelExecutionState.Idle)
            {
                _statusAccent.color = state switch
                {
                    ModelExecutionState.StartingTraining or ModelExecutionState.Training => Amber,
                    ModelExecutionState.Completed => ReadyGreen,
                    ModelExecutionState.Inferring => Amber,
                    ModelExecutionState.Failed => Yellow,
                    _ => TextMuted
                };
            }
            if (!_isBusy && _hasIssues) { ShowAssemblyStatus(); }
            RefreshSecondaryReasons();
            RefreshTabStyles();
            _inference.SetAvailability(hasTrainedModel, canInfer && !_showingTrainingResult);
            RefreshStartButton();
            RefreshRealityGuide();
        }

        internal void ShowError(string message)
        {
            Debug.LogError($"[ModelControlPanel] {message}");
            if (_executionError == ErrorCard.SampleInference) { ShowSampleFailure(); }
            else { ShowErrorCard(_executionError); }
        }

        internal void ShowDatasetPreparationFailure(string message)
        {
            _executionError = ErrorCard.Dataset;
            ShowError(message);
        }

        internal void ShowRealityEntryFailure() => ShowErrorCard(ErrorCard.Reality);

        internal void ShowExperienceCompleted()
        {
            DismissError();
            _experienceCompleted = true;
            _stateText.text = "체험 완료";
            _statusAccent.color = ReadyGreen;
            SetTrainingResultPresentation(false, false);
            RefreshSecondaryReasons();
        }

        private void ShowSampleFailure()
        {
            _inference.ClearPendingPrediction();
            if (_activeError != ErrorCard.Cancelling) { ShowErrorCard(ErrorCard.SampleInference); }
        }

        private void ShowErrorCard(ErrorCard card)
        {
            var (title, reason, action, code, canDismiss) = card switch
            {
                ErrorCard.Training => ("학습이 중간에 멈췄어요", "조립한 모델에는 문제가 없어요. 다시 학습해 보세요.",
                    "다시 학습하기", "ERR-TRAIN", true),
                ErrorCard.Cancelling => ("학습을 멈추는 중이에요", "잠시 후 다시 시작할 수 있어요.",
                    "", "", false),
                ErrorCard.SampleInference => ("예측하지 못했어요", "다른 샘플을 고르거나 다시 눌러 보세요.",
                    "", "ERR-INFER", true),
                ErrorCard.Reality => ("현실 숫자 인식을 열지 못했어요", "헤드셋 연결과 카메라 상태를 확인한 뒤 다시 시도해 주세요.",
                    "다시 시도", "ERR-REALITY", true),
                ErrorCard.Dataset => ("학습 데이터를 불러오지 못했어요", "진행자에게 알려 주세요.",
                    "", "ERR-DATA", false),
                _ => throw new ArgumentOutOfRangeException(nameof(card))
            };

            _activeError = card;
            Color borderColor = card == ErrorCard.Cancelling
                ? TextMuted : Yellow;
            foreach (Image edge in _errorBorder) { edge.color = borderColor; }
            _errorTitle.text = title;
            _errorText.text = reason;
            _errorCode.text = code;
            _errorCode.gameObject.SetActive(code.Length > 0);
            _errorActionLabel.text = action;
            _errorActionButton.gameObject.SetActive(action.Length > 0);
            _errorActionButton.interactable = card switch
            {
                ErrorCard.Training => _startTrainingButton.interactable,
                ErrorCard.Reality => _realityButton.interactable,
                _ => true
            };
            _dismissErrorButton.gameObject.SetActive(canDismiss);

            RectTransform close = (RectTransform)_dismissErrorButton.transform;
            close.anchoredPosition = new Vector2(action.Length > 0 ? 276f : 28f, close.anchoredPosition.y);
            _errorPanel.SetActive(true);
            RefreshRealityGuide();
        }

        internal void ShowCancelled()
        {
            _stateText.text = "학습 취소됨";
            DismissError();
            _inference.ResetTraining();
        }

        internal void SetModelChanged(bool changed)
        {
            _modelChangedNotice.SetActive(changed);
        }

        private void RefreshStartButton()
        {
            bool canStart = !_isBusy && !_isDragging && !_showingTrainingResult;
            _startTrainingButton.interactable = canStart;
            if (_activeError == ErrorCard.Training) { _errorActionButton.interactable = canStart; }
            SetSecondaryStyle(_startTrainingBorder, _startTrainingLabel, canStart);
            if (canStart && _highlightReality)
            {
                foreach (Image edge in _startTrainingBorder) { edge.color = TextDisabled; }
                _startTrainingLabel.color = TextBright;
            }
            _startTrainingLabel.text = _hasTrainedModel ? "▶ 다시 학습하기" : "▶ 학습 시작";
        }

        private void ShowTrainingAction(bool showStop)
        {
            _startTrainingButton.gameObject.SetActive(!showStop);
            _stopTrainingButton.gameObject.SetActive(showStop);
            SetTrainingStyle(_stopTrainingBorder, _stopTrainingLabel, _stopTrainingButton.interactable);
        }

        private static void SetSecondaryStyle(Image[] border, Text label, bool available)
        {
            foreach (Image edge in border) { edge.color = available ? Amber : AvailableTabBorder; }
            label.color = available ? Amber : TextDisabled;
        }

        private static void SetTrainingStyle(Image[] border, Text label, bool available)
        {
            foreach (Image edge in border) { edge.enabled = !available; }
            label.color = available ? ActionText : TextDisabled;
        }

        private void HandleStartRequested()
        {
            if (isActiveAndEnabled && _startTrainingButton.interactable)
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
        private void DismissError()
        {
            // Other controls may still be usable while the engine finishes cancelling.
            if (_activeError == ErrorCard.Cancelling && _isBusy) { return; }
            HideErrorCard();
        }

        private void HideErrorCard()
        {
            _activeError = ErrorCard.None;
            _errorPanel.SetActive(false);
            RefreshRealityGuide();
        }

        private void HandleErrorAction()
        {
            switch (_activeError)
            {
                case ErrorCard.Training: HandleStartRequested(); break;
                case ErrorCard.Reality: HandleRealityRequested(); break;
            }
        }

        private void ShowPage(int page)
        {
            _currentPage = page;
            RefreshHeader();
            for (int index = 0; index < _pages.Length; index++)
            {
                _pages[index].SetActive(index == page);
            }
            RefreshTabStyles();
        }

        private void RefreshTabStyles()
        {
            for (int index = 0; index < _tabLabels.Length; index++)
            {
                bool selected = index == _currentPage;
                bool unavailable = index == 2 && !_hasTrainedModel;
                bool emphasized = selected && !_highlightReality;
                _tabBorders[index].color = emphasized ? Amber : unavailable ? UnavailableTabBorder : AvailableTabBorder;
                _tabBackgrounds[index].color = emphasized ? SelectedTabFill : selected ? UnavailableTabBorder : PanelFill;
                _tabLabels[index].color = emphasized ? Amber : unavailable ? TextDisabled : TextMuted;
            }
        }

        internal void ShowAssemblyOutputSizes(IReadOnlyList<string> sizes)
        {
            _assembly.ShowOutputSizes(sizes);
        }

        internal void ShowPlacementGuide(PlacementGuideState state, int slotIndex)
        {
            _assembly.ShowPlacementGuide(state, slotIndex);
            RefreshPlacementGuideVisibility();
        }

        private void RefreshPlacementGuideVisibility()
        {
            _assembly.SetPlacementGuideAvailable(!_hasIssues && !_isBusy && !_showingTrainingResult);
        }

        private void ShowAssemblyStatus()
        {
            int placedBlockCount = _assembly.PlacedBlockCount;
            _stateText.text = !_isDragging && _hasIssues ? "구성 수정 필요"
                : _isDragging || placedBlockCount > 0 ? $"조립 중 · 블록 {placedBlockCount}개" : "조립 대기";
            _statusAccent.color = _hasIssues || _isDragging || placedBlockCount > 0 ? Amber : TextMuted;
        }

        private void RefreshSecondaryReasons()
        {
            _realityLabel.text = _experienceCompleted ? "AR 룸 다시 입장" : "AR 룸 입장";
            _exampleReason.text = _exampleButton.interactable ? "" : _isDragging ? "블록을 놓은 뒤 사용"
                : _state is ModelExecutionState.StartingTraining or ModelExecutionState.Training
                    or ModelExecutionState.CancellingTraining ? "학습 중에는 쓸 수 없어요" : "";
            _realityReason.text = !_hasTrainedModel ? "학습을 마치면 입장할 수 있습니다"
                : _isDragging ? "잡고 있는 블록을 먼저 놓아 주세요"
                : _showingTrainingResult ? "학습 결과 확인 후 입장"
                : _isBusy ? "작업이 끝나면 입장할 수 있습니다"
                : _realityButton.interactable ? "현실 숫자 촬영 · 0~3" : "";
            _realityReason.color = _realityButton.interactable && _highlightReality ? ActionText : TextMuted;
            _exampleReason.gameObject.SetActive(_exampleReason.text.Length > 0);
            _realityReason.gameObject.SetActive(_realityReason.text.Length > 0);
            SetSecondaryLabelPosition(_exampleLabel, _exampleReason.gameObject.activeSelf);
            SetSecondaryLabelPosition(_realityLabel, _realityReason.gameObject.activeSelf);
        }

        private static void SetSecondaryLabelPosition(Text label, bool withReason)
        {
            RectTransform rect = label.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, withReason ? 42f : 78f);
        }

        private void RefreshHeader()
        {
            _headerTitle.text = _experienceCompleted && !_isBusy && !_hasIssues
                && _activeError == ErrorCard.None ? "체험 완료 · 수고하셨습니다"
                : _highlightReality && _hasTrainedModel && !_isBusy
                && !_showingTrainingResult && _activeError == ErrorCard.None && !_hasIssues
                ? "학습 완료 · AR 룸으로!" : "숫자를 읽는 CNN 만들기";
        }

        private void RefreshRealityGuide()
        {
            // The existing availability includes scene transitions; a held block is the only
            // unavailable state that keeps the invitation visible with a release instruction.
            bool visible = (_highlightReality || _experienceCompleted) && _hasTrainedModel && !_isBusy
                && !_showingTrainingResult && _activeError == ErrorCard.None
                && (_realityButton.interactable || _isDragging);
            _realityGuide.Show(visible, _realityButton.interactable, _experienceCompleted);
            RefreshHeader();
        }

        private void OnDestroy()
        {
            _validationTab.onClick.RemoveListener(ShowValidationPage);
            _trainingTab.onClick.RemoveListener(ShowTrainingPage);
            _predictionTab.onClick.RemoveListener(ShowPredictionPage);
            _exampleButton.onClick.RemoveListener(HandleExampleRequested);
            _realityButton.onClick.RemoveListener(HandleRealityRequested);
            _startTrainingButton.onClick.RemoveListener(HandleStartRequested);
            _stopTrainingButton.onClick.RemoveListener(HandleStopRequested);
            _dismissErrorButton.onClick.RemoveListener(DismissError);
            _errorActionButton.onClick.RemoveListener(HandleErrorAction);
            _inference.OnInferenceRequested -= HandleInferenceRequested;
            _inference.OnError -= ShowSampleFailure;
        }
    }
}
