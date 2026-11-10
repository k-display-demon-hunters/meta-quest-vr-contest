using System;

namespace FeatureMapCorridor
{
    internal sealed class RoundStepSequence
    {
        private readonly RoundOutcome _finish;
        private readonly LessonStep[] _steps;

        internal RoundStepSequence(RoundOutcome finish, params LessonStep[] steps)
        {
            _finish = finish;
            _steps = steps;
        }

        internal LessonStep First => _steps[0];
        internal LessonStep Last => _steps[_steps.Length - 1];
        internal static bool WaitsForFilter(LessonStep step) => step == LessonStep.FilterIntro;

        internal RoundTransition Evaluate(LessonStep step, RoundRequest request, bool presentationPending)
        {
            int index = Array.IndexOf(_steps, step);
            if (request == RoundRequest.Previous)
            {
                return index == 0
                    ? new RoundTransition(RoundOutcome.BeforeFirstStep)
                    : new RoundTransition(RoundOutcome.Move, _steps[index - 1]);
            }

            if (request == RoundRequest.FilterExpanded)
            {
                if (!WaitsForFilter(step))
                {
                    return new RoundTransition(RoundOutcome.Stay);
                }
            }
            else if (WaitsForFilter(step) || presentationPending)
            {
                return new RoundTransition(RoundOutcome.Stay);
            }

            return index == _steps.Length - 1
                ? new RoundTransition(_finish)
                : new RoundTransition(RoundOutcome.Move, _steps[index + 1]);
        }
    }
}
