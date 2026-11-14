using UnityEngine;

namespace CNNConvolution
{
    internal static class WorkbenchColors
    {
        // Match the Feature Map Lab grid, card faces and mint track.
        public const float HeadingHeight = 0.51f;
        public const int HeadingFontSize = 38;

        public static readonly Color PanelFrame = new Color32(58, 74, 90, 255);
        public static readonly Color PanelFace = new Color32(22, 32, 46, 255);
        public static readonly Color PlacementMint = new Color32(98, 228, 217, 255);

        public static readonly Color CalculationAccent = new Color(1f, 0.86f, 0.57f);

        public static Color Channel(int channel)
        {
            return channel == 0 ? new Color(1f, 0.40f, 0.42f)
                : channel == 1 ? new Color(0.41f, 0.93f, 0.65f)
                : new Color(0.38f, 0.68f, 1f);
        }
    }
}
