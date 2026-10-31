using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningProgress
    {
        private readonly HashSet<LearningStageId> _completedStages = new HashSet<LearningStageId>();

        public bool IsCompleted(LearningStageId stage) => _completedStages.Contains(stage);

        public bool RecordCompletion(LearningStageId stage)
        {
            if (stage == LearningStageId.None) { return false; }
            return _completedStages.Add(LearningStageCatalog.Get(stage).Id);
        }

        public LearningProgressSnapshot CreateSnapshot(LearningStageId currentStage, bool canTravel) =>
            new LearningProgressSnapshot(currentStage, canTravel, _completedStages);
    }
}
