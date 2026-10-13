using System.Collections.Generic;
using ModelDesign;
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
            _training.OnStopRequested += FinishTraining;

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
        }

        private void LateUpdate()
        {
            if (_initialized)
            {
                _conveyor.PreviewPlacement(_grabbedBlock);
            }
        }

        public void FinishTraining()
        {
            // A future training completion callback restores the start button through this method.
            _training.SetTrainingState(false);
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

        private void HandleTrainingStartRequested()
        {
            if (!isActiveAndEnabled || _training.IsTraining || _grabbedBlock != null)
            {
                return;
            }
            if (!ModelJsonSerializer.TrySerialize(_workspace, out string json,
                out IReadOnlyList<ValidationIssue> issues))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                foreach (ValidationIssue issue in issues)
                {
                    Debug.LogWarning($"[ModelDesign] 모델 JSON 변환 실패: {issue.Path}: {issue.Message}", this);
                }
#endif
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Temporary preview only: server training requests are not connected yet.
            Debug.Log($"[ModelDesign] 서버에 보낼 모델 JSON:\n{json}", this);
#endif
            _training.SetTrainingState(true);
        }

        private void OnDisable()
        {
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
                _training.OnStopRequested -= FinishTraining;
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
            _workspace?.Dispose();
        }
    }
}
