using System;

namespace ModelDesignScene.Model
{
    public enum LayerType { Conv2D, MaxPooling2D, Flatten, Dense, Input, Output }
    public enum ActivationType { Linear, ReLU, Sigmoid, Tanh, Softmax }

    public sealed class LayerDefinition
    {
        internal event Action OnChanged;

        public LayerType Type { get; }
        public int Filters { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public int Units { get; private set; }
        public ActivationType Activation { get; private set; }

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

        public static LayerDefinition CreateDefault(LayerType type)
        {
            switch (type)
            {
                case LayerType.Conv2D: return Conv2D(4, 3, 3, ActivationType.ReLU);
                case LayerType.MaxPooling2D: return MaxPooling2D(2, 2);
                case LayerType.Flatten: return Flatten();
                case LayerType.Dense: return Dense(32, ActivationType.ReLU);
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static LayerDefinition Conv2D(int filters, int kernelHeight, int kernelWidth,
            ActivationType activation)
        {
            RequireRange(filters, LayerLimits.Conv2DMaxFilters, nameof(filters));
            RequireRange(kernelHeight, LayerLimits.Conv2DMaxKernelSize, nameof(kernelHeight));
            RequireRange(kernelWidth, LayerLimits.Conv2DMaxKernelSize, nameof(kernelWidth));
            RequireActivation(activation);
            return new LayerDefinition(LayerType.Conv2D, filters, kernelHeight, kernelWidth, activation: activation);
        }

        public static LayerDefinition MaxPooling2D(int poolHeight, int poolWidth)
        {
            RequireRange(poolHeight, LayerLimits.MaxPoolMaxPoolSize, nameof(poolHeight));
            RequireRange(poolWidth, LayerLimits.MaxPoolMaxPoolSize, nameof(poolWidth));
            return new LayerDefinition(LayerType.MaxPooling2D, height: poolHeight, width: poolWidth);
        }

        public static LayerDefinition Flatten()
        {
            return new LayerDefinition(LayerType.Flatten);
        }

        public static LayerDefinition Dense(int units, ActivationType activation)
        {
            RequireRange(units, LayerLimits.DenseMaxUnits, nameof(units));
            RequireActivation(activation);
            return new LayerDefinition(LayerType.Dense, units: units, activation: activation);
        }

        internal static LayerDefinition Input()
        {
            return new LayerDefinition(LayerType.Input);
        }

        internal static LayerDefinition Output()
        {
            return new LayerDefinition(LayerType.Output, units: LayerLimits.OutputClassCount, activation: ActivationType.Softmax);
        }

        public void SetFilters(int filters)
        {
            RequireType(LayerType.Conv2D);
            RequireRange(filters, LayerLimits.Conv2DMaxFilters, nameof(filters));
            if (Filters == filters)
            {
                return;
            }
            Filters = filters;
            OnChanged?.Invoke();
        }

        public void SetKernelSize(int height, int width)
        {
            RequireType(LayerType.Conv2D);
            RequireRange(height, LayerLimits.Conv2DMaxKernelSize, nameof(height));
            RequireRange(width, LayerLimits.Conv2DMaxKernelSize, nameof(width));
            if (Height == height && Width == width)
            {
                return;
            }
            Height = height;
            Width = width;
            OnChanged?.Invoke();
        }

        public void SetPoolSize(int height, int width)
        {
            RequireType(LayerType.MaxPooling2D);
            RequireRange(height, LayerLimits.MaxPoolMaxPoolSize, nameof(height));
            RequireRange(width, LayerLimits.MaxPoolMaxPoolSize, nameof(width));
            if (Height == height && Width == width)
            {
                return;
            }
            Height = height;
            Width = width;
            OnChanged?.Invoke();
        }

        public void SetUnits(int units)
        {
            RequireType(LayerType.Dense);
            RequireRange(units, LayerLimits.DenseMaxUnits, nameof(units));
            if (Units == units)
            {
                return;
            }
            Units = units;
            OnChanged?.Invoke();
        }

        public void SetActivation(ActivationType activation)
        {
            if (Type != LayerType.Conv2D && Type != LayerType.Dense)
            {
                throw new InvalidOperationException("Only Conv2D and Dense have an activation setting.");
            }
            RequireActivation(activation);
            if (Activation == activation)
            {
                return;
            }
            Activation = activation;
            OnChanged?.Invoke();
        }

        private void RequireType(LayerType expectedType)
        {
            if (Type != expectedType)
            {
                throw new InvalidOperationException("This setting requires a " + expectedType + " layer.");
            }
        }

        private static void RequireRange(int value, int maximum, string parameterName)
        {
            if (value < LayerLimits.MinParameterValue || value > maximum)
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
