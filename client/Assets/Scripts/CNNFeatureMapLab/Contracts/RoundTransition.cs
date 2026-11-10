namespace FeatureMapCorridor
{
    internal enum RoundRequest
    {
        Next, Previous, FilterExpanded
    }

    internal enum RoundOutcome
    {
        Stay, Move, BeforeFirstStep, ShowRecap, Complete
    }

    internal readonly struct RoundTransition
    {
        internal RoundOutcome Outcome { get; }
        internal LessonStep Step { get; }

        internal RoundTransition(RoundOutcome outcome, LessonStep step = default)
        {
            Outcome = outcome;
            Step = step;
        }
    }
}
