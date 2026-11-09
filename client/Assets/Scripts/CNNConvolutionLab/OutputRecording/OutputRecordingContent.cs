namespace CNNConvolution
{
    internal static class OutputRecordingContent
    {
        public static string FirstTitle() => "세 채널의 합이 출력값 하나가 돼요";
        public static string FirstExplanation() => "오른쪽 판은 지금부터 출력값이 채워지는 지도예요.";
        public static string RecordTitle(int row, int column) => $"출력 {row + 1}행 {column + 1}열에 기록해요";
        public static string RecordExplanation() => "R 합 + G 합 + B 합 = 출력값 하나";
        public static string Equation(ConvolutionCalculation calculation) => $"<color=#FF666B>R {calculation.Sum(0)}</color> + "
                + $"<color=#69EDA6>G {calculation.Sum(1)}</color> + "
                + $"<color=#61ADFF>B {calculation.Sum(2)}</color> = "
                + calculation.Output(calculation.Row, calculation.Column);
    }
}
