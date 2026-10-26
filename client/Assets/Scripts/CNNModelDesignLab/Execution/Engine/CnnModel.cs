using System;
using System.Collections.Generic;
using System.Threading;

namespace ModelDesignScene.Execution.Engine
{
    internal interface ICnnLayer
    {
        float[] Output { get; }
        void Forward(float[] input, CancellationToken cancellation);
        float[] Backward(float[] gradient, CancellationToken cancellation);
        void Update(AdamOptimizer optimizer);
    }

    internal sealed class CnnModel
    {
        private readonly List<ICnnLayer> _layers = new List<ICnnLayer>();
        private readonly DenseLayer _classifier;
        private readonly float[] _lossGradient = new float[10];
        internal int InputSize { get; }
        internal float[] Output => _classifier.Output;
        internal float[] Logits => _classifier.Logits;

        internal CnnModel(ModelExecutionDefinition definition, CnnTrainingOptions options)
        {
            InputSize = definition.InputSize;
            int height = definition.Height;
            int width = definition.Width;
            int channels = definition.Channels;
            int elements = InputSize;
            Random random = new Random(options.Seed);
            foreach (CnnLayerDefinition layer in definition.Layers)
            {
                switch (layer.Kind)
                {
                    case LayerKind.Conv2D:
                        _layers.Add(new ConvolutionLayer(height, width, channels, layer, random));
                        height -= layer.Height - 1;
                        width -= layer.Width - 1;
                        channels = layer.Units;
                        elements = height * width * channels;
                        break;
                    case LayerKind.MaxPooling2D:
                        _layers.Add(new MaxPoolingLayer(height, width, channels, layer));
                        height /= layer.Height;
                        width /= layer.Width;
                        elements = height * width * channels;
                        break;
                    case LayerKind.Flatten:
                        // HWC is already contiguous: reshape and its gradient are identity views.
                        break;
                    case LayerKind.Dense:
                        _layers.Add(new DenseLayer(elements, layer, random));
                        elements = layer.Units;
                        break;
                }
            }
            _classifier = (DenseLayer)_layers[_layers.Count - 1];
        }

        internal void Forward(float[] input, CancellationToken cancellation)
        {
            float[] values = input;
            foreach (ICnnLayer layer in _layers)
            {
                cancellation.ThrowIfCancellationRequested();
                layer.Forward(values, cancellation);
                values = layer.Output;
            }
        }

        internal void Backward(int label, CancellationToken cancellation)
        {
            ClassificationLoss.Gradient(Output, label, _lossGradient);
            float[] gradient = _classifier.BackwardLogits(_lossGradient, cancellation);
            for (int i = _layers.Count - 2; i >= 0; i--)
            {
                cancellation.ThrowIfCancellationRequested();
                gradient = _layers[i].Backward(gradient, cancellation);
            }
        }

        internal void Update(AdamOptimizer optimizer)
        {
            foreach (ICnnLayer layer in _layers)
            {
                layer.Update(optimizer);
            }
        }
    }
}
