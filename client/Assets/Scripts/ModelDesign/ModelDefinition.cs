using System;
using System.Collections.Generic;

namespace ModelDesign
{
    public sealed class ModelDefinition
    {
        private readonly List<LayerDefinition> _layers = new List<LayerDefinition>();
        public IReadOnlyList<LayerDefinition> Layers { get; }

        public ModelDefinition()
        {
            Layers = _layers.AsReadOnly();
        }

        public void AddLayer(LayerDefinition layer)
        {
            InsertLayer(_layers.Count, layer);
        }

        public void InsertLayer(int index, LayerDefinition layer)
        {
            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }
            _layers.Insert(index, layer);
        }

        public void ReplaceLayer(int index, LayerDefinition layer)
        {
            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }
            _layers[index] = layer;
        }

        public void RemoveLayer(int index)
        {
            _layers.RemoveAt(index);
        }

        // toIndex is the final index, not an insertion slot before removal.
        public void MoveLayer(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            }
            if (toIndex < 0 || toIndex >= _layers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(toIndex));
            }
            LayerDefinition layer = _layers[fromIndex];
            _layers.RemoveAt(fromIndex);
            _layers.Insert(toIndex, layer);
        }
    }
}
