using System;
using System.Collections.Generic;

namespace ModelDesignScene.Model
{
    public sealed class ModelDefinition : IDisposable
    {
        public const int SlotCount = 8;

        private readonly LayerDefinition[] _layers = new LayerDefinition[SlotCount];
        private IReadOnlyList<LayerDefinition> _readOnlyLayers;

        public LayerDefinition Input { get; } = LayerDefinition.Input();
        public LayerDefinition Output { get; } = LayerDefinition.Output();
        public IReadOnlyList<LayerDefinition> Layers => _readOnlyLayers ??= Array.AsReadOnly(_layers);
        public event Action OnChanged;

        public void PlaceLayer(int slot, LayerDefinition layer)
        {
            if (layer == null || layer.Type == LayerType.Input || layer.Type == LayerType.Output)
            {
                throw new ArgumentException("Only editable layers can occupy a slot.", nameof(layer));
            }
            if (_layers[slot] != null || Array.IndexOf(_layers, layer) >= 0)
            {
                throw new InvalidOperationException("The slot or layer is already placed.");
            }
            _layers[slot] = layer;
            layer.OnChanged += NotifyChanged;
            NotifyChanged();
        }

        public void RemoveLayer(int slot)
        {
            LayerDefinition layer = _layers[slot];
            if (layer == null)
            {
                return;
            }
            layer.OnChanged -= NotifyChanged;
            _layers[slot] = null;
            NotifyChanged();
        }

        public void Dispose()
        {
            foreach (LayerDefinition layer in _layers)
            {
                if (layer != null)
                {
                    layer.OnChanged -= NotifyChanged;
                }
            }
            OnChanged = null;
        }

        private void NotifyChanged()
        {
            OnChanged?.Invoke();
        }
    }
}
