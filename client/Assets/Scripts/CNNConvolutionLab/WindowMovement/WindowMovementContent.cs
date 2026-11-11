namespace CNNConvolution
{
    internal static class WindowMovementContent
    {
        public static string Title(int row, int column) => column == 0 && row > 0
            ? "다음 행의 맨 왼쪽으로 이동해요"
            : "필터를 오른쪽 한 칸 옮겨요";
        public static string Explanation() => "새 위치에서도 R · G · B를 각각 계산해요.";
    }
}
