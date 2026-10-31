using System;
using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningProgressSnapshot
    {
        private readonly HashSet<LearningStageId> _completedStages;

        public static LearningProgressSnapshot Empty { get; } =
            new LearningProgressSnapshot(LearningStageId.None, false, Array.Empty<LearningStageId>());

        public LearningStageId CurrentStage { get; }
        public bool CanTravel { get; }
        public int CompletedCount { get; }
        public int TotalCount => LearningStageCatalog.ProgressStages.Count;
        public float Progress01 => CompletedCount / (float)TotalCount;

        internal LearningProgressSnapshot(LearningStageId currentStage, bool canTravel,
            IEnumerable<LearningStageId> completedStages)
        {
            CurrentStage = currentStage;
            CanTravel = canTravel;
            _completedStages = new HashSet<LearningStageId>(completedStages);
            int completedCount = 0;
            foreach (LearningStageDefinition stage in LearningStageCatalog.ProgressStages)
            {
                if (_completedStages.Contains(stage.Id)) { completedCount++; }
            }
            CompletedCount = completedCount;
        }

        public bool IsCompleted(LearningStageId stage) => _completedStages.Contains(stage);
    }
}
