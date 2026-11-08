using UnityEngine;

namespace FeatureMapCorridor
{
    internal static class CorridorMath
    {
        internal static float Contribution(float[] source, float[,] kernel, int size, int index)
        {
            int y = index / size;
            int x = index % size;
            float sum = 0;
            for (int row = 0; row < 3; row++)
            {
                int srcY = y + row - 1;
                if ((uint)srcY >= (uint)size) { continue; }
                for (int column = 0; column < 3; column++)
                {
                    int srcX = x + column - 1;
                    if ((uint)srcX < (uint)size) { sum += source[srcY * size + srcX] * kernel[row, column]; }
                }
            }
            return sum;
        }

        internal static string MapKey(int layer, int map) => layer + "-" + map;

        internal static void RoundPercentages(float[] probabilities, int[] percentages, float fraction = 1)
        {
            int remaining = Mathf.RoundToInt(100 * fraction);
            int awarded = 0;
            for (int i = 0; i < percentages.Length; i++)
            {
                percentages[i] = Mathf.FloorToInt(probabilities[i] * 100 * fraction);
                remaining -= percentages[i];
            }
            while (remaining-- > 0)
            {
                int best = -1;
                float bestRemainder = -1;
                for (int i = 0; i < percentages.Length; i++)
                {
                    if ((awarded & (1 << i)) != 0) { continue; }
                    float remainder = probabilities[i] * 100 * fraction - percentages[i];
                    if (remainder > bestRemainder) { best = i; bestRemainder = remainder; }
                }
                percentages[best]++;
                awarded |= 1 << best;
            }
        }
    }
}
