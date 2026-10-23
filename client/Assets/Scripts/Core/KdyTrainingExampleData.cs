using UnityEngine;

namespace KdyLearning
{
    // Authored educational outcomes for all 24 designs. This class does not train or run a network.
    public static class KdyTrainingExampleData
    {
        private const int CurveSamples = 121;
        // Layer count, channel count (4/32), kernel (3/9), activation (ReLU/Sigmoid).
        // Greater capacity helps this particular example in many cases, but is not a universal rule.
        private static readonly float[,,,] Improvements =
        {
            { { { 23f, 26f }, { 27f, 30f } }, { { 39f, 35f }, { 45f, 41f } } },
            { { { 34f, 31f }, { 40f, 35f } }, { { 55f, 47f }, { 60f, 51f } } },
            { { { 39f, 27f }, { 45f, 32f } }, { { 68f, 55f }, { 72f, 58f } } }
        };
        private static readonly float[] RepeatOffsets = { -1.2f, .7f, 1.6f, -.5f, .1f };
        private static readonly Color[] ClearImage = KdySignExampleData.CreateImage();
        private static readonly Color[] BlurredImage = Blur(ClearImage);
        public static float BaselineError { get; } = MeanSquaredError(BlurredImage, ClearImage);

        public static Color[] CreateClearImage()
        {
            return (Color[])ClearImage.Clone();
        }

        public static Color[] CreateBlurredImage()
        {
            return (Color[])BlurredImage.Clone();
        }

        public static float GetImprovement(KdyNetworkConfiguration configuration, int attempt)
        {
            if (configuration == null || !configuration.IsComplete)
            {
                return 0f;
            }
            int layers = configuration.LayerCount - 1;
            int channels = configuration.FeatureMaps == 32 ? 1 : 0;
            int kernel = configuration.KernelSize == 9 ? 1 : 0;
            int activation = configuration.UseSigmoid ? 1 : 0;
            float variation = RepeatOffsets[Mathf.Max(0, attempt - 1) % RepeatOffsets.Length];
            return Improvements[layers, channels, kernel, activation] + variation;
        }

        public static float[] CreateLossCurve(KdyNetworkConfiguration configuration, int attempt)
        {
            float score = GetImprovement(configuration, attempt);
            float endLoss = BaselineError * (1f - score / 100f);
            float[] samples = new float[CurveSamples];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)(samples.Length - 1);
                float eased = (1f - Mathf.Exp(-4f * t)) / (1f - Mathf.Exp(-4f));
                samples[i] = Mathf.Lerp(BaselineError, endLoss, eased);
            }
            // The graph endpoint and the displayed RGB reconstruction use the same MSE definition.
            samples[0] = BaselineError;
            samples[samples.Length - 1] = endLoss;
            return samples;
        }

        public static Color[] BlendForImprovement(float score)
        {
            float amount = 1f - Mathf.Sqrt(1f - Mathf.Clamp(score, 0f, 99.9f) / 100f);
            Color[] pixels = new Color[ClearImage.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.Lerp(BlurredImage[i], ClearImage[i], amount);
                pixels[i].a = 1f;
            }
            return pixels;
        }

        public static float MeanSquaredError(Color[] prediction, Color[] target)
        {
            float squaredError = 0f;
            for (int i = 0; i < prediction.Length; i++)
            {
                Color delta = prediction[i] - target[i];
                squaredError += delta.r * delta.r + delta.g * delta.g + delta.b * delta.b;
            }
            return squaredError / (prediction.Length * 3f);
        }

        private static Color[] Blur(Color[] source)
        {
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            Color[] pixels = new Color[source.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color sum = Color.clear;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int px = Mathf.Clamp(x + dx, 0, width - 1);
                            int py = Mathf.Clamp(y + dy, 0, height - 1);
                            sum += source[py * width + px];
                        }
                    }
                    pixels[y * width + x] = sum / 9f;
                }
            }
            return pixels;
        }
    }
}
