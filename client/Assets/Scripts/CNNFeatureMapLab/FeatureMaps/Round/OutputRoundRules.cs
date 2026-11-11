namespace FeatureMapCorridor
{
    internal static class OutputRoundRules
    {
        internal static RoundStepSequence Sequence { get; } = new RoundStepSequence(
            RoundOutcome.ShowRecap, LessonStep.CompareTemplates, LessonStep.FillProbabilities, LessonStep.RevealAnswer);

        internal static RoundStepSequence Details { get; } = new RoundStepSequence(
            RoundOutcome.ShowRecap, LessonStep.FilterIntro, LessonStep.FilterOpened, LessonStep.Scan,
            LessonStep.Explain, LessonStep.Merge);
    }
}
