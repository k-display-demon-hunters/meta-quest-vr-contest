using System.Collections.Generic;
using ModelDesignScene.Editing;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ModelDesignScene.Assembly
{
    public sealed class ModelAssemblyController : MonoBehaviour
    {
        [SerializeField] private ModelBlockController _inputBlock;
        [SerializeField] private ModelBlockController _outputBlock;
        [SerializeField] private ModelSupplyController _supply;
        [SerializeField] private ModelConveyorController _conveyor;

        private readonly HashSet<ModelBlockController> _blocks = new HashSet<ModelBlockController>();
        private ModelAssemblyManager _manager;
        private ConveyorSlots _slots;
        private ModelBlockController _grabbedBlock;
        private bool _initialized;
        private bool _interactionEnabled;

        internal bool IsDragging => _grabbedBlock != null;

        internal void Initialize(ModelAssemblyManager manager, LayerDefinition input, LayerDefinition output)
        {
            _manager = manager;
            _slots = new ConveyorSlots(_conveyor.SlotCount);
            _inputBlock.Initialize(input, false);
            _outputBlock.Initialize(output, false);
            RegisterBlock(_inputBlock);
            RegisterBlock(_outputBlock);
            _supply.OnBlockCreated += RegisterBlock;
            _supply.Initialize(_manager.CreateLayer);
            _conveyor.Initialize(_slots);
            foreach (SlotSettingsController control in _conveyor.SettingsControls)
            {
                control.OnSettingsRequested += HandleSettingsRequested;
            }
            _initialized = true;
            RefreshOrder();
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
            // A released/cancelled loose block must not survive as an invisible workspace fragment.
            ModelBlockController looseBlock = _grabbedBlock;
            _grabbedBlock = null;
            if (looseBlock != null)
            {
                DeleteBlock(looseBlock);
            }
        }

        internal IReadOnlyList<LayerDefinition> GetSlotLayers()
        {
            LayerDefinition[] layers = new LayerDefinition[_slots.Count];
            for (int index = 0; index < layers.Length; index++)
            {
                layers[index] = _slots[index] != null ? _slots[index].Layer : null;
            }
            return System.Array.AsReadOnly(layers);
        }

        internal void RefreshLabels()
        {
            foreach (ModelBlockController block in _blocks)
            {
                block.RefreshLabel();
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
                _conveyor.RefreshSlot(slot);
                RefreshOrder();
            }
        }

        private void RefreshOrder()
        {
            List<LayerDefinition> layers = new List<LayerDefinition>();
            for (int index = 0; index < _slots.Count; index++)
            {
                if (_slots[index] != null)
                {
                    layers.Add(_slots[index].Layer);
                }
            }
            _manager.ApplyOrder(layers);
        }

        private void DeleteBlock(ModelBlockController block)
        {
            RemoveFromConveyor(block);
            _manager.RemoveLayer(block.Layer);
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
            if (block.IsPalette && _supply.TryTakeBlock(block))
            {
                _manager.RegisterLayer(block.Layer);
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
                _conveyor.AlignBlock(slot, block);
                RefreshOrder();
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
