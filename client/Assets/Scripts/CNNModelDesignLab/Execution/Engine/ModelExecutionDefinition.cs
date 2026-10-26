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
        public const int MaxFilters = 16;
        public const int MaxDenseUnits = 128;
        public int Height { get; }
        public int Width { get; }
        public int Channels { get; }
        public int InputSize { get; }
        public IReadOnlyList<CnnLayerDefinition> Layers { get; }

        public ModelExecutionDefinition(int height, int width, int channels, IEnumerable<CnnLayerDefinition> layers)
        {
            if (height < 1 || width < 1 || channels < 1 || height > 200000 || width > 200000 || channels > 256 ||
                (long)height * width * channels > 200000)
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
                if (layer == null || snapshot.Count == 32)
                {
                    throw new ArgumentException("Invalid layer list.");
                }
                snapshot.Add(layer);
            }
            Height = height;
            Width = width;
            Channels = channels;
            InputSize = height * width * channels;
            Layers = snapshot.AsReadOnly();
        }

        public void Validate(CnnTrainingOptions options)
        {
            CalculateMultiplyAdds(options);
        }

        internal long CalculateMultiplyAdds(CnnTrainingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (Layers.Count < 3 || Layers.Count > options.MaxLayers || Layers[0].Kind != LayerKind.Conv2D)
            {
                throw new ArgumentException("A CNN must start with Conv2D and include Flatten and a classifier.");
            }
            int height = Height;
            int width = Width;
            int channels = Channels;
            long elements = InputSize;
            long parameters = 0;
            long multiplyAdds = 0;
            long bufferedElements = elements;
            bool flattened = false;
            for (int i = 0; i < Layers.Count; i++)
            {
                CnnLayerDefinition layer = Layers[i];
                if (!Enum.IsDefined(typeof(CnnActivation), layer.Activation))
                {
                    throw new ArgumentException("Unsupported activation.");
                }
                switch (layer.Kind)
                {
                    case LayerKind.Conv2D:
                        if (flattened || layer.Units < 1 || layer.Units > MaxFilters || layer.Height < 1 || layer.Width < 1 ||
                            layer.Height > height || layer.Width > width)
                        {
                            throw new ArgumentException("Invalid Conv2D shape or placement.");
                        }
                        long kernelInputs = (long)layer.Height * layer.Width * channels;
                        height -= layer.Height - 1;
                        width -= layer.Width - 1;
                        channels = layer.Units;
                        elements = (long)height * width * channels;
                        parameters += (kernelInputs + 1) * channels;
                        multiplyAdds += elements * kernelInputs;
                        break;
                    case LayerKind.MaxPooling2D:
                        if (flattened || layer.Height < 1 || layer.Width < 1 || layer.Height > height || layer.Width > width)
                        {
                            throw new ArgumentException("Invalid MaxPooling2D shape or placement.");
                        }
                        height /= layer.Height;
                        width /= layer.Width;
                        elements = (long)height * width * channels;
                        break;
                    case LayerKind.Flatten:
                        if (flattened)
                        {
                            throw new ArgumentException("Flatten may appear only once.");
                        }
                        flattened = true;
                        break;
                    case LayerKind.Dense:
                        if (!flattened || layer.Units < 1 || layer.Units > MaxDenseUnits)
                        {
                            throw new ArgumentException("Dense requires an explicit Flatten and positive units.");
                        }
                        parameters += (elements + 1) * layer.Units;
                        multiplyAdds += elements * layer.Units;
                        elements = layer.Units;
                        break;
                    default:
                        throw new ArgumentException("Unsupported layer type.");
                }
                bufferedElements += elements;
                if (elements > options.MaxActivationElements || parameters > options.MaxParameters ||
                    multiplyAdds > options.MaxMultiplyAdds || bufferedElements > (long)options.MaxActivationElements * options.MaxLayers)
                {
                    throw new ArgumentException("The model exceeds the local training resource limits.");
                }
            }
            CnnLayerDefinition final = Layers[Layers.Count - 1];
            if (InputSize > options.MaxActivationElements || final.Kind != LayerKind.Dense || final.Units != 10 ||
                final.Activation != CnnActivation.Softmax)
            {
                throw new ArgumentException("The output must be Dense(10, Softmax) within the input size limit.");
            }
            return multiplyAdds;
        }
    }
}
