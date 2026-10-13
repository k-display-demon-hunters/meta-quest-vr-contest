using System.Collections.Generic;
using ModelDesign;
using Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene
{
    public sealed class CNNModelDesignLabManager : MonoBehaviour
    {
        [SerializeField] private ModelBlockController _inputBlock;
        [SerializeField] private ModelBlockController _outputBlock;
        [SerializeField] private ModelSupplyController _supply;
        [SerializeField] private ModelConveyorController _conveyor;
        [SerializeField] private ModelSettingsController _settings;
        [SerializeField] private ModelTrainingController _training;
        [SerializeField] private ModelExecutionController _execution;
        [SerializeField] private ModelResultsController _results;
        [SerializeField] private ModelXRInputController _xrInput;

        private readonly HashSet<ModelBlockController> _blocks = new HashSet<ModelBlockController>();
        private readonly List<LayerDefinition> _connectedLayers = new List<LayerDefinition>();
        private ModelWorkspace _workspace;
        private ModelBlockController _grabbedBlock;
        private bool _initialized;

        private void Awake()
        {
            _workspace = new ModelWorkspace();
        }

        private void OnEnable()
        {
            _xrInput.EnableInput();
            foreach (ModelBlockController block in _blocks)
            {
                block.SetInteractionEnabled(true);
            }
        }

        private void Start()
        {
            _supply.OnBlockCreated += RegisterBlock;
            _settings.OnDeleteRequested += HandleDeleteRequested;
            _training.OnStartRequested += HandleTrainingStartRequested;
            _training.OnStopRequested += HandleTrainingStopRequested;
            _execution.OnStateChanged += HandleExecutionStateChanged;
            _execution.OnTrainingProgress += HandleTrainingProgress;
            _execution.OnTrainingCompleted += HandleTrainingCompleted;
            _execution.OnTrainingCancelled += HandleTrainingCancelled;
            _execution.OnInferenceCompleted += HandleInferenceCompleted;
            _execution.OnError += HandleExecutionError;
            _results.OnInferenceRequested += HandleInferenceRequested;

            // All scene blocks have completed Awake before their model data is assigned.
            _inputBlock.Initialize(_workspace.Input, false);
            _outputBlock.Initialize(_workspace.Output, false);
            RegisterBlock(_inputBlock);
            RegisterBlock(_outputBlock);
            _supply.Initialize();
            _conveyor.Initialize(_inputBlock, _outputBlock);
            foreach (SlotSettingsController control in _conveyor.SettingsControls)
            {
                control.OnSettingsRequested += HandleSettingsRequested;
            }
            RefreshModelOrder();
            _initialized = true;
            _workspace.OnChanged += HandleWorkspaceChanged;
            HandleExecutionStateChanged();
        }

        private void LateUpdate()
        {
            if (_initialized)
            {
                _conveyor.PreviewPlacement(_grabbedBlock);
            }
        }

        private void RegisterBlock(ModelBlockController block)
        {
            if (!_blocks.Add(block))
            {
                return;
            }
            block.OnGrabbed += HandleBlockGrabbed;
            block.OnReleased += HandleBlockReleased;
            block.SetInteractionEnabled(isActiveAndEnabled);
        }

        private void UnregisterBlock(ModelBlockController block)
        {
            block.SetInteractionEnabled(false);
            block.OnGrabbed -= HandleBlockGrabbed;
            block.OnReleased -= HandleBlockReleased;
            _blocks.Remove(block);
        }

        private bool IsWorkspaceBlock(ModelBlockController block)
        {
            return block != null && !block.IsPalette && block.Layer != null && _blocks.Contains(block);
        }

        private void RemoveFromConveyor(ModelBlockController block)
        {
            if (_conveyor.RemoveBlock(block))
            {
                RefreshModelOrder();
            }
        }

        private void RefreshModelOrder()
        {
            for (int index = 1; index < _connectedLayers.Count; index++)
            {
                _workspace.Disconnect(_connectedLayers[index - 1], _connectedLayers[index]);
            }

            _connectedLayers.Clear();
            _connectedLayers.Add(_workspace.Input);
            for (int index = 0; index < _conveyor.SlotCount; index++)
            {
                ModelBlockController block = _conveyor.GetBlock(index);
                if (block != null)
                {
                    _connectedLayers.Add(block.Layer);
                }
            }
            _connectedLayers.Add(_workspace.Output);

            // Empty slots do not create layers; physical block positions stay unchanged.
            for (int index = 1; index < _connectedLayers.Count; index++)
            {
                _workspace.Connect(_connectedLayers[index - 1], _connectedLayers[index]);
            }
        }

        private void HandleBlockGrabbed(ModelBlockController block)
        {
            if (!isActiveAndEnabled || !_blocks.Contains(block))
            {
                return;
            }
            _settings.Close();
            if (block.IsPalette && _supply.TryTakeBlock(block))
            {
                _workspace.AddLayer(block.Layer);
            }
            RemoveFromConveyor(block);
            _grabbedBlock = block;
        }

        private void HandleBlockReleased(ModelBlockController block, bool cancelled)
        {
            if (!isActiveAndEnabled || _grabbedBlock != block)
            {
                return;
            }
            // Taken blocks belong to the workspace, not the parts supply rack.
            block.transform.SetParent(transform, true);
            _conveyor.ClearPreview();
            if (!cancelled && _conveyor.TryPlaceBlock(block))
            {
                RefreshModelOrder();
            }
            _grabbedBlock = null;
        }

        private void HandleSettingsRequested(SlotSettingsController control, IXRActivateInteractor interactor)
        {
            if (isActiveAndEnabled && _grabbedBlock == null && IsWorkspaceBlock(control.Block) &&
                _xrInput.CanActivateSlot(control, interactor))
            {
                _settings.Open(control);
            }
        }

        private void HandleDeleteRequested(ModelBlockController block)
        {
            if (!isActiveAndEnabled || !IsWorkspaceBlock(block) || block.IsFixed)
            {
                return;
            }
            _settings.Close();
            // Disconnect the old sequence before removing its layer from the workspace.
            RemoveFromConveyor(block);
            _workspace.RemoveLayer(block.Layer);
            UnregisterBlock(block);
            // Remove interaction immediately; Unity destroys the object at the end of the frame.
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }

        private async void HandleTrainingStartRequested()
        {
            if (!isActiveAndEnabled || _execution.IsBusy || _grabbedBlock != null)
            {
                return;
            }
            if (!ModelJsonSerializer.TrySerialize(_workspace, out string json,
                out IReadOnlyList<ValidationIssue> issues))
            {
                _results.ShowError("모델 구성을 확인하세요.\n" + issues[0].Message);
                return;
            }
            _settings.Close();
            _results.ResetTraining();
            await _execution.StartTrainingAsync(json);
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
            _training.ShowState(_execution.State, _execution.IsBusy, _execution.CanCancel);
            _results.ShowState(_execution.State, _execution.HasTrainedModel,
                !_execution.IsBusy && _execution.HasTrainedModel);
            HandleWorkspaceChanged();
        }

        private void HandleTrainingProgress(TrainingProgress progress)
        {
            _results.ShowProgress(progress);
        }

        private void HandleTrainingCompleted(TrainingResult result)
        {
            _results.ShowCompleted(result);
        }

        private void HandleTrainingCancelled()
        {
            _results.ShowCancelled();
        }

        private void HandleInferenceCompleted(InferenceResult result)
        {
            _results.ShowInference(result);
        }

        private void HandleExecutionError(string message)
        {
            _results.ShowError(message);
        }

        private void HandleWorkspaceChanged()
        {
            string submitted = _execution.SubmittedModelJson;
            bool changed = submitted != null &&
                (!ModelJsonSerializer.TrySerialize(_workspace, out string current, out _) ||
                 !string.Equals(submitted, current, System.StringComparison.Ordinal));
            _results.SetModelChanged(changed);
        }

        private void OnDisable()
        {
            _execution.CloseSession();
            if (_initialized)
            {
                _results.ResetSessionUI();
            }
            _conveyor.ClearPreview();
            _settings.Close();
            foreach (ModelBlockController block in _blocks)
            {
                if (block == null)
                {
                    continue;
                }
                block.SetInteractionEnabled(false);
                if (block.IsGrabbed)
                {
                    block.CancelGrab();
                    block.transform.SetParent(transform, true);
                }
            }
            _grabbedBlock = null;
            _xrInput.DisableInput();
        }

        private void OnDestroy()
        {
            if (_supply != null)
            {
                _supply.OnBlockCreated -= RegisterBlock;
            }
            if (_settings != null)
            {
                _settings.OnDeleteRequested -= HandleDeleteRequested;
            }
            if (_training != null)
            {
                _training.OnStartRequested -= HandleTrainingStartRequested;
                _training.OnStopRequested -= HandleTrainingStopRequested;
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
            }
            if (_conveyor != null)
            {
                foreach (SlotSettingsController control in _conveyor.SettingsControls)
                {
                    if (control != null)
                    {
                        control.OnSettingsRequested -= HandleSettingsRequested;
                    }
                }
            }
            foreach (ModelBlockController block in _blocks)
            {
                if (block != null)
                {
                    block.OnGrabbed -= HandleBlockGrabbed;
                    block.OnReleased -= HandleBlockReleased;
                }
            }
            if (_workspace != null)
            {
                _workspace.OnChanged -= HandleWorkspaceChanged;
                _workspace.Dispose();
            }
        }
    }
}
