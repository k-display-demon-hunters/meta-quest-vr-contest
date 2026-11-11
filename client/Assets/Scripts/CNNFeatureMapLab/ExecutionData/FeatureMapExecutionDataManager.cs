using UnityEngine;

namespace FeatureMapCorridor
{
    public sealed class FeatureMapExecutionDataManager : MonoBehaviour
    {
        internal FeatureMapSnapshot GetSnapshot(int digit) =>
            DigitExecutionData.ForDigit(digit).GetSnapshot();

        internal RoundCalculationResult GetRound(int digit, int layer, int map) =>
            DigitExecutionData.ForDigit(digit).GetRound(digit, layer, map);

        internal PoolingCalculationResult GetPooling(int digit, int channel) =>
            DigitExecutionData.ForDigit(digit).GetPooling(channel);
    }
}
