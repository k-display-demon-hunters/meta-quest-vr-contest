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
            // Inventory IDs describe the three physical scrolls, not the learning catalog's legacy documents.
            Stage = documentId == 0 ? LearningStageId.ScreenLab : LearningStageId.FeatureMap;
            LocationText = documentId == 0 ? "ROOM 1 입구 앞 복도" : "ROOM 2 입구 앞 복도";
        }
    }
}
