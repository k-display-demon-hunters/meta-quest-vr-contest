using UnityEngine;

namespace FeatureMapCorridor
{
    internal static class ConvolutionMath
    {
        internal static float Contribution(float[] source, float[, ] kernel, int size, int index)
        {
            int y = index / size;
            int x = index % size;
            float sum = 0;
            for (int row = 0; row < 3; row++)
            {
                int srcY = y + row - 1;
                if ((uint)srcY >= (uint)size)
                {
                    continue;
                }
                for (int column = 0; column < 3; column++)
                {
                    int srcX = x + column - 1;
                    if ((uint)srcX < (uint)size)
                    {
                        sum += source[srcY * size + srcX] * kernel[row, column];
                    }
                }
            }
            return sum;
        }
    }
}
