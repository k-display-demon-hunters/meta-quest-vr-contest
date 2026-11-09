using UnityEngine;

namespace FeatureMapCorridor
{
    internal static class PercentageFormatter
    {
        internal static void RoundPercentages(System.Collections.Generic.IReadOnlyList<float> probabilities, int[] percentages, float fraction = 1)
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
                    if ((awarded & (1 << i)) != 0)
                    {
                        continue;
                    }
                    float remainder = probabilities[i] * 100 * fraction - percentages[i];
                    if (remainder > bestRemainder)
                    {
                        best = i;
                        bestRemainder = remainder;
                    }
                }
                percentages[best]++;
                awarded |= 1 << best;
            }
        }
    }
}
