using System.Collections.Generic;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelBuilderController : MonoBehaviour
    {
        [SerializeField] private ModelBlockController _inputBlock;
        [SerializeField] private ModelBlockController _outputBlock;
        [SerializeField] private ModelSupplyController _supply;
        [SerializeField] private ModelConveyorController _conveyor;

        private readonly HashSet<ModelBlockController> _blocks = new HashSet<ModelBlockController>();
        private ModelBuilderManager _manager;
        private ConveyorSlots _slots;
        private ModelDefinition _model;
        private ModelBlockController _grabbedBlock;
        private bool _initialized;
        private bool _interactionEnabled;

        internal bool IsDragging => _grabbedBlock != null;

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
            for (int slot = 0; slot < _slots.Count; slot++)
            {
                ModelBlockController block = _slots[slot];
                if (block == null) continue;
                block.RefreshLabel(inputChannels);
                if (block.Layer.Type == LayerType.Conv2D && inputChannels > 0)
                {
                    inputChannels = block.Layer.Filters;
                }
                else if (block.Layer.Type == LayerType.Flatten || block.Layer.Type == LayerType.Dense)
                {
                    inputChannels = 0;
                }
            }
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
            if (block.IsPalette)
            {
                _supply.TryTakeBlock(block);
            }
            RemoveFromConveyor(block);
            _grabbedBlock = block;
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
                _conveyor.AlignBlock(slot, block);
                _manager.NotifyChanged();
            }
            else
            {
                DeleteBlock(block);
            }
        }

        private void HandleSettingsRequested(SlotSettingsController control, IXRActivateInteractor interactor)
        {
            if (!_interactionEnabled || IsDragging || control.Block == null)
            {
                return;
            }
            Collider hit = _manager.GetActivationHit(interactor);
            if (hit != null && hit.GetComponentInParent<SlotSettingsController>() == control)
            {
                Transform anchor = control.PopupAnchor;
                _manager.RequestSettings(control.Block.Layer, new Pose(anchor.position, anchor.rotation));
            }
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
