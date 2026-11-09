namespace CNNConvolution
{
    internal static class MultiplicationContent
    {
        public static string CellTitle(int channel, int index) => $"{"RGB"[channel]} · {index / 3 + 1}행 {index % 3 + 1}열 · {index + 1} / 9";
        public static string Explanation() => "현재 칸의 입력값과 같은 위치의 가중치를 곱해요.";
        public static string Guide() => "원래 숫자는 제자리에 있고 사본만 이동해요.";
        public static string WeightFeedback(int weight) => weight == 0
                    ? "0을 곱한 결과도 오른쪽 판에 기록해요."
                    : "1을 곱하면 입력값이 그대로 남아요.";
        public static string RecordedFeedback(int product) => $"곱한 값 {product}을 같은 위치에 기록했어요.";
    }
}
