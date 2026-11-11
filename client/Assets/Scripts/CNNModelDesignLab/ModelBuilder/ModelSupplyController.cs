using System;
using System.Collections.Generic;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelSupplyController : MonoBehaviour
    {
        [SerializeField] private Transform _paletteRoot;
        [SerializeField] private PaletteBlock[] _paletteBlocks;

        private readonly Dictionary<ModelBlockController, Pose> _paletteSlots = new Dictionary<ModelBlockController, Pose>();
        private readonly Dictionary<LayerType, PaletteBlock> _paletteByType = new Dictionary<LayerType, PaletteBlock>();
        private Func<LayerType, LayerDefinition> _createLayer;

        internal event Action<ModelBlockController> OnBlockCreated;

        internal void Initialize(Func<LayerType, LayerDefinition> createLayer)
        {
            _createLayer = createLayer;
            _paletteSlots.Clear();
            _paletteByType.Clear();
            foreach (PaletteBlock entry in _paletteBlocks)
            {
                _paletteByType.Add(entry.Type, entry);
                InitializeBlock(entry.Block, entry);
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
            PaletteBlock entry = _paletteByType[block.Layer.Type];
            ModelBlockController replacement = Instantiate(entry.Prefab, _paletteRoot);
            replacement.transform.SetLocalPositionAndRotation(slot.position, slot.rotation);
            InitializeBlock(replacement, entry);
            return true;
        }

        private void InitializeBlock(ModelBlockController block, PaletteBlock entry)
        {
            block.Initialize(_createLayer(entry.Type), true, entry.Nameplate, entry.ExampleCaption);
            _paletteSlots.Add(block, new Pose(block.transform.localPosition, block.transform.localRotation));
            OnBlockCreated?.Invoke(block);
        }

        [Serializable]
        private sealed class PaletteBlock
        {
            [SerializeField] private LayerType _type;
            [SerializeField] private ModelBlockController _block;
            [SerializeField] private ModelBlockController _prefab;
            [SerializeField] private Text _nameplate;
            [SerializeField] private Text _exampleCaption;

            public LayerType Type => _type;
            public ModelBlockController Block => _block;
            public ModelBlockController Prefab => _prefab;
            public Text Nameplate => _nameplate;
            public Text ExampleCaption => _exampleCaption;
        }
    }
}
