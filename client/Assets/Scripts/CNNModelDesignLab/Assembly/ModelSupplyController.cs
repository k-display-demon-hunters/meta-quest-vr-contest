using System;
using System.Collections.Generic;
using ModelDesignScene.Editing;
using UnityEngine;

namespace ModelDesignScene.Assembly
{
    public sealed class ModelSupplyController : MonoBehaviour
    {
        [SerializeField] private Transform _paletteRoot;
        [SerializeField] private PaletteBlock[] _paletteBlocks;

        private readonly Dictionary<ModelBlockController, Pose> _paletteSlots = new Dictionary<ModelBlockController, Pose>();
        private readonly Dictionary<LayerType, ModelBlockController> _prefabs = new Dictionary<LayerType, ModelBlockController>();
        private Func<LayerType, LayerDefinition> _createLayer;

        internal event Action<ModelBlockController> OnBlockCreated;

        internal void Initialize(Func<LayerType, LayerDefinition> createLayer)
        {
            _createLayer = createLayer;
            _paletteSlots.Clear();
            _prefabs.Clear();
            foreach (PaletteBlock entry in _paletteBlocks)
            {
                _prefabs.Add(entry.Type, entry.Prefab);
                InitializeBlock(entry.Block, entry.Type);
            }
        }

        internal bool TryTakeBlock(ModelBlockController block)
        {
            if (!_paletteSlots.TryGetValue(block, out Pose slot))
            {
                return false;
            }

            _paletteSlots.Remove(block);
            block.TakeFromPalette();
            // Refill the fixed supply position with a fresh block of the same type.
            ModelBlockController replacement = Instantiate(_prefabs[block.Layer.Type], _paletteRoot);
            replacement.transform.SetLocalPositionAndRotation(slot.position, slot.rotation);
            InitializeBlock(replacement, block.Layer.Type);
            return true;
        }

        private void InitializeBlock(ModelBlockController block, LayerType type)
        {
            block.Initialize(_createLayer(type), true);
            _paletteSlots.Add(block, new Pose(block.transform.localPosition, block.transform.localRotation));
            OnBlockCreated?.Invoke(block);
        }

        [Serializable]
        private sealed class PaletteBlock
        {
            [SerializeField] private LayerType _type;
            [SerializeField] private ModelBlockController _block;
            [SerializeField] private ModelBlockController _prefab;

            public LayerType Type => _type;
            public ModelBlockController Block => _block;
            public ModelBlockController Prefab => _prefab;
        }
    }
}
