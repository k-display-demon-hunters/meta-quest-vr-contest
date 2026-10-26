using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace ModelDesignScene.Execution.Engine.Tests
{
    public sealed class CnnMathTests
    {
        [Test]
        public void RectangularMultichannelConvolutionTanhMatchesNumericalGradients()
        {
            ConvolutionLayer layer = new ConvolutionLayer(4, 5, 2,
                new CnnLayerDefinition(LayerKind.Conv2D, 2, 2, 3, CnnActivation.Tanh), new Random(23));
            float[] input = new float[40];
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (i % 11 - 5) * 0.07f;
            }
            AssertLayerGradients(layer, input);
        }

        [Test]
        public void DenseSoftmaxMatchesNumericalGradientsIncludingCrossClassTerms()
        {
            DenseLayer layer = new DenseLayer(4,
                new CnnLayerDefinition(LayerKind.Dense, 3, activation: CnnActivation.Softmax), new Random(17));
            AssertLayerGradients(layer, new[] { -0.3f, 0.2f, 0.7f, -0.1f });
        }

        [Test]
        public void MaxPoolRoutesGradientsToSelectedChannelValuesAndDropsValidPaddingRemainder()
        {
            MaxPoolingLayer layer = new MaxPoolingLayer(3, 3, 2,
                new CnnLayerDefinition(LayerKind.MaxPooling2D, height: 2, width: 2));
            // HWC: each pair is a pixel. Last row/column are outside the valid 2x2 window.
            float[] input = { 1, 8, 5, 2, 90, 90, 7, 3, 4, 6, 90, 90, 90, 90, 90, 90, 90, 90 };
            layer.Forward(input, CancellationToken.None);
            CollectionAssert.AreEqual(new[] { 7f, 8f }, layer.Output);

            float[] gradient = layer.Backward(new[] { 0.25f, -0.75f }, CancellationToken.None);
            float[] expected = new float[input.Length];
            expected[6] = 0.25f;
            expected[1] = -0.75f;
            CollectionAssert.AreEqual(expected, gradient);

            // A later pass must not retain the previous sample's maxima or input gradient.
            input[0] = 9;
            input[7] = 10;
            layer.Forward(input, CancellationToken.None);
            gradient = layer.Backward(new[] { 2f, 3f }, CancellationToken.None);
            Array.Clear(expected, 0, expected.Length);
            expected[0] = 2;
            expected[7] = 3;
            CollectionAssert.AreEqual(expected, gradient);
        }

        [Test]
        public void SoftmaxCrossEntropyRemainsFiniteForExtremeLogitsAndHasCorrectFusedGradient()
        {
            float[] extreme = { 1000, -1000, 0 };
            float[] probabilities = new float[3];
            ActivationFunctions.Forward(extreme, probabilities, CnnActivation.Softmax, 3);
            Assert.That(ClassificationLoss.Calculate(extreme, 1), Is.EqualTo(2000).Within(1e-8));
            CollectionAssert.AreEqual(new[] { 1f, 0f, 0f }, probabilities);

            float[] logits = { 1.2f, -0.4f, 0.3f };
            float[] shifted = { 1001.2f, 999.6f, 1000.3f };
            Assert.That(ClassificationLoss.Calculate(shifted, 1),
                Is.EqualTo(ClassificationLoss.Calculate(logits, 1)).Within(0.0002));
            ActivationFunctions.Forward(logits, probabilities, CnnActivation.Softmax, 3);
            float[] gradient = new float[3];
            ClassificationLoss.Gradient(probabilities, 1, gradient);
            AssertNumericalGradient(logits, gradient, () => ClassificationLoss.Calculate(logits, 1), "logit");
        }

        private static void AssertLayerGradients(ICnnLayer layer, float[] input)
        {
            float[] upstream = new float[layer.Output.Length];
            for (int i = 0; i < upstream.Length; i++)
            {
                upstream[i] = (i % 5 - 2) * 0.13f + 0.04f;
            }
            layer.Forward(input, CancellationToken.None);
            float[] inputGradient = (float[])layer.Backward(upstream, CancellationToken.None).Clone();
            TrainableParameters weights = ReadParameters(layer, "_weights");
            TrainableParameters biases = ReadParameters(layer, "_biases");
            float[] weightGradient = (float[])weights.Gradients.Clone();
            float[] biasGradient = (float[])biases.Gradients.Clone();
            Func<double> objective = () =>
            {
                layer.Forward(input, CancellationToken.None);
                double value = 0;
                for (int i = 0; i < upstream.Length; i++)
                {
                    value += (double)layer.Output[i] * upstream[i];
                }
                return value;
            };
            AssertNumericalGradient(input, inputGradient, objective, "input");
            AssertNumericalGradient(weights.Values, weightGradient, objective, "weight");
            AssertNumericalGradient(biases.Values, biasGradient, objective, "bias");
        }

        private static TrainableParameters ReadParameters(ICnnLayer layer, string fieldName)
        {
            FieldInfo field = layer.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (TrainableParameters)field.GetValue(layer);
        }

        private static void AssertNumericalGradient(float[] values, float[] analytic, Func<double> objective, string name)
        {
            const float step = 0.001f;
            for (int i = 0; i < values.Length; i++)
            {
                float original = values[i];
                double positive;
                double negative;
                try
                {
                    values[i] = original + step;
                    positive = objective();
                    values[i] = original - step;
                    negative = objective();
                }
                finally
                {
                    values[i] = original;
                }
                double numerical = (positive - negative) / (2 * step);
                double tolerance = 0.0002 + Math.Abs(numerical) * 0.003;
                Assert.That(analytic[i], Is.EqualTo(numerical).Within(tolerance), name + "[" + i + "]");
            }
        }
    }
}
