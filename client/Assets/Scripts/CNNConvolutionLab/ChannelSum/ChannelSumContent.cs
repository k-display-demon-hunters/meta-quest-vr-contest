namespace CNNConvolution
{
    internal static class ChannelSumContent
    {
        public static string Detailed(int channel) => $"아홉 곱을 모두 더하면 {"RGB"[channel]} 층의 합 하나가 돼요.";
        public static string Compact(int channel) => $"{"RGB"[channel]} 층도 같은 자리끼리 곱하고 모두 더해요.";
        public static string Equation(ConvolutionCalculation calculation, int channel, bool productsOnly)
        {
            string color = channel == 0 ? "FF666B" : channel == 1 ? "69EDA6" : "61ADFF";
            string equation = $"<color=#{color}>{"RGB"[channel]}</color> ";
            for (int index = 0; index < 9; index += 4)
            {
                if (index > 0) { equation += " + "; }
                equation += $"<color=#{color}>{calculation.Input(channel, index)}</color>";
                if (!productsOnly) { equation += $"×<color=#{color}>1</color>"; }
            }
            return equation + " = " + calculation.Sum(channel);
        }
    }
}
