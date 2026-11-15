using System.Collections.Generic;
using Learning;
using RealityLab;
using ModelDesignScene.ModelBuilder;
using ModelDesignScene.BlockSettings;
using ModelDesignScene.Model;
using ModelDesignScene.Execution;
using ModelDesignScene.Execution.Engine;
using ModelDesignScene.ControlPanel;
using ModelDesignScene.Dataset;
using ModelDesignScene.Hologram;
using UnityEngine;

namespace ModelDesignScene
{
    public sealed class CNNModelDesignLabManager : MonoBehaviour, ILearningExperience
    {
        private const float TrainingResultDisplaySeconds = 5f;

        [SerializeField] private ModelBuilderManager _builder;
        [SerializeField] private ModelBlockSettingsManager _settings;
        [SerializeField] private ModelExecutionManager _execution;
        [SerializeField] private ModelControlPanelManager _controlPanel;
        [SerializeField] private ModelDatasetManager _dataset;
        [SerializeField] private CnnHologramController _hologram;

        private bool _hologramTraining;
        private float? _trainingResultEndsAt;
        private bool IsShowingTrainingResult => _trainingResultEndsAt.HasValue;

        [SerializeField] private Cinematic.CinematicModelResult _cinematicResult;

        private ModelRealitySessionController _reality;
        private bool CanEnterReality => CanExit && _execution.HasTrainedModel && !_builder.IsDragging
            && !_reality.IsOpen && !_reality.IsTransitioning;
        private bool CanPlayPlacementExample => _initialized && isActiveAndEnabled && !_execution.IsBusy
            && !IsShowingTrainingResult
            && !_reality.IsOpen && !_reality.IsTransitioning && _builder.CanPlayPlacementExample;
        public System.Threading.Tasks.Task EnterRealityAsync()
        {
            if (!CanEnterReality)
                throw new System.InvalidOperationException("Finish training and release the block before entering Reality.");
            _settings.Close();
            _builder.CancelPlacementExample();
            _builder.CancelTrainingFlow();
            return _reality.EnterAsync(gameObject.scene, Camera.main, InferRealityAsync);
        }
        public System.Threading.Tasks.Task ExitRealityAsync() => _reality.ExitAsync();
        public System.Threading.Tasks.Task<InferenceResult> InferRealityAsync(float[] input, string requestId)
        {
            if (!_reality.IsOpen || !requestId.StartsWith("reality:", System.StringComparison.Ordinal))
                throw new System.InvalidOperationException("Use an input captured by the active Reality session.");
            return _execution.InferExternalAsync(input, requestId);
        }
        private ModelDefinition _model;
        private IReadOnlyList<ValidationIssue> _validationIssues = System.Array.Empty<ValidationIssue>();
        private bool _hasValidationResult;
        private bool _initialized;
        private bool _trainingCompleted;
        private bool _progressionAvailable;
        public System.Collections.IEnumerator EndSession()
        {
            _trainingResultEndsAt = null;
            _hologramTraining = false;
            _hologram.Clear();
            _builder.CancelTrainingFlow();
            _execution.CloseSession();
            yield break;
        }

        public LearningStageId StageId => LearningStageId.ModelDesign;
        public float Progress { get; private set; }
        public event System.Action<float> ProgressChanged;
        public event System.Action<bool> ExitAvailabilityChanged;

        private void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (Progress == value) { return; }
            Progress = value;
            ProgressChanged?.Invoke(value);
        }
        private bool _modelUIIsDirty;

        public bool CanExit => isActiveAndEnabled && _trainingCompleted && !_execution.IsBusy;

        private void Awake()
        {
            _model = new ModelDefinition();
            _reality = gameObject.AddComponent<ModelRealitySessionController>();
        }

        private void OnEnable()
        {
            _modelUIIsDirty = true;
            _builder.OnPlacementGuideChanged += HandlePlacementGuideChanged;
            _controlPanel.OnExampleRequested += HandleExampleRequested;
            _controlPanel.OnRealityRequested += HandleRealityRequested;
            if (_initialized)
            {
                _builder.BeginInteraction();
                HandlePlacementGuideChanged();
            }
        }

        private void Start()
        {
            _model.OnChanged += HandleModelChanged;
            _builder.OnChanged += HandleBuilderChanged;
            _builder.OnSettingsRequested += HandleSettingsRequested;
            _settings.OnDeleteRequested += HandleDeleteRequested;
            _execution.OnStateChanged += HandleExecutionStateChanged;
            _execution.OnTrainingProgress += HandleTrainingProgress;
            _execution.OnTrainingCompleted += HandleTrainingCompleted;
            _execution.OnTrainingCancelled += HandleTrainingCancelled;
            _execution.OnInferenceCompleted += HandleInferenceCompleted;
            _execution.OnError += HandleExecutionError;
            _execution.OnValidationFailed += HandleValidationFailed;
            _controlPanel.OnInferenceRequested += HandleInferenceRequested;
            _controlPanel.OnStartRequested += HandleTrainingStartRequested;
            _controlPanel.OnStopRequested += HandleTrainingStopRequested;

            // Scene components have completed Awake before the module managers connect them.
            _dataset.Initialize();
            _execution.Initialize(_dataset);
            _controlPanel.Initialize(_dataset);
            _builder.Initialize(_model);
            _builder.BeginInteraction();
            _initialized = true;
            HandleExecutionStateChanged();
        }

        private void LateUpdate()
        {
            if (_trainingResultEndsAt is float endsAt && Time.unscaledTime >= endsAt)
            {
                FinishTrainingResultPresentation();
            }
            if (_initialized)
            {
                _controlPanel.SetRealityAvailable(CanEnterReality);
                _controlPanel.SetExampleAvailable(CanPlayPlacementExample);
                _builder.SetPlacementGuideAvailable(!_execution.IsBusy && !IsShowingTrainingResult
                    && !_reality.IsOpen && !_reality.IsTransitioning && _validationIssues.Count == 0);
            }
            if (_initialized && _modelUIIsDirty)
            {
                RefreshModelUI();
            }
        }

        private void HandlePlacementGuideChanged()
        {
            _controlPanel.ShowPlacementGuide(_builder.PlacementGuideState, _builder.PlacementGuideSlot);
        }

        private void HandleBuilderChanged()
        {
            _settings.Close();
            HandleModelChanged();
        }

        private void HandleSettingsRequested(LayerDefinition layer, Pose anchorPose)
        {
            if (isActiveAndEnabled)
            {
                _settings.Open(layer, anchorPose);
            }
        }

        private void HandleDeleteRequested(LayerDefinition layer)
        {
            _settings.Close();
            _builder.DeleteLayer(layer);
        }

        private void HandleExampleRequested()
        {
            if (!CanPlayPlacementExample) { return; }
            _settings.Close();
            if (_builder.PlayPlacementExample(
                slots => _execution.TryValidateConfiguration(slots, _model.Output, out _))) { return; }
            if (_execution.TryValidateConfiguration(_model.Layers, _model.Output, out ValidationIssue issue))
            {
                _validationIssues = System.Array.Empty<ValidationIssue>();
                _hasValidationResult = true;
                _controlPanel.ShowModelValidation(_validationIssues, _model.Layers, false, true, true);
            }
            else
            {
                HandleValidationFailed(new[] { issue });
            }
        }

        private async void HandleTrainingStartRequested()
        {
            if (!isActiveAndEnabled || _execution.IsBusy || _builder.IsDragging || IsShowingTrainingResult)
            {
                return;
            }
            _builder.SetPlacementGuideAvailable(false);
            _settings.Close();
            _builder.CancelPlacementExample();
            await _execution.StartTrainingAsync(_model);
        }

        private async void HandleTrainingStopRequested()
        {
            if (isActiveAndEnabled && _execution.CanCancel)
            {
                await _execution.CancelTrainingAsync();
            }
        }

        private async void HandleInferenceRequested(string imageId)
        {
            if (isActiveAndEnabled && !_execution.IsBusy && _execution.HasTrainedModel && !IsShowingTrainingResult)
            {
                _builder.CancelPlacementExample();
                await _execution.InferAsync(imageId);
            }
        }

        private async void HandleRealityRequested()
        {
            if (!CanEnterReality) { return; }
            try
            {
                await EnterRealityAsync();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[ERR-REALITY] {exception}");
                if (this != null) { _controlPanel.ShowRealityEntryFailure(); }
            }
        }

        private void HandleExecutionStateChanged()
        {
            if (_initialized && isActiveAndEnabled)
            {
                if (_execution.State == ModelExecutionState.Training)
                {
                    _trainingCompleted = false;
                    _controlPanel.SetTrainingResultPresentation(false, false);
                    SetProgress(0f);
                    _validationIssues = System.Array.Empty<ValidationIssue>();
                    _hasValidationResult = true;
                    _controlPanel.ResetTraining();
                    RunPresentation(ShowStructureHologram);
                }
                else if (_execution.State == ModelExecutionState.Completed)
                {
                    RunPresentation(_builder.FinishTrainingFlow);
                }
                else
                {
                    RunPresentation(_builder.FadeModelFlow);
                }
                UpdateStructureHologramState(_execution.State);
                _controlPanel.ShowState(_execution.State, _execution.HasTrainedModel,
                    !_execution.IsBusy && _execution.HasTrainedModel, _execution.CanCancel);
                _modelUIIsDirty = true;
                PublishProgressionState();
            }
        }

        private void ShowStructureHologram()
        {
            _builder.RefreshLabels();
            _hologram.Show(_execution.SubmittedModel, _builder.ReadHologramLayers());
            _hologramTraining = true;
            _builder.PlayHologramFlow(_hologram.ReadFlowFrames(), true, _hologram.FlowReadyAt);
        }

        private void UpdateStructureHologramState(ModelExecutionState state)
        {
            if (!_hologramTraining) { return; }
            switch (state)
            {
                case ModelExecutionState.Completed:
                    _hologramTraining = false;
                    _hologram.Complete();
                    break;
                case ModelExecutionState.CancellingTraining:
                case ModelExecutionState.Cancelled:
                case ModelExecutionState.Failed:
                    _hologramTraining = false;
                    _hologram.Fade();
                    break;
            }
        }

        private void HandleTrainingProgress(TrainingProgress progress)
        {
            RunPresentation(() => _controlPanel.ShowProgress(progress));
            RunPresentation(() => _builder.ShowEpochFlow(progress.Epoch));
            SetProgress(progress.Epochs > 0 ? 0.95f * progress.Epoch / progress.Epochs : 0f);
        }
        private void HandleTrainingCancelled() => _controlPanel.ShowCancelled();
        private void HandleInferenceCompleted(InferenceResult result)
        {
            if (!result.ImageId.StartsWith("reality:", System.StringComparison.Ordinal))
                _controlPanel.ShowInference(result);
        }
        private void HandleExecutionError(string message) => _controlPanel.ShowError(message);

        private void HandleTrainingCompleted(TrainingResult result)
        {
            if (result == null) { return; }

            _trainingResultEndsAt = Time.unscaledTime + TrainingResultDisplaySeconds;
            _controlPanel.SetTrainingResultPresentation(true, false);
            RunPresentation(() => _controlPanel.ShowCompleted(result));
            RunPresentation(() => RecordTrainedModel(_execution.SubmittedModel, result));
        }

        private void FinishTrainingResultPresentation()
        {
            _trainingResultEndsAt = null;
            _hologram.Clear();
            _builder.CancelTrainingFlow();
            _trainingCompleted = true;
            _controlPanel.SetTrainingResultPresentation(false, true);
            SetProgress(1f);
            PublishProgressionState();
        }

        private void RecordTrainedModel(ModelExecutionDefinition submitted, TrainingResult result)
        {
            List<string> layers = new List<string> { "INPUT" };
            // The final definition is the fixed OUTPUT classifier.
            for (int i = 0; i < submitted.Layers.Count - 1; i++)
            {
                CnnLayerDefinition layer = submitted.Layers[i];
                string label = layer.Kind == LayerKind.MaxPooling2D ? nameof(LayerType.MaxPooling2D) : layer.Kind.ToString();
                if (layer.Kind == LayerKind.Conv2D) { label += "\n" + layer.Units + " × " + layer.Height + "×" + layer.Width; }
                if (layer.Kind == LayerKind.Dense) { label += "\n" + layer.Units; }
                layers.Add(label);
            }
            layers.Add("OUTPUT");
            double? testAccuracy = result.Evaluation.TryGetValue("accuracy", out double accuracy) ? accuracy : null;
            _cinematicResult.Record(layers, testAccuracy);
        }

        private static void RunPresentation(System.Action action)
        {
            try { action(); }
            catch (System.Exception exception) { Debug.LogError(exception); }
        }

        private void PublishProgressionState()
        {
            bool available = CanExit;
            if (_progressionAvailable == available) { return; }
            _progressionAvailable = available;
            ExitAvailabilityChanged?.Invoke(available);
        }

        private void HandleModelChanged()
        {
            _builder.CancelPlacementExample();
            _validationIssues = System.Array.Empty<ValidationIssue>();
            _hasValidationResult = false;
            _modelUIIsDirty = true;
        }

        private void HandleValidationFailed(IReadOnlyList<ValidationIssue> issues)
        {
            _validationIssues = issues;
            _hasValidationResult = true;
            _modelUIIsDirty = true;
            _controlPanel.ShowModelValidation(issues, _model.Layers, _builder.IsDragging, true, true);
        }

        private void RefreshModelUI()
        {
            _modelUIIsDirty = false;
            _builder.RefreshLabels();
            _controlPanel.ShowAssemblyOutputSizes(_builder.ReadOutputSizes());
            _settings.Refresh();
            _controlPanel.ShowModelValidation(_validationIssues, _model.Layers,
                _builder.IsDragging, _hasValidationResult);
            bool changed = _execution.SubmittedModel != null && !_execution.MatchesSubmittedModel(_model);
            _controlPanel.SetModelChanged(changed);
            _hologram.SetAssemblyMatches(!changed);
        }

        private void OnDisable()
        {
            _builder.OnPlacementGuideChanged -= HandlePlacementGuideChanged;
            _trainingResultEndsAt = null;
            _hologramTraining = false;
            _hologram.Clear();
            _controlPanel.OnExampleRequested -= HandleExampleRequested;
            _controlPanel.OnRealityRequested -= HandleRealityRequested;
            _trainingCompleted = false;
            SetProgress(0f);
            PublishProgressionState();
            _hasValidationResult = false;
            _validationIssues = System.Array.Empty<ValidationIssue>();
            _execution.CloseSession();
            if (_initialized)
            {
                _builder.EndInteraction();
                _controlPanel.ResetSessionUI();
            }
            _settings.Close();
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                if (_builder != null)
                {
                    _builder.OnChanged -= HandleBuilderChanged;
                    _builder.OnSettingsRequested -= HandleSettingsRequested;
                }
                if (_settings != null)
                {
                    _settings.OnDeleteRequested -= HandleDeleteRequested;
                }
                if (_execution != null)
                {
                    _execution.OnStateChanged -= HandleExecutionStateChanged;
                    _execution.OnTrainingProgress -= HandleTrainingProgress;
                    _execution.OnTrainingCompleted -= HandleTrainingCompleted;
                    _execution.OnTrainingCancelled -= HandleTrainingCancelled;
                    _execution.OnInferenceCompleted -= HandleInferenceCompleted;
                    _execution.OnError -= HandleExecutionError;
                    _execution.OnValidationFailed -= HandleValidationFailed;
                }
                if (_controlPanel != null)
                {
                    _controlPanel.OnInferenceRequested -= HandleInferenceRequested;
                    _controlPanel.OnStartRequested -= HandleTrainingStartRequested;
                    _controlPanel.OnStopRequested -= HandleTrainingStopRequested;
                }
            }
            _model.OnChanged -= HandleModelChanged;
            _model.Dispose();
        }
    }
}
