namespace FeatureMapCorridor
{
    internal static class ConvRoundRules
    {
        private static readonly RoundStepSequence Sequence = new RoundStepSequence(
            RoundOutcome.ShowRecap, LessonStep.FilterOpened, LessonStep.Scan, LessonStep.Explain);

        internal static RoundStepSequence ForMap(MapId round) => Sequence;
    }
}
