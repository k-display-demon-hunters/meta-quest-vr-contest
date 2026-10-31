using System;
using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningProgressSnapshot
    {
        private readonly Dictionary<LearningStageId, float> _stageProgress;
        private readonly HashSet<LearningStageId> _completedStages;

        public static LearningProgressSnapshot Empty { get; } =
            new LearningProgressSnapshot(LearningStageId.None, false, Array.Empty<LearningStageId>(), new Dictionary<LearningStageId, float>());

        public LearningStageId CurrentStage { get; }
        public bool CanTravel { get; }
        public int CompletedCount { get; }
        public int TotalCount => LearningStageCatalog.ProgressStages.Count;
        public float Progress01 { get; }

        internal LearningProgressSnapshot(LearningStageId currentStage, bool canTravel,
            IEnumerable<LearningStageId> completedStages, Dictionary<LearningStageId, float> stageProgress)
        {
            CurrentStage = currentStage;
            CanTravel = canTravel;
            _stageProgress = new Dictionary<LearningStageId, float>(stageProgress);
            _completedStages = new HashSet<LearningStageId>(completedStages);
            int completedCount = 0;
            float totalProgress = 0f;
            foreach (LearningStageDefinition stage in LearningStageCatalog.ProgressStages)
            {
                totalProgress += GetProgress(stage.Id);
                if (_completedStages.Contains(stage.Id)) { completedCount++; }
            }
            CompletedCount = completedCount;
            Progress01 = totalProgress / TotalCount;
        }

        public float GetProgress(LearningStageId stage) => _stageProgress.TryGetValue(stage, out float value) ? value : 0f;

        public bool IsCompleted(LearningStageId stage) => _completedStages.Contains(stage);
    }
}
