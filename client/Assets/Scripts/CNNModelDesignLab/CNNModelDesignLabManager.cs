using System.Collections.Generic;
using ModelDesignScene.ModelBuilder;
using ModelDesignScene.BlockSettings;
using ModelDesignScene.Model;
using ModelDesignScene.Execution;
using ModelDesignScene.Execution.Engine;
using ModelDesignScene.ControlPanel;
using ModelDesignScene.Dataset;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene
{
    public sealed class CNNModelDesignLabManager : MonoBehaviour
    {
        [SerializeField] private ModelBuilderManager _builder;
        [SerializeField] private ModelBlockSettingsManager _settings;
        [SerializeField] private ModelExecutionManager _execution;
        [SerializeField] private ModelControlPanelManager _controlPanel;
        [SerializeField] private ModelDatasetManager _dataset;
        [SerializeField] private ModelXRInputController _xrInput;

        private ModelDefinition _model;
        private IReadOnlyList<ValidationIssue> _validationIssues = System.Array.Empty<ValidationIssue>();
        private bool _hasValidationResult;
        private bool _initialized;
        private bool _trainingCompleted;
        private bool _modelUIIsDirty;

        public bool ReadyForFactoryExit => _trainingCompleted && !_execution.IsBusy;

        private void Awake()
        {
            _model = new ModelDefinition();
        }

        private void OnEnable()
        {
            _modelUIIsDirty = true;
            _xrInput.EnableInput();
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
            _builder.Initialize(_model, GetActivationHit);
            _builder.BeginInteraction();
            _initialized = true;
            HandleExecutionStateChanged();
        }

        private void LateUpdate()
        {
            if (_initialized && _modelUIIsDirty)
            {
                RefreshModelUI();
            }
        }

        private Collider GetActivationHit(IXRActivateInteractor interactor)
        {
            return _xrInput.GetActivationHit(interactor);
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

        private async void HandleTrainingStartRequested()
        {
            if (!isActiveAndEnabled || _execution.IsBusy || _builder.IsDragging)
            {
                return;
            }
            _settings.Close();
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
                await _execution.InferAsync(imageId);
            }
        }

        private void HandleExecutionStateChanged()
        {
            if (_initialized && isActiveAndEnabled)
            {
                if (_execution.State == ModelExecutionState.Training)
                {
                    _trainingCompleted = false;
                    _validationIssues = System.Array.Empty<ValidationIssue>();
                    _hasValidationResult = true;
                    _controlPanel.ResetTraining();
                }
                _controlPanel.ShowState(_execution.State, _execution.HasTrainedModel,
                    !_execution.IsBusy && _execution.HasTrainedModel, _execution.CanCancel);
                _modelUIIsDirty = true;
            }
        }

        private void HandleTrainingProgress(TrainingProgress progress) => _controlPanel.ShowProgress(progress);
        private void HandleTrainingCancelled() => _controlPanel.ShowCancelled();
        private void HandleInferenceCompleted(InferenceResult result) => _controlPanel.ShowInference(result);
        private void HandleExecutionError(string message) => _controlPanel.ShowError(message);

        private void HandleTrainingCompleted(TrainingResult result)
        {
            _controlPanel.ShowCompleted(result);
            _trainingCompleted = result != null;
        }

        private void HandleModelChanged()
        {
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
            _trainingCompleted = false;
            _hasValidationResult = false;
            _validationIssues = System.Array.Empty<ValidationIssue>();
            _execution.CloseSession();
            if (_initialized)
            {
                _builder.EndInteraction();
                _controlPanel.ResetSessionUI();
            }
            _settings.Close();
            _xrInput.DisableInput();
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
