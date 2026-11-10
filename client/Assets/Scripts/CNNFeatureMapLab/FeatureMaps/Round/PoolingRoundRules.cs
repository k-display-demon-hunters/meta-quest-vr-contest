namespace FeatureMapCorridor
{
    internal static class PoolingRoundRules
    {
        internal static RoundStepSequence Sequence { get; } = new RoundStepSequence(
            RoundOutcome.Complete, LessonStep.PoolingWindow, LessonStep.PoolingValues,
            LessonStep.PoolingMaximum, LessonStep.PoolingApply);
    }
}
