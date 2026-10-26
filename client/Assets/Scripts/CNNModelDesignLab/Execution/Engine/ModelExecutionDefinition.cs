using System;
using System.Collections.Generic;

namespace ModelDesignScene.Execution.Engine
{
    public enum LayerKind
    {
        Conv2D,
        MaxPooling2D,
        Flatten,
        Dense
    }

    public enum CnnActivation
    {
        Linear,
        ReLU,
        Sigmoid,
        Tanh,
        Softmax
    }

    public sealed class CnnLayerDefinition
    {
        public LayerKind Kind { get; }
        public int Units { get; }
        public int Height { get; }
        public int Width { get; }
        public CnnActivation Activation { get; }

        public CnnLayerDefinition(LayerKind kind, int units = 0, int height = 1, int width = 1,
            CnnActivation activation = CnnActivation.Linear)
        {
            Kind = kind;
            Units = units;
            Height = height;
            Width = width;
            Activation = activation;
        }
    }

    /// <summary>Immutable execution snapshot. Tensors use height, width, channel order.</summary>
    public sealed class ModelExecutionDefinition
    {
        public int Height { get; }
        public int Width { get; }
        public int Channels { get; }
        public int InputSize { get; }
        public IReadOnlyList<CnnLayerDefinition> Layers { get; }

        public ModelExecutionDefinition(int height, int width, int channels, IEnumerable<CnnLayerDefinition> layers)
        {
            if (height < 1 || width < 1 || channels < 1)
            {
                throw new ArgumentException("Invalid input tensor size.");
            }
            if (layers == null)
            {
                throw new ArgumentNullException(nameof(layers));
            }
            List<CnnLayerDefinition> snapshot = new List<CnnLayerDefinition>();
            foreach (CnnLayerDefinition layer in layers)
            {
                if (layer == null)
                {
                    throw new ArgumentException("Invalid layer list.");
                }
                snapshot.Add(layer);
            }
            Height = height;
            Width = width;
            Channels = channels;
            InputSize = checked(height * width * channels);
            Layers = snapshot.AsReadOnly();
        }
    }
}
