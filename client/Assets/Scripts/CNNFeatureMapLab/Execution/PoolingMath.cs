using UnityEngine;

namespace FeatureMapCorridor
{
    internal static class PoolingMath
    {
        internal static PoolingCalculationResult ReadWindow(float[] source, int inputSize, int outputIndex)
        {
            int outputSize = inputSize / 2;
            int inputIndex = 2 * (outputIndex / outputSize * inputSize + outputIndex % outputSize);
            float topLeft = source[inputIndex];
            float topRight = source[inputIndex + 1];
            float bottomLeft = source[inputIndex + inputSize];
            float bottomRight = source[inputIndex + inputSize + 1];
            float maximum = Mathf.Max(Mathf.Max(topLeft, topRight), Mathf.Max(bottomLeft, bottomRight));
            int maximumCell = topLeft == maximum ? 0 : topRight == maximum ? 1 : bottomLeft == maximum ? 2 : 3;
            return new PoolingCalculationResult(inputIndex, outputIndex, maximumCell,
                topLeft, topRight, bottomLeft, bottomRight, maximum);
        }
    }
}
