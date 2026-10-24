using System;
using System.Threading;

namespace CNNTraining
{
    internal sealed class ConvolutionLayer : ICnnLayer
    {
        private readonly int _inputWidth;
        private readonly int _inputChannels;
        private readonly int _outputHeight;
        private readonly int _outputWidth;
        private readonly int _filters;
        private readonly int _kernelHeight;
        private readonly int _kernelWidth;
        private readonly int _kernelInputs;
        private readonly CnnActivation _activation;
        private readonly TrainableParameters _weights;
        private readonly TrainableParameters _biases;
        private readonly float[] _logits;
        private readonly float[] _delta;
        private readonly float[] _inputGradient;
        private float[] _input;
        public float[] Output { get; }

        internal ConvolutionLayer(int height, int width, int channels, CnnLayerDefinition definition, Random random)
        {
            _inputWidth = width;
            _inputChannels = channels;
            _kernelHeight = definition.Height;
            _kernelWidth = definition.Width;
            _filters = definition.Units;
            _activation = definition.Activation;
            _outputHeight = height - _kernelHeight + 1;
            _outputWidth = width - _kernelWidth + 1;
            _kernelInputs = _kernelHeight * _kernelWidth * channels;
            _weights = new TrainableParameters(_filters * _kernelInputs);
            _weights.Initialize(random, _kernelInputs, _kernelHeight * _kernelWidth * _filters, _activation);
            _biases = new TrainableParameters(_filters);
            Output = new float[_outputHeight * _outputWidth * _filters];
            _logits = new float[Output.Length];
            _delta = new float[Output.Length];
            _inputGradient = new float[height * width * channels];
        }

        public void Forward(float[] input, CancellationToken cancellation)
        {
            _input = input;
            for (int y = 0; y < _outputHeight; y++)
            {
                cancellation.ThrowIfCancellationRequested();
                for (int x = 0; x < _outputWidth; x++)
                {
                    int outputBase = (y * _outputWidth + x) * _filters;
                    for (int filter = 0; filter < _filters; filter++)
                    {
                        float sum = _biases.Values[filter];
                        int weightIndex = filter * _kernelInputs;
                        for (int ky = 0; ky < _kernelHeight; ky++)
                        {
                            int inputIndex = ((y + ky) * _inputWidth + x) * _inputChannels;
                            int rowLength = _kernelWidth * _inputChannels;
                            for (int k = 0; k < rowLength; k++)
                            {
                                sum += input[inputIndex + k] * _weights.Values[weightIndex++];
                            }
                        }
                        _logits[outputBase + filter] = sum;
                    }
                }
            }
            ActivationFunctions.EnsureFinite(_logits);
            ActivationFunctions.Forward(_logits, Output, _activation, _filters);
        }

        public float[] Backward(float[] gradient, CancellationToken cancellation)
        {
            ActivationFunctions.Backward(Output, gradient, _delta, _activation, _filters);
            Array.Clear(_inputGradient, 0, _inputGradient.Length);
            for (int y = 0; y < _outputHeight; y++)
            {
                cancellation.ThrowIfCancellationRequested();
                for (int x = 0; x < _outputWidth; x++)
                {
                    int outputBase = (y * _outputWidth + x) * _filters;
                    for (int filter = 0; filter < _filters; filter++)
                    {
                        float delta = _delta[outputBase + filter];
                        _biases.Gradients[filter] += delta;
                        int weightIndex = filter * _kernelInputs;
                        for (int ky = 0; ky < _kernelHeight; ky++)
                        {
                            int inputIndex = ((y + ky) * _inputWidth + x) * _inputChannels;
                            int rowLength = _kernelWidth * _inputChannels;
                            for (int k = 0; k < rowLength; k++, weightIndex++)
                            {
                                _weights.Gradients[weightIndex] += _input[inputIndex + k] * delta;
                                _inputGradient[inputIndex + k] += _weights.Values[weightIndex] * delta;
                            }
                        }
                    }
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
