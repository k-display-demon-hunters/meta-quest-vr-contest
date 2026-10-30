namespace Parchment
{
    public readonly struct ParchmentInventoryItem
    {
        public string Title { get; }
        public bool Collected { get; }
        public ParchmentLocation Location { get; }
        public string LocationText => Location switch
        {
            ParchmentLocation.PixelLabEntrance => "픽셀 체험 입구 앞",
            ParchmentLocation.ConvolutionLabEntrance => "합성곱 체험 입구 앞",
            ParchmentLocation.FeatureMapLabEntrance => "특징 맵 체험 입구 앞",
            ParchmentLocation.ModelDesignLabEntrance => "모델 설계 체험 입구 앞",
            _ => "위치 배정 전"
        };
        public ParchmentInventoryItem(string title, bool collected, ParchmentLocation location)
        { Title = title; Collected = collected; Location = location; }
    }
}
