using System;
using System.Threading;

namespace ModelDesignScene.Execution.Engine
{
    internal sealed class MaxPoolingLayer : ICnnLayer
    {
        private readonly int _inputWidth;
        private readonly int _channels;
        private readonly int _height;
        private readonly int _width;
        private readonly int _poolHeight;
        private readonly int _poolWidth;
        private readonly int[] _selectedIndices;
        private readonly float[] _inputGradient;
        public float[] Output { get; }

        internal MaxPoolingLayer(int height, int width, int channels, CnnLayerDefinition definition)
        {
            _inputWidth = width;
            _channels = channels;
            _poolHeight = definition.Height;
            _poolWidth = definition.Width;
            _height = height / _poolHeight;
            _width = width / _poolWidth;
            Output = new float[_height * _width * channels];
            _selectedIndices = new int[Output.Length];
            _inputGradient = new float[height * width * channels];
        }

        public void Forward(float[] input, CancellationToken cancellation)
        {
            for (int y = 0; y < _height; y++)
            {
                cancellation.ThrowIfCancellationRequested();
                for (int x = 0; x < _width; x++)
                {
                    for (int channel = 0; channel < _channels; channel++)
                    {
                        int selected = ((y * _poolHeight) * _inputWidth + x * _poolWidth) * _channels + channel;
                        float maximum = input[selected];
                        for (int py = 0; py < _poolHeight; py++)
                        {
                            for (int px = 0; px < _poolWidth; px++)
                            {
                                int index = ((y * _poolHeight + py) * _inputWidth + x * _poolWidth + px) * _channels + channel;
                                if (input[index] > maximum)
                                {
                                    maximum = input[index];
                                    selected = index;
                                }
                            }
                        }
                        int outputIndex = (y * _width + x) * _channels + channel;
                        Output[outputIndex] = maximum;
                        _selectedIndices[outputIndex] = selected;
                    }
                }
            }
        }

        public float[] Backward(float[] gradient, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            Array.Clear(_inputGradient, 0, _inputGradient.Length);
            for (int i = 0; i < gradient.Length; i++)
            {
                _inputGradient[_selectedIndices[i]] += gradient[i];
            }
            return _inputGradient;
        }

        public void Update(AdamOptimizer optimizer)
        {
            // Pooling has no trainable parameters.
        }
    }
}
