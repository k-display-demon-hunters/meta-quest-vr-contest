using System.Collections.Generic;
using ModelDesignScene.Model;
using UnityEngine;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelBuilderController : MonoBehaviour
    {
        [SerializeField] private ModelBlockController _inputBlock;
        [SerializeField] private ModelBlockController _outputBlock;
        [SerializeField] private ModelSupplyController _supply;
        [SerializeField] private ModelConveyorController _conveyor;
        [SerializeField] private PlacementExampleController _placementExample;
        [SerializeField] private TrainingFlowController _trainingFlow;
        [SerializeField] private Room3EntryGuide _entryGuide;

        private readonly HashSet<ModelBlockController> _blocks = new HashSet<ModelBlockController>();
        private ModelBuilderManager _manager;
        private ConveyorSlots _slots;
        private ModelDefinition _model;
        private ModelBlockController _grabbedBlock;
        private bool _initialized;
        private bool _interactionEnabled;
        private bool _placementGuideAvailable;

        internal bool IsDragging => _grabbedBlock != null;
        internal PlacementGuideState PlacementGuideState => _entryGuide.State;
        internal int PlacementGuideSlot => _entryGuide.TargetSlotIndex;
        internal event System.Action OnPlacementGuideChanged
        {
            add => _entryGuide.OnChanged += value;
            remove => _entryGuide.OnChanged -= value;
        }

        internal bool CanPlayPlacementExample
        {
            get
            {
                if (!_initialized || !_interactionEnabled || IsDragging || _placementExample.IsPlaying) { return false; }
                for (int index = 0; index < _slots.Count; index++)
                {
                    if (_slots[index] == null) { return true; }
                }
                return false;
            }
        }

        internal bool PlayPlacementExample(System.Func<IReadOnlyList<LayerDefinition>, bool> isValid)
        {
            if (!CanPlayPlacementExample) { return false; }
            (LayerType Type, int SlotIndex)? placement = FindPlacementExample(isValid);
            if (!placement.HasValue) { return false; }
            _entryGuide.Hide();
            var display = System.Array.Find(_supply.GetExampleDisplays(),
                candidate => candidate.Block.Layer.Type == placement.Value.Type);
            _placementExample.Play(display, placement.Value.SlotIndex, _conveyor);
            return true;
        }

        private (LayerType Type, int SlotIndex)? FindPlacementExample(
            System.Func<IReadOnlyList<LayerDefinition>, bool> isValid)
        {
            LayerDefinition[] candidate = new LayerDefinition[_model.Layers.Count];
            bool hasConv = false;
            bool hasFlatten = false;
            for (int slot = 0; slot < candidate.Length; slot++)
            {
                LayerDefinition layer = _model.Layers[slot];
                candidate[slot] = layer;
                hasConv |= layer?.Type == LayerType.Conv2D;
                hasFlatten |= layer?.Type == LayerType.Flatten;
            }
            if (hasConv && hasFlatten) { return null; }

            LayerType nextType = hasConv ? LayerType.Flatten : LayerType.Conv2D;
            LayerDefinition nextLayer = LayerDefinition.CreateDefault(nextType);
            LayerDefinition followingFlatten = !hasConv && !hasFlatten
                ? LayerDefinition.CreateDefault(LayerType.Flatten) : null;
            for (int slot = 0; slot < candidate.Length; slot++)
            {
                if (candidate[slot] != null) { continue; }
                candidate[slot] = nextLayer;
                bool valid = false;
                if (followingFlatten == null)
                {
                    valid = isValid(candidate);
                }
                else
                {
                    // CONV만 시연해도 이후 FLATTEN까지 놓을 수 있는 위치를 고른다.
                    for (int flattenSlot = 0; flattenSlot < candidate.Length; flattenSlot++)
                    {
                        if (candidate[flattenSlot] != null) { continue; }
                        candidate[flattenSlot] = followingFlatten;
                        valid = isValid(candidate);
                        candidate[flattenSlot] = null;
                        if (valid) { break; }
                    }
                }
                candidate[slot] = null;
                if (valid) { return (nextType, slot); }
            }
            return null;
        }

        internal void CancelPlacementExample() => _placementExample.Cancel();
        internal void SetPlacementGuideAvailable(bool available)
        {
            _placementGuideAvailable = available;
            if (!available) { _entryGuide.Hide(); }
        }

        internal void PlayHologramFlow((Vector3 Center, Vector2 Extents, bool Trainable)[] frames,
            bool training, float readyAt)
        {
            if (frames.Length < 2) { return; }
            var occupied = new List<(ModelBlockController Block, int Slot)>(_slots.Count);
            if (training)
            {
                for (int slot = 0; slot < _slots.Count; slot++)
                {
                    ModelBlockController block = _slots[slot];
                    if (block != null) { occupied.Add((block, slot)); }
                }
            }
            _trainingFlow.Play(frames, occupied, IsOriginalFlowBlock, training, readyAt);
        }

        private bool IsOriginalFlowBlock(int slot, ModelBlockController block) => _slots[slot] == block;

        internal void ShowEpochFlow(int epoch) => _trainingFlow.QueueEpoch(epoch);
        internal void FinishTrainingFlow() => _trainingFlow.FinishTraining();
        internal void FadeModelFlow() => _trainingFlow.Fade();
        internal void CancelTrainingFlow() => _trainingFlow.Cancel();

        internal void Initialize(ModelBuilderManager manager, ModelDefinition model)
        {
            _manager = manager;
            _model = model;
            _slots = new ConveyorSlots(ModelDefinition.SlotCount);
            _inputBlock.Initialize(model.Input, false);
            _outputBlock.Initialize(model.Output, false);
            RegisterBlock(_inputBlock);
            RegisterBlock(_outputBlock);
            _supply.OnBlockCreated += RegisterBlock;
            _supply.Initialize(LayerDefinition.CreateDefault);
            _conveyor.Initialize(_slots);
            _entryGuide.Initialize();
            foreach (SlotSettingsController control in _conveyor.SettingsControls)
            {
                control.OnSettingsRequested += HandleSettingsRequested;
            }
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (_initialized && _interactionEnabled)
            {
                _conveyor.PreviewPlacement(_grabbedBlock);
                _entryGuide.UpdateGuide(_placementGuideAvailable && !_placementExample.IsPlaying,
                    IsDragging, _conveyor.AvailablePlacementSlot);
            }
        }

        internal void BeginInteraction()
        {
            _interactionEnabled = true;
            foreach (ModelBlockController block in _blocks)
            {
                block.SetInteractionEnabled(true);
            }
        }

        internal void EndInteraction()
        {
            _interactionEnabled = false;
            _entryGuide.Hide();
            _placementExample.Cancel();
            _trainingFlow.Cancel();
            if (!_initialized)
            {
                return;
            }
            _conveyor.ClearPreview();
            foreach (ModelBlockController block in _blocks)
            {
                block.SetInteractionEnabled(false);
                if (block.IsGrabbed)
                {
                    block.CancelGrab();
                    block.transform.SetParent(transform, true);
                }
            }
            // Discard the loose block when interaction ends; only placed blocks remain.
            ModelBlockController looseBlock = _grabbedBlock;
            _grabbedBlock = null;
            if (looseBlock != null)
            {
                DeleteBlock(looseBlock);
            }
        }

        internal (Vector3Int Grid, int Elements, Vector3 Origin)[] ReadHologramLayers()
        {
            var layers = new List<(Vector3Int Grid, int Elements, Vector3 Origin)>(_slots.Count + 2);
            AddLayer(_inputBlock);
            for (int slot = 0; slot < _slots.Count; slot++)
            {
                ModelBlockController block = _slots[slot];
                if (block != null) { AddLayer(block); }
            }
            AddLayer(_outputBlock);
            return layers.ToArray();

            void AddLayer(ModelBlockController block)
            {
                layers.Add((block.SpatialOutputSize, block.VectorOutputSize, (block.FlowStart + block.FlowEnd) * 0.5f));
            }
        }

        internal string[] ReadOutputSizes()
        {
            var sizes = new List<string> { _inputBlock.OutputSizeLabel };
            for (int slot = 0; slot < _slots.Count; slot++)
            {
                ModelBlockController block = _slots[slot];
                if (block != null) { sizes.Add(block.OutputSizeLabel); }
            }
            sizes.Add(_outputBlock.OutputSizeLabel);
            return sizes.ToArray();
        }

        internal void RefreshLabels()
        {
            foreach (ModelBlockController block in _blocks)
            {
                if (block.IsPalette || block.IsFixed || block.IsGrabbed)
                {
                    block.RefreshLabel();
                }
            }
            int inputChannels = LayerLimits.InputChannels;
            int inputHeight = LayerLimits.InputHeight;
            int inputWidth = LayerLimits.InputWidth;
            int inputElements = 0;
            List<Vector3> connections = new List<Vector3>();
            ModelBlockController previousBlock = _inputBlock;
            for (int slot = 0; slot < _slots.Count; slot++)
            {
                ModelBlockController block = _slots[slot];
                if (block == null) continue;
                block.RefreshLabel(inputChannels, inputHeight, inputWidth, inputElements);
                _conveyor.SettingsControls[slot].RefreshLabel();
                connections.Add(previousBlock.FlowEnd);
                connections.Add(block.FlowStart);
                previousBlock = block;
                if (block.Layer.Type == LayerType.Conv2D)
                {
                    inputElements = 0;
                    if (inputChannels > 0)
                    {
                        inputHeight = Mathf.Max(0, inputHeight - block.Layer.Height + 1);
                        inputWidth = Mathf.Max(0, inputWidth - block.Layer.Width + 1);
                        inputChannels = inputHeight > 0 && inputWidth > 0 ? block.Layer.Filters : 0;
                    }
                }
                else if (block.Layer.Type == LayerType.MaxPooling2D)
                {
                    inputElements = 0;
                    if (inputChannels > 0)
                    {
                        inputHeight /= block.Layer.Height;
                        inputWidth /= block.Layer.Width;
                        if (inputHeight == 0 || inputWidth == 0)
                        {
                            inputChannels = 0;
                        }
                    }
                }
                else if (block.Layer.Type == LayerType.Flatten)
                {
                    inputElements = inputHeight * inputWidth * inputChannels;
                    inputChannels = 0;
                }
                else if (block.Layer.Type == LayerType.Dense)
                {
                    inputElements = inputElements > 0 ? block.Layer.Units : 0;
                    inputChannels = 0;
                }
                if (inputChannels == 0)
                {
                    inputHeight = 0;
                    inputWidth = 0;
                }
            }
            connections.Add(previousBlock.FlowEnd);
            connections.Add(_outputBlock.FlowStart);
            _conveyor.RefreshConnections(connections);
        }

        internal void DeleteLayer(LayerDefinition layer)
        {
            ModelBlockController target = null;
            foreach (ModelBlockController block in _blocks)
            {
                if (ReferenceEquals(block.Layer, layer))
                {
                    target = block;
                    break;
                }
            }
            if (_interactionEnabled && target != null && !target.IsFixed && !target.IsPalette)
            {
                DeleteBlock(target);
            }
        }

        private void RegisterBlock(ModelBlockController block)
        {
            _blocks.Add(block);
            block.OnGrabbed += HandleBlockGrabbed;
            block.OnReleased += HandleBlockReleased;
            block.SetInteractionEnabled(_interactionEnabled);
        }

        private void UnregisterBlock(ModelBlockController block)
        {
            block.SetInteractionEnabled(false);
            block.OnGrabbed -= HandleBlockGrabbed;
            block.OnReleased -= HandleBlockReleased;
            _blocks.Remove(block);
        }

        private void RemoveFromConveyor(ModelBlockController block)
        {
            int slot = _slots.Remove(block);
            if (slot >= 0)
            {
                _model.RemoveLayer(slot);
                _conveyor.RefreshSlot(slot);
            }
        }

        private void DeleteBlock(ModelBlockController block)
        {
            RemoveFromConveyor(block);
            UnregisterBlock(block);
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
            _manager.NotifyChanged();
        }

        private void HandleBlockGrabbed(ModelBlockController block)
        {
            if (!_interactionEnabled)
            {
                return;
            }
            _placementExample.Cancel();
            if (block.IsPalette)
            {
                _supply.TryTakeBlock(block);
            }
            RemoveFromConveyor(block);
            block.SetPose(block.transform.position, _conveyor.GetSlotPose(0).rotation);
            _grabbedBlock = block;
            _entryGuide.BlockGrabbed();
            _manager.NotifyChanged();
        }

        private void HandleBlockReleased(ModelBlockController block, bool cancelled)
        {
            if (!_interactionEnabled || _grabbedBlock != block)
            {
                return;
            }
            block.transform.SetParent(transform, true);
            _conveyor.ClearPreview();
            _grabbedBlock = null;
            int slot = _conveyor.FindSlot(block.transform.position);
            if (!cancelled && _slots.TryPlace(slot, block))
            {
                _model.PlaceLayer(slot, block.Layer);
                block.transform.localScale = Vector3.one;
                _conveyor.AlignBlock(slot, block);
                _entryGuide.BlockReleased(true);
                _manager.NotifyChanged();
            }
            else
            {
                DeleteBlock(block);
                _entryGuide.BlockReleased(false);
            }
        }

        private void HandleSettingsRequested(SlotSettingsController control)
        {
            if (!_interactionEnabled || IsDragging)
            {
                return;
            }
            _placementExample.Cancel();
            Transform anchor = control.PopupAnchor;
            _manager.RequestSettings(control.Block.Layer, new Pose(anchor.position, anchor.rotation));
        }

        private void OnDestroy()
        {
            if (!_initialized)
            {
                return;
            }
            if (_supply != null)
            {
                _supply.OnBlockCreated -= RegisterBlock;
            }
            foreach (SlotSettingsController control in _conveyor.SettingsControls)
            {
                if (control != null)
                {
                    control.OnSettingsRequested -= HandleSettingsRequested;
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
        }
    }
}
