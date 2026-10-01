using System;

namespace ModelDesign
{
    public enum LayerType { Conv2D, MaxPooling2D, Flatten, Dense }
    public enum ActivationType { Linear, ReLU, Sigmoid, Tanh, Softmax }

    // Immutable settings: editing replaces a layer without exposing mutable arrays.
    public sealed class LayerDefinition
    {
        public LayerType Type { get; }
        public int Filters { get; }
        public int Height { get; }
        public int Width { get; }
        public int Units { get; }
        public ActivationType Activation { get; }

        private LayerDefinition(LayerType type, int filters = 0, int height = 0,
            int width = 0, int units = 0, ActivationType activation = ActivationType.Linear)
        {
            Type = type;
            Filters = filters;
            Height = height;
            Width = width;
            Units = units;
            Activation = activation;
        }

        public static LayerDefinition Conv2D(int filters, int kernelHeight, int kernelWidth,
            ActivationType activation)
        {
            RequirePositive(filters, nameof(filters));
            RequirePositive(kernelHeight, nameof(kernelHeight));
            RequirePositive(kernelWidth, nameof(kernelWidth));
            RequireActivation(activation);
            return new LayerDefinition(LayerType.Conv2D, filters, kernelHeight, kernelWidth, activation: activation);
        }

        public static LayerDefinition MaxPooling2D(int poolHeight, int poolWidth)
        {
            RequirePositive(poolHeight, nameof(poolHeight));
            RequirePositive(poolWidth, nameof(poolWidth));
            return new LayerDefinition(LayerType.MaxPooling2D, height: poolHeight, width: poolWidth);
        }

        public static LayerDefinition Flatten()
        {
            return new LayerDefinition(LayerType.Flatten);
        }

        public static LayerDefinition Dense(int units, ActivationType activation)
        {
            RequirePositive(units, nameof(units));
            RequireActivation(activation);
            return new LayerDefinition(LayerType.Dense, units: units, activation: activation);
        }

        public static LayerDefinition Output()
        {
            return Dense(10, ActivationType.Softmax);
        }

        private static void RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be positive.");
            }
        }

        private static void RequireActivation(ActivationType activation)
        {
            if (!Enum.IsDefined(typeof(ActivationType), activation))
            {
                throw new ArgumentOutOfRangeException(nameof(activation));
            }
        }
    }
}
