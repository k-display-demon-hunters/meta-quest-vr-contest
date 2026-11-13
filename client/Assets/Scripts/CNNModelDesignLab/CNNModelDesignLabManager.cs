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
using UnityEngine;

namespace ModelDesignScene
{
    public sealed class CNNModelDesignLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private ModelBuilderManager _builder;
        [SerializeField] private ModelBlockSettingsManager _settings;
        [SerializeField] private ModelExecutionManager _execution;
        [SerializeField] private ModelControlPanelManager _controlPanel;
        [SerializeField] private ModelDatasetManager _dataset;

        [SerializeField] private Cinematic.CinematicModelResult _cinematicResult;

        private ModelRealitySessionController _reality;
        private bool CanEnterReality => CanExit && _execution.HasTrainedModel && !_builder.IsDragging
            && !_reality.IsOpen && !_reality.IsTransitioning;
        private bool CanPlayPlacementExample => _initialized && isActiveAndEnabled && !_execution.IsBusy
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
            _controlPanel.OnExampleRequested += HandleExampleRequested;
            _controlPanel.OnRealityRequested += HandleRealityRequested;
            if (_initialized)
            {
                _builder.BeginInteraction();
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
            if (_initialized)
            {
                _controlPanel.SetRealityAvailable(CanEnterReality);
                _controlPanel.SetExampleAvailable(CanPlayPlacementExample);
            }
            if (_initialized && _modelUIIsDirty)
            {
                RefreshModelUI();
            }
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
            if (CanPlayPlacementExample)
            {
                _settings.Close();
                _builder.PlayPlacementExample();
            }
        }

        private async void HandleTrainingStartRequested()
        {
            if (!isActiveAndEnabled || _execution.IsBusy || _builder.IsDragging)
            {
                return;
            }
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
            if (isActiveAndEnabled && !_execution.IsBusy && _execution.HasTrainedModel)
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
                if (this != null) { _controlPanel.ShowError(exception.Message); }
            }
        }

        private void HandleExecutionStateChanged()
        {
            if (_initialized && isActiveAndEnabled)
            {
                if (_execution.State == ModelExecutionState.Training)
                {
                    _trainingCompleted = false;
                    SetProgress(0f);
                    _validationIssues = System.Array.Empty<ValidationIssue>();
                    _hasValidationResult = true;
                    _controlPanel.ResetTraining();
                    _builder.PlayTrainingFlow();
                }
                else if (_execution.State == ModelExecutionState.Completed)
                {
                    _builder.FinishTrainingFlow();
                }
                else
                {
                    _builder.CancelTrainingFlow();
                }
                _controlPanel.ShowState(_execution.State, _execution.HasTrainedModel,
                    !_execution.IsBusy && _execution.HasTrainedModel, _execution.CanCancel);
                _modelUIIsDirty = true;
                PublishProgressionState();
            }
        }

        private void HandleTrainingProgress(TrainingProgress progress)
        {
            _controlPanel.ShowProgress(progress);
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
            _controlPanel.ShowCompleted(result);
            _trainingCompleted = result != null;
            if (_trainingCompleted)
            {
                SetProgress(1f);
                List<string> layers = new List<string> { "INPUT" };
                foreach (LayerDefinition layer in _model.Layers)
                {
                    if (layer == null) { continue; }
                    string label = layer.Type.ToString();
                    if (layer.Type == LayerType.Conv2D) { label += "\n" + layer.Filters + " × " + layer.Height + "×" + layer.Width; }
                    if (layer.Type == LayerType.Dense) { label += "\n" + layer.Units; }
                    layers.Add(label);
                }
                layers.Add("OUTPUT");
                _cinematicResult.Record(layers);
            }
            PublishProgressionState();
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
            _builder.CancelTrainingFlow();
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
            _settings.Refresh();
            _controlPanel.ShowModelValidation(_validationIssues, _model.Layers,
                _builder.IsDragging, _hasValidationResult);
            _controlPanel.SetModelChanged(_execution.SubmittedModel != null
                && !_execution.MatchesSubmittedModel(_model));
        }

        private void OnDisable()
        {
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
