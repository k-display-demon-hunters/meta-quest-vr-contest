using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RealityLab
{
    public enum RealityStage { AwaitingModel, Ready, Captured, Recognizing, Result, Summary }

    public sealed class RealityAttempt
    {
        public RealityRecognitionData Input { get; }
        public IReadOnlyList<double> Scores { get; }
        public int Sample { get; }
        public int Attempt { get; }
        public int Actual { get; }
        public int Predicted { get; }
        public bool IsCorrect => Actual == Predicted;

        public RealityAttempt(RealityRecognitionData input, IReadOnlyList<double> scores,
            int sample, int attempt, int actual)
        {
            Input = input;
            double[] copy = new double[scores.Count];
            int predicted = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = scores[i];
                if (copy[i] > copy[predicted]) { predicted = i; }
            }
            Scores = System.Array.AsReadOnly(copy);
            Sample = sample;
            Attempt = attempt;
            Actual = actual;
            Predicted = predicted;
        }
    }

    // Session-only observations. Labels never become training or inference input.
    public sealed class RealitySession
    {
        private readonly List<RealityAttempt> _attempts = new List<RealityAttempt>();
        public ReadOnlyCollection<RealityAttempt> Attempts { get; }
        public int Sample { get; private set; } = 1;
        public int Attempt { get; private set; } = 1;
        public RealityAttempt Previous { get; private set; }

        public RealitySession() { Attempts = _attempts.AsReadOnly(); }

        public RealityAttempt Record(RealityRecognitionData input, IReadOnlyList<double> scores, int actual)
        {
            RealityAttempt result = new RealityAttempt(input, scores, Sample, Attempt, actual);
            _attempts.Add(result);
            return result;
        }

        public void PrepareNext(bool retry)
        {
            if (retry)
            {
                Previous = _attempts[_attempts.Count - 1];
                Attempt++;
            }
            else
            {
                Previous = null;
                Sample++;
                Attempt = 1;
            }
        }
    }
}
