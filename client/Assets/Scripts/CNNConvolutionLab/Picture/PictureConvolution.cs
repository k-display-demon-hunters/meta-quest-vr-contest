using UnityEngine;

namespace CNNConvolution
{
    // The picture as numbers and one 3×3×3 filter applied to it without padding, one cell at a time.
    // Rows count from the top of the picture and columns from the left.
    internal sealed class PictureConvolution
    {
        public const int MapColumns = PictureSampler.Columns - 2;
        public const int MapRows = PictureSampler.Rows - 2;
        // Demonstration filter: every cell of the red layer weighs one ninth and the other layers weigh
        // nothing, so an output is the mean red value under the filter and can never be negative.
        private static readonly float[] LayerWeights = { 1f / 9f, 0f, 0f };
        private static readonly string[] LayerWeightLabels = { "1/9", "0", "0" };

        private readonly Color32[] _pixels;

        public PictureConvolution(Color32[] pixels)
        {
            _pixels = pixels;
        }

        public string WeightLabel(int channel) => LayerWeightLabels[channel];

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
            for (int channel = 0; channel < LayerWeights.Length; channel++)
            {
                for (int index = 0; index < 9; index++)
                {
                    sum += Input(channel, row, column, index) * LayerWeights[channel];
                }
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
