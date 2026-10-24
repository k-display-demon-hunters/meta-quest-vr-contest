using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CNNTraining
{
    public sealed class TrainingProgress
    {
        public int Epoch { get; }
        public int Epochs { get; }
        public IReadOnlyDictionary<string, double> Logs { get; }

        internal TrainingProgress(int epoch, int epochs, Dictionary<string, double> logs)
        {
            Epoch = epoch;
            Epochs = epochs;
            Logs = new ReadOnlyDictionary<string, double>(logs);
        }
    }

    public sealed class TrainingResult
    {
        public int Epoch { get; }
        public IReadOnlyDictionary<string, double> Logs { get; }
        public IReadOnlyDictionary<string, double> Evaluation { get; }

        internal TrainingResult(int epoch, Dictionary<string, double> logs, Dictionary<string, double> evaluation)
        {
            Epoch = epoch;
            Logs = new ReadOnlyDictionary<string, double>(logs);
            Evaluation = new ReadOnlyDictionary<string, double>(evaluation);
        }
    }

    public sealed class InferenceResult
    {
        public string ImageId { get; }
        public IReadOnlyList<double> Output { get; }

        internal InferenceResult(string imageId, double[] output)
        {
            ImageId = imageId;
            Output = System.Array.AsReadOnly(output);
        }
    }
}
