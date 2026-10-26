using System;

namespace ModelDesignScene.Editing
{
    public enum LayerType { Conv2D, MaxPooling2D, Flatten, Dense, Input, Output }
    public enum ActivationType { Linear, ReLU, Sigmoid, Tanh, Softmax }

    public sealed class LayerDefinition
    {
        internal event Action OnChanged;
        internal ModelWorkspace Workspace { get; set; }

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

        internal static LayerDefinition Conv2D(int filters, int kernelHeight, int kernelWidth,
            ActivationType activation)
        {
            RequirePositive(filters, nameof(filters));
            RequirePositive(kernelHeight, nameof(kernelHeight));
            RequirePositive(kernelWidth, nameof(kernelWidth));
            RequireActivation(activation);
            return new LayerDefinition(LayerType.Conv2D, filters, kernelHeight, kernelWidth, activation: activation);
        }

        internal static LayerDefinition MaxPooling2D(int poolHeight, int poolWidth)
        {
            RequirePositive(poolHeight, nameof(poolHeight));
            RequirePositive(poolWidth, nameof(poolWidth));
            return new LayerDefinition(LayerType.MaxPooling2D, height: poolHeight, width: poolWidth);
        }

        internal static LayerDefinition Flatten()
        {
            return new LayerDefinition(LayerType.Flatten);
        }

        internal static LayerDefinition Dense(int units, ActivationType activation)
        {
            RequirePositive(units, nameof(units));
            RequireActivation(activation);
            return new LayerDefinition(LayerType.Dense, units: units, activation: activation);
        }

        internal static LayerDefinition Input()
        {
            return new LayerDefinition(LayerType.Input);
        }

        internal static LayerDefinition Output()
        {
            return new LayerDefinition(LayerType.Output, units: 10, activation: ActivationType.Softmax);
        }

        internal void SetFilters(int filters)
        {
            RequireType(LayerType.Conv2D);
            RequirePositive(filters, nameof(filters));
            if (Filters == filters)
            {
                return;
            }
            Filters = filters;
            OnChanged?.Invoke();
        }

        internal void SetKernelSize(int height, int width)
        {
            RequireType(LayerType.Conv2D);
            RequirePositive(height, nameof(height));
            RequirePositive(width, nameof(width));
            if (Height == height && Width == width)
            {
                return;
            }
            Height = height;
            Width = width;
            OnChanged?.Invoke();
        }

        internal void SetPoolSize(int height, int width)
        {
            RequireType(LayerType.MaxPooling2D);
            RequirePositive(height, nameof(height));
            RequirePositive(width, nameof(width));
            if (Height == height && Width == width)
            {
                return;
            }
            Height = height;
            Width = width;
            OnChanged?.Invoke();
        }

        internal void SetUnits(int units)
        {
            RequireType(LayerType.Dense);
            RequirePositive(units, nameof(units));
            if (Units == units)
            {
                return;
            }
            Units = units;
            OnChanged?.Invoke();
        }

        internal void SetActivation(ActivationType activation)
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
