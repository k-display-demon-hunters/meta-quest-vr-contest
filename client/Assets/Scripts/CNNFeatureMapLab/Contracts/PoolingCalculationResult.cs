namespace FeatureMapCorridor
{
    internal readonly struct PoolingCalculationResult
    {
        internal readonly int InputIndex;
        internal readonly int OutputIndex;
        internal readonly int MaximumCell;
        internal readonly float TopLeft, TopRight, BottomLeft, BottomRight, Maximum;

        internal PoolingCalculationResult(int inputIndex, int outputIndex, int maximumCell, float topLeft, float topRight,
            float bottomLeft, float bottomRight, float maximum)
        {
            InputIndex = inputIndex;
            OutputIndex = outputIndex;
            MaximumCell = maximumCell;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            Maximum = maximum;
        }
    }
}
