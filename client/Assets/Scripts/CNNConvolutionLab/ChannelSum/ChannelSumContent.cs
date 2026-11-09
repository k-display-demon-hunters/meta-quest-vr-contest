namespace CNNConvolution
{
    internal static class ChannelSumContent
    {
        public static string DetailedTitle(int channel) => $"{"RGB"[channel]} · 아홉 칸 계산 완료";
        public static string DetailedExplanation() => "아홉 곱을 오른쪽 판 가운데로 모아 더해요.";
        public static string DetailedGuide(int channel) => $"아홉 칸의 곱을 모두 더하면 {"RGB"[channel]}의 합이에요.";
        public static string EmptyFeedback() => "";
        public static string CompactTitle(int channel, int row, int column) => $"{"RGB"[channel]} · 출력 {row + 1}행 {column + 1}열";
        public static string CompactExplanation() => "현재 위치의 아홉 곱을 모두 더해요.";
        public static string CompactGuide() => "R · G · B의 계산은 같은 위치에서 이어져요.";
        public static string CompactFeedback() => "";
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
