using System.Globalization;
using UnityEngine;

namespace CNNConvolution
{
    // The picture as numbers and one 3×3×3 filter applied to it without padding, one cell at a time.
    // Rows count from the top of the picture and columns from the left.
    internal sealed class PictureConvolution
    {
        public const int MapColumns = PictureSampler.Columns - 2;
        public const int MapRows = PictureSampler.Rows - 2;
        // Fitted nonnegative denoising weights, quantized to 0.01 and summing to one.
        // Only R contributes; the existing zero-bias calculation and 0..255 range are preserved.
        // Reproduction: mnist-color-data/learned-filters/fit_and_export.py
        private static readonly float[] RedWeights = new[] { 0.00f, 0.12f, 0.04f, 0.14f, 0.42f, 0.13f, 0.04f, 0.11f, 0.00f };

        private readonly Color32[] _pixels;

        public PictureConvolution(Color32[] pixels)
        {
            _pixels = pixels;
        }

        public string WeightLabel(int channel, int index) =>
            (channel == 0 ? RedWeights[index] : 0f).ToString("0.##", CultureInfo.InvariantCulture);

        // Value of the pixel under filter cell `index` (0..8, row by row) when the filter's top-left cell
        // is at (row, column).
        public int Input(int channel, int row, int column, int index)
        {
            Color32 pixel = _pixels[(PictureSampler.Rows - 1 - (row + index / 3)) * PictureSampler.Columns + column + index % 3];
            return channel == 0 ? pixel.r : channel == 1 ? pixel.g : pixel.b;
        }

        public float Output(int row, int column)
        {
            float sum = 0f;
            for (int index = 0; index < RedWeights.Length; index++)
            {
                sum += Input(0, row, column, index) * RedWeights[index];
            }
            return sum;
        }

        // The lesson starts where the filter responds most, so the first worked value is a clear one.
        public Vector2Int StrongestWindow()
        {
            Vector2Int strongest = Vector2Int.zero;
            float peak = float.MinValue;
            for (int row = 0; row < MapRows; row++)
            {
                for (int column = 0; column < MapColumns; column++)
                {
                    float output = Output(row, column);
                    if (output > peak)
                    {
                        peak = output;
                        strongest = new Vector2Int(column, row);
                    }
                }
            }
            return strongest;
        }
    }
}
