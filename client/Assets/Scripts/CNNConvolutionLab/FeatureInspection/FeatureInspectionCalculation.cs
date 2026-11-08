using UnityEngine;

namespace CNNConvolution
{
    public enum InspectionFilter
    {
        RedResponse,
        VerticalBoundary
    }

    // Fixed teaching specimen. Scores retain their sign; display intensity is separate.
    public static class FeatureInspectionCalculation
    {
        public const int InputSize = 6;
        public const int KernelSize = 3;
        public const int OutputSize = 4;

        public static Color SpecimenColor(int row, int column, bool rotated)
        {
            // Clockwise rotation: destination[row, column] = source[5-column, row].
            int sourceColumn = rotated ? row : column;
            return sourceColumn < InputSize / 2
                ? (Color)new Color32(223, 109, 81, 255)
                : (Color)new Color32(51, 68, 101, 255);
        }

        public static float[] CalculateMap(InspectionFilter filter, bool rotated)
        {
            float[] scores = new float[OutputSize * OutputSize];
            for (int row = 0; row < OutputSize; row++)
            {
                for (int column = 0; column < OutputSize; column++)
                {
                    float score = 0f;
                    for (int kernelRow = 0; kernelRow < KernelSize; kernelRow++)
                    {
                        for (int kernelColumn = 0; kernelColumn < KernelSize; kernelColumn++)
                        {
                            Color pixel = SpecimenColor(row + kernelRow, column + kernelColumn, rotated);
                            if (filter == InspectionFilter.RedResponse)
                            {
                                score += pixel.r / 9f - pixel.g / 18f - pixel.b / 18f;
                            }
                            else
                            {
                                float luminance = 0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b;
                                float columnWeight = kernelColumn == 0 ? -1f : kernelColumn == 1 ? 1f : 0f;
                                score += luminance * columnWeight / 3f;
                            }
                        }
                    }
                    scores[row * OutputSize + column] = score;
                }
            }
            return scores;
        }

        public static float DisplayIntensity(InspectionFilter filter, float score)
        {
            return filter == InspectionFilter.RedResponse
                ? Mathf.Clamp01(Mathf.Max(0f, score) * 2.9f)
                : Mathf.Clamp01(Mathf.Abs(score) * 4.5f);
        }
    }
}
