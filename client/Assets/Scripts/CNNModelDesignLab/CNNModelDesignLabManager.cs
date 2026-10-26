using System.Collections.Generic;
using ModelDesignScene.Assembly;
using ModelDesignScene.BlockSettings;
using ModelDesignScene.Editing;
using ModelDesignScene.Execution;
using ModelDesignScene.Execution.Engine;
using ModelDesignScene.Results;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene
{
    public sealed class CNNModelDesignLabManager : MonoBehaviour
    {
        [SerializeField] private ModelAssemblyManager _assembly;
        [SerializeField] private ModelBlockSettingsManager _settings;
        [SerializeField] private ModelExecutionManager _execution;
        [SerializeField] private ModelResultsManager _results;
        [SerializeField] private ModelXRInputController _xrInput;

        private ModelEditingManager _editing;
        private bool _initialized;
        private bool _trainingCompleted;
        private bool _workspaceUIIsDirty;

        public bool ReadyForFactoryExit => _trainingCompleted && !_execution.IsBusy;

        private void Awake()
        {
            _editing = new ModelEditingManager();
        }

        private void OnEnable()
        {
            _workspaceUIIsDirty = true;
            _xrInput.EnableInput();
            if (_initialized)
            {
                _assembly.BeginInteraction();
            }
        }

        private void Start()
        {
            _editing.OnChanged += HandleWorkspaceChanged;
            _assembly.OnChanged += HandleAssemblyChanged;
            _assembly.OnSettingsRequested += HandleSettingsRequested;
            _settings.OnDeleteRequested += HandleDeleteRequested;
            _execution.OnStateChanged += HandleExecutionStateChanged;
            _execution.OnTrainingProgress += HandleTrainingProgress;
            _execution.OnTrainingCompleted += HandleTrainingCompleted;
            _execution.OnTrainingCancelled += HandleTrainingCancelled;
            _execution.OnInferenceCompleted += HandleInferenceCompleted;
            _execution.OnError += HandleExecutionError;
            _results.OnInferenceRequested += HandleInferenceRequested;
            _results.OnStartRequested += HandleTrainingStartRequested;
            _results.OnStopRequested += HandleTrainingStopRequested;

            // Scene components have completed Awake before the module managers connect them.
            _settings.Initialize(_editing);
            _assembly.Initialize(_editing, GetActivationHit);
            _assembly.BeginInteraction();
            _initialized = true;
            HandleExecutionStateChanged();
        }

        private void LateUpdate()
        {
            if (_initialized && _workspaceUIIsDirty)
            {
                RefreshWorkspaceUI();
            }
        }

        private Collider GetActivationHit(IXRActivateInteractor interactor)
        {
            return _xrInput.GetActivationHit(interactor);
        }

        private void HandleAssemblyChanged()
        {
            _settings.Close();
            HandleWorkspaceChanged();
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
            _assembly.DeleteLayer(layer);
        }

        private async void HandleTrainingStartRequested()
        {
            if (!isActiveAndEnabled || _execution.IsBusy || _assembly.IsDragging)
            {
                return;
            }
            bool valid = _editing.TryBuild(_execution.TrainingOptions,
                out ModelExecutionDefinition definition, out IReadOnlyList<ValidationIssue> issues);
            PublishWorkspaceUI(valid, definition, issues, !valid);
            if (!valid)
            {
                return;
            }
            _settings.Close();
            _results.ResetTraining();
            _trainingCompleted = false;
            await _execution.StartTrainingAsync(definition);
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
                _results.ShowState(_execution.State, _execution.HasTrainedModel,
                    !_execution.IsBusy && _execution.HasTrainedModel, _execution.CanCancel);
                HandleWorkspaceChanged();
            }
        }

        private void HandleTrainingProgress(TrainingProgress progress) => _results.ShowProgress(progress);
        private void HandleTrainingCancelled() => _results.ShowCancelled();
        private void HandleInferenceCompleted(InferenceResult result) => _results.ShowInference(result);
        private void HandleExecutionError(string message) => _results.ShowError(message);

        private void HandleTrainingCompleted(TrainingResult result)
        {
            _results.ShowCompleted(result);
            _trainingCompleted = result != null;
        }

        private void HandleWorkspaceChanged()
        {
            _workspaceUIIsDirty = true;
        }

        private void RefreshWorkspaceUI()
        {
            _assembly.RefreshLabels();
            _settings.Refresh();
            bool valid = _editing.TryBuild(_execution.TrainingOptions,
                out ModelExecutionDefinition definition, out IReadOnlyList<ValidationIssue> issues);
            PublishWorkspaceUI(valid, definition, issues, false);
        }

        private void PublishWorkspaceUI(bool valid, ModelExecutionDefinition definition,
            IReadOnlyList<ValidationIssue> issues, bool focus)
        {
            _workspaceUIIsDirty = false;
            _results.ShowModelValidation(issues, _assembly.SlotLayers, _assembly.IsDragging, focus);
            ModelExecutionDefinition submitted = _execution.SubmittedModel;
            bool changed = submitted != null && (!valid || !_editing.Matches(submitted, definition));
            _results.SetModelChanged(changed);
        }

        private void OnDisable()
        {
            _trainingCompleted = false;
            _execution.CloseSession();
            if (_initialized)
            {
                _assembly.EndInteraction();
                _results.ResetSessionUI();
            }
            _settings.Close();
            _xrInput.DisableInput();
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                if (_assembly != null)
                {
                    _assembly.OnChanged -= HandleAssemblyChanged;
                    _assembly.OnSettingsRequested -= HandleSettingsRequested;
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
                }
                if (_results != null)
                {
                    _results.OnInferenceRequested -= HandleInferenceRequested;
                    _results.OnStartRequested -= HandleTrainingStartRequested;
                    _results.OnStopRequested -= HandleTrainingStopRequested;
                }
            }
            _editing.OnChanged -= HandleWorkspaceChanged;
            _editing.Dispose();
        }
    }
}
