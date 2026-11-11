namespace CNNConvolution
{
    internal static class ChannelSumContent
    {
        public static string DetailedTitle(int channel) => $"{"RGB"[channel]} · 아홉 칸 계산 완료";
        public static string DetailedExplanation() => "중간 계산의 아홉 곱을 더하면 R 채널의 합 하나가 돼요.";
        public static string CompactTitle(int channel, int row, int column) => $"{"RGB"[channel]} · 출력 {row + 1}행 {column + 1}열";
        public static string CompactExplanation() => "필터 위치와 가중치는 그대로예요. 현재 채널의 아홉 곱을 더해요.";
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
