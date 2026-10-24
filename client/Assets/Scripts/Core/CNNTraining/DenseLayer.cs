using System;
using System.Threading;

namespace CNNTraining
{
    internal sealed class DenseLayer : ICnnLayer
    {
        private readonly int _inputSize;
        private readonly CnnActivation _activation;
        private readonly TrainableParameters _weights;
        private readonly TrainableParameters _biases;
        private readonly float[] _delta;
        private readonly float[] _inputGradient;
        private float[] _input;
        public float[] Output { get; }
        internal float[] Logits { get; }

        internal DenseLayer(int inputSize, CnnLayerDefinition definition, Random random)
        {
            _inputSize = inputSize;
            _activation = definition.Activation;
            _weights = new TrainableParameters(inputSize * definition.Units);
            _weights.Initialize(random, inputSize, definition.Units, _activation);
            _biases = new TrainableParameters(definition.Units);
            Output = new float[definition.Units];
            Logits = new float[definition.Units];
            _delta = new float[definition.Units];
            _inputGradient = new float[inputSize];
        }

        public void Forward(float[] input, CancellationToken cancellation)
        {
            _input = input;
            for (int unit = 0; unit < Output.Length; unit++)
            {
                cancellation.ThrowIfCancellationRequested();
                float sum = _biases.Values[unit];
                int weightBase = unit * _inputSize;
                for (int i = 0; i < _inputSize; i++)
                {
                    sum += input[i] * _weights.Values[weightBase + i];
                }
                Logits[unit] = sum;
            }
            ActivationFunctions.EnsureFinite(Logits);
            ActivationFunctions.Forward(Logits, Output, _activation, Output.Length);
        }

        public float[] Backward(float[] gradient, CancellationToken cancellation)
        {
            ActivationFunctions.Backward(Output, gradient, _delta, _activation, Output.Length);
            return BackwardLogits(_delta, cancellation);
        }

        // Final Softmax + CE supplies dL/dlogits directly; do not differentiate Softmax twice.
        internal float[] BackwardLogits(float[] gradient, CancellationToken cancellation)
        {
            Array.Clear(_inputGradient, 0, _inputGradient.Length);
            for (int unit = 0; unit < Output.Length; unit++)
            {
                cancellation.ThrowIfCancellationRequested();
                float delta = gradient[unit];
                _biases.Gradients[unit] += delta;
                int weightBase = unit * _inputSize;
                for (int i = 0; i < _inputSize; i++)
                {
                    int weightIndex = weightBase + i;
                    _weights.Gradients[weightIndex] += _input[i] * delta;
                    _inputGradient[i] += _weights.Values[weightIndex] * delta;
                }
            }
            return _inputGradient;
        }

        public void Update(AdamOptimizer optimizer)
        {
            optimizer.Update(_weights);
            optimizer.Update(_biases);
        }
    }
}
