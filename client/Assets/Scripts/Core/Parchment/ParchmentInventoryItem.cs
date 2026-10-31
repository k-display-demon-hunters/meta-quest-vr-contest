using Learning;

namespace Parchment
{
    public readonly struct ParchmentInventoryItem
    {
        public string Title { get; }
        public bool Collected { get; }
        public LearningStageId EntranceStage { get; }
        public string LocationText => EntranceStage == LearningStageId.None
            ? "위치 배정 전"
            : LearningStageCatalog.Get(EntranceStage).Title + " 체험 입구 앞";

        public ParchmentInventoryItem(int documentId, string title, bool collected)
        {
            Title = title;
            Collected = collected;
            EntranceStage = LearningStageCatalog.GetParchmentEntranceStage(documentId);
        }
    }
}
