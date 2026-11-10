namespace CNNConvolution
{
    internal static class MultiplicationContent
    {
        public static string CellTitle(int channel, int index) => $"{"RGB"[channel]} · {index / 3 + 1}행 {index % 3 + 1}열 · {index + 1} / 9";
        public static string Explanation() => "현재 칸의 입력값과 같은 위치의 가중치를 곱해요.";
    }
}
