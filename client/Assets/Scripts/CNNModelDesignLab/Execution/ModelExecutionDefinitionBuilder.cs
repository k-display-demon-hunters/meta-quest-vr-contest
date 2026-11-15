using System;
using System.Collections.Generic;
using ModelDesignScene.Model;
using ModelDesignScene.Execution.Engine;

namespace ModelDesignScene.Execution
{
    internal static class ModelExecutionDefinitionBuilder
    {
        internal static ModelExecutionDefinition Build(ModelDefinition model, out int?[] slotIndices)
        {
            return Build(model.Layers, model.Output, out slotIndices);
        }

        internal static ModelExecutionDefinition Build(IReadOnlyList<LayerDefinition> slotLayers,
            LayerDefinition output, out int?[] slotIndices)
        {
            List<CnnLayerDefinition> layers = new List<CnnLayerDefinition>();
            List<int?> slots = new List<int?>();
            for (int slot = 0; slot < slotLayers.Count; slot++)
            {
                LayerDefinition layer = slotLayers[slot];
                if (layer != null)
                {
                    layers.Add(CopyLayer(layer));
                    slots.Add(slot);
                }
            }
            layers.Add(CopyLayer(output));
            slots.Add(null);
            slotIndices = slots.ToArray();
            return new ModelExecutionDefinition(LayerLimits.InputHeight, LayerLimits.InputWidth,
                LayerLimits.InputChannels, layers);
        }

        internal static bool Matches(ModelDefinition model, ModelExecutionDefinition submitted)
        {
            if (submitted == null || submitted.Height != LayerLimits.InputHeight
                || submitted.Width != LayerLimits.InputWidth || submitted.Channels != LayerLimits.InputChannels)
            {
                return false;
            }
            int index = 0;
            foreach (LayerDefinition layer in model.Layers)
            {
                if (layer == null)
                {
                    continue;
                }
                if (index >= submitted.Layers.Count || !MatchesLayer(layer, submitted.Layers[index++]))
                {
                    return false;
                }
            }
            return index + 1 == submitted.Layers.Count && MatchesLayer(model.Output, submitted.Layers[index]);
        }

        private static bool MatchesLayer(LayerDefinition layer, CnnLayerDefinition snapshot)
        {
            CnnLayerDefinition current = CopyLayer(layer);
            return current.Kind == snapshot.Kind && current.Units == snapshot.Units
                && current.Height == snapshot.Height && current.Width == snapshot.Width
                && current.Activation == snapshot.Activation;
        }

        private static CnnLayerDefinition CopyLayer(LayerDefinition layer)
        {
            switch (layer.Type)
            {
                case LayerType.Conv2D:
                    return new CnnLayerDefinition(LayerKind.Conv2D, layer.Filters,
                        layer.Height, layer.Width, ConvertActivation(layer.Activation));
                case LayerType.MaxPooling2D:
                    return new CnnLayerDefinition(LayerKind.MaxPooling2D, height: layer.Height, width: layer.Width);
                case LayerType.Flatten:
                    return new CnnLayerDefinition(LayerKind.Flatten);
                case LayerType.Dense:
                case LayerType.Output:
                    return new CnnLayerDefinition(LayerKind.Dense, layer.Units,
                        activation: ConvertActivation(layer.Activation));
                default:
                    throw new InvalidOperationException("Unsupported model block.");
            }
        }

        private static CnnActivation ConvertActivation(ActivationType activation)
        {
            switch (activation)
            {
                case ActivationType.Linear: return CnnActivation.Linear;
                case ActivationType.ReLU: return CnnActivation.ReLU;
                case ActivationType.Sigmoid: return CnnActivation.Sigmoid;
                case ActivationType.Tanh: return CnnActivation.Tanh;
                case ActivationType.Softmax: return CnnActivation.Softmax;
                default: throw new ArgumentOutOfRangeException(nameof(activation));
            }
        }
    }
}
