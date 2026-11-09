using UnityEngine;

namespace CNNConvolution
{
    internal static class WorkbenchColors
    {
        public static readonly Color CalculationAccent = new Color(1f, 0.86f, 0.57f);
        public static readonly Color PendingValue = new Color(0.46f, 0.53f, 0.57f);

        public static Color Channel(int channel)
        {
            return channel == 0 ? new Color(1f, 0.40f, 0.42f)
                : channel == 1 ? new Color(0.41f, 0.93f, 0.65f)
                : new Color(0.38f, 0.68f, 1f);
        }
    }
}
