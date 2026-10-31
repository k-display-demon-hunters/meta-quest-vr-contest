namespace Parchment
{
    public readonly struct ParchmentInventoryItem
    {
        public string Title { get; }
        public bool Collected { get; }
        public ParchmentLocation Location { get; }
        public string LocationText => Location switch
        {
            ParchmentLocation.PixelLabEntrance => "ROOM 1 입구 앞 복도",
            ParchmentLocation.ConvolutionLabEntrance => "합성곱 체험 입구 앞",
            ParchmentLocation.FeatureMapLabEntrance => "ROOM 2 입구 앞 복도",
            ParchmentLocation.ModelDesignLabEntrance => "모델 설계 체험 입구 앞",
            _ => "위치 배정 전"
        };
        public ParchmentInventoryItem(string title, bool collected, ParchmentLocation location)
        { Title = title; Collected = collected; Location = location; }
    }
}
