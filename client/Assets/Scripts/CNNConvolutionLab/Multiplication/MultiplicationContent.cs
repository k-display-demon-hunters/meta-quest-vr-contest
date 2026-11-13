namespace CNNConvolution
{
    internal static class MultiplicationContent
    {
        public static string Intro() => "겹친 아홉 칸에서 같은 자리의 입력값과 가중치를 곱해요.";
        public static string Cell(int channel, int index) =>
            $"{"RGB"[channel]} 층 · {index / 3 + 1}행 {index % 3 + 1}열 · 입력값 × 가중치 ({index + 1}/9)";
        public static string Done() => "아홉 칸을 모두 곱했어요. 가중치가 0이면 곱도 0이에요.";
    }
}
