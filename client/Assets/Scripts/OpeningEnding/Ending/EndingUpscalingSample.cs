namespace OpeningEnding.Ending
{
    /// <summary>Rounded display-space brightness sampled at 16x9 centers from OpeningBroadcast.png (news, t=3).</summary>
    internal static class EndingUpscalingSample
    {
        // Source: existing Textures/CNNPixelLab/OpeningBroadcast.png. No runtime readback or model inference.
        private static readonly float[] Brightness =
        {
            .48f, .99f, .99f, .48f, .48f, .48f, .48f, .48f, .48f, .48f, .48f, .48f, .48f, .40f, .40f, .40f,
            .46f, .46f, .46f, .46f, .46f, .46f, .46f, .46f, .46f, .34f, .34f, .34f, .34f, .34f, .34f, .46f,
            .44f, .44f, .44f, .44f, .25f, .25f, .44f, .44f, .44f, .34f, .34f, .49f, .76f, .44f, .34f, .44f,
            .41f, .41f, .41f, .41f, .45f, .83f, .41f, .41f, .41f, .34f, .34f, .50f, .50f, .44f, .34f, .41f,
            .39f, .39f, .39f, .45f, .45f, .98f, .45f, .39f, .39f, .34f, .34f, .34f, .46f, .76f, .34f, .39f,
            .70f, .70f, .70f, .72f, .95f, .95f, .72f, .70f, .70f, .70f, .34f, .34f, .34f, .34f, .34f, .36f,
            .35f, .35f, .35f, .35f, .35f, .35f, .35f, .35f, .35f, .35f, .33f, .33f, .33f, .33f, .33f, .33f,
            .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f, .60f,
            .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f, .25f
        };
        // Illustrative edge filters; these are not learned weights exported from a training run.
        private static readonly float[,] Kernels =
        {
            { -.25f, 0f, .25f, -.5f, 0f, .5f, -.25f, 0f, .25f },
            { -.25f, -.5f, -.25f, 0f, 0f, 0f, .25f, .5f, .25f },
            { -.5f, -.25f, 0f, -.25f, 0f, .25f, 0f, .25f, .5f }
        };

        internal static float Value(int column, int row) => Brightness[row * 16 + column];
        internal static float Weight(int map, int term) => Kernels[map, term];

        internal static float SumTerms(int column, int row, int count)
        {
            float sum = 0f;
            for (int term = 0; term < count; term++)
            {
                sum += Value(column + term % 3, row + term / 3) * Kernels[0, term];
            }
            return sum;
        }

        internal static float[] CalculateMap(int map)
        {
            float[] result = new float[14 * 7];
            for (int row = 0; row < 7; row++)
            {
                for (int column = 0; column < 14; column++)
                {
                    for (int term = 0; term < 9; term++)
                    {
                        result[row * 14 + column] += Value(column + term % 3, row + term / 3) * Kernels[map, term];
                    }
                }
            }
            return result;
        }
    }
}
