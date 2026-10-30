using UnityEngine;

namespace CNNFeatureMaps
{
    // Demonstration kernels, not learned network weights. Valid convolution, stride 1.
    public static class RgbFilterCalculation
    {
        public const int InputSize = 6;
        public const int KernelSize = 3;
        public const int OutputSize = 4;
        public static int KernelWidth(bool redFilter) => redFilter ? 3 : 2;
        public static int OutputWidth(bool redFilter) => InputSize - KernelWidth(redFilter) + 1;

        public static float Weight(bool redFilter, int channel, int column)
        {
            if (redFilter)
            {
                return channel == 0 ? 1f / 9f : -0.5f / 9f;
            }
            float luminance = channel == 0 ? 0.2126f : channel == 1 ? 0.7152f : 0.0722f;
            return (column == 0 ? -1f : 1f) * luminance / 3f;
        }

        public static float ChannelSum(Texture2D input, bool redFilter, int channel, int row, int column)
        {
            float sum = 0f;
            for (int y = 0; y < KernelSize; y++)
            {
                for (int x = 0; x < KernelWidth(redFilter); x++)
                {
                    Color pixel = input.GetPixel(column + x, InputSize - 1 - row - y);
                    sum += pixel[channel] * Weight(redFilter, channel, x);
                }
            }
            return sum;
        }
    }
}
