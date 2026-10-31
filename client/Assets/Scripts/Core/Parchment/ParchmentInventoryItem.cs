using Learning;

namespace Parchment
{
    public readonly struct ParchmentInventoryItem
    {
        public string Title { get; }
        public bool Collected { get; }
        public LearningStageId Stage { get; }
        public string LocationText { get; }

        public ParchmentInventoryItem(int documentId, string title, bool collected)
        {
            Title = title;
            Collected = collected;
            LearningStageId stage = LearningStageCatalog.GetParchmentStage(documentId);
            Stage = stage;
            if (stage == LearningStageId.None)
            {
                LocationText = "위치 배정 전";
                return;
            }
            LearningStageDefinition definition = LearningStageCatalog.Get(stage);
            bool atEntrance = false;
            foreach (int id in definition.EntranceParchmentIds)
            {
                if (id == documentId) { atEntrance = true; break; }
            }
            LocationText = definition.Title + (atEntrance ? " 체험 입구 앞" : " 체험실 내부");
        }
    }
}
