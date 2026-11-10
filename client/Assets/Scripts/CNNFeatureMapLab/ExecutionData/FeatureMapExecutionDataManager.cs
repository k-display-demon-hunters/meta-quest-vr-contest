using UnityEngine;

namespace FeatureMapCorridor
{
    public sealed class FeatureMapExecutionDataManager : MonoBehaviour
    {
        internal int Digit { get; private set; } = 2;
        internal FeatureMapSnapshot Snapshot { get; private set; }

        private DigitExecutionData Data => DigitExecutionData.ForDigit(Digit);

        internal void Initialize() => RefreshSnapshot();

        internal void SelectDigit(int digit)
        {
            Digit = digit;
            RefreshSnapshot();
        }

        internal RoundCalculationResult GetRound(int layer, int map) => Data.GetRound(Digit, layer, map);
        internal PoolingCalculationResult GetPooling(int channel) => Data.GetPooling(channel);

        private void RefreshSnapshot() => Snapshot = Data.GetSnapshot();
    }
}
