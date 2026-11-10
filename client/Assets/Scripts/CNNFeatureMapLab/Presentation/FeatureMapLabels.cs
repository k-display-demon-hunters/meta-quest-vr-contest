namespace FeatureMapCorridor
{
    internal static class FeatureMapLabels
    {
        internal const string DoorOpen = "열림 · 다음 방으로";
        internal const string InputR = "입력 R";
        internal const string InputG = "입력 G";
        internal const string InputB = "입력 B";
        internal static readonly string[] Channels =
        {
            "R", "G", "B"
        };
        internal static readonly string[] InputChannels =
        {
            InputR, InputG, InputB
        };
        internal static readonly string[] Marks =
        {
            "①", "②", "③", "④", "⑤"
        };
        internal static string MapId(int layer, int map) => $"피처맵 {layer}-{map + 1}";
        internal static string Answer(int digit) => $"이 그림은 숫자 {digit}!";
        internal static string FilterTitle(int layer, int count, int size) => $"필터 · {size}×{size} 판 {count}장";
    }
}
