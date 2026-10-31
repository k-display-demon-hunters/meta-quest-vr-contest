using System.Collections.Generic;

namespace Learning
{
    public sealed class LearningProgress
    {
        private readonly Dictionary<LearningStageId, float> _stageProgress = new Dictionary<LearningStageId, float>();
        private readonly HashSet<LearningStageId> _completedStages = new HashSet<LearningStageId>();

        public bool IsCompleted(LearningStageId stage) => _completedStages.Contains(stage);

        public bool RecordCompletion(LearningStageId stage)
        {
            if (stage == LearningStageId.None) { return false; }
            _stageProgress[LearningStageCatalog.Get(stage).Id] = 1f;
            return _completedStages.Add(stage);
        }

        public bool RecordProgress(LearningStageId stage, float value)
        {
            LearningStageCatalog.Get(stage);
            value = UnityEngine.Mathf.Clamp01(value);
            _stageProgress.TryGetValue(stage, out float previous);
            if (value <= previous) { return false; }
            _stageProgress[stage] = value;
            if (value >= 1f) { _completedStages.Add(stage); }
            return true;
        }

        public LearningProgressSnapshot CreateSnapshot(LearningStageId currentStage, bool canTravel) =>
            new LearningProgressSnapshot(currentStage, canTravel, _completedStages, _stageProgress);
    }
}
