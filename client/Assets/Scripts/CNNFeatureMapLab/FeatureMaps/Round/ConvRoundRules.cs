namespace FeatureMapCorridor
{
    internal static class ConvRoundRules
    {
        private static readonly RoundStepSequence Full = new RoundStepSequence(
            RoundOutcome.ShowRecap, LessonStep.FilterIntro, LessonStep.FilterOpened, LessonStep.Scan,
            LessonStep.Explain, LessonStep.CellBreakdown, LessonStep.Merge);

        private static readonly RoundStepSequence Comparison = new RoundStepSequence(
            RoundOutcome.ShowRecap, LessonStep.FilterIntro, LessonStep.FilterOpened, LessonStep.Scan,
            LessonStep.Explain, LessonStep.Merge);

        internal static RoundStepSequence ForMap(int map) => map == 1 ? Comparison : Full;
    }
}
