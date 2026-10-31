using System;
namespace Learning
{
    public interface ILearningExperience
    {
        System.Collections.IEnumerator EndSession();
        LearningStageId StageId { get; }
        float Progress { get; }
        bool CanExit { get; }
        event Action<float> ProgressChanged;
        event Action<bool> ExitAvailabilityChanged;
    }
}
