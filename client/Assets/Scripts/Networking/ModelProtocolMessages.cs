using System.Collections.Generic;

namespace Networking
{
    public enum ModelOperation
    {
        Idle,
        Training,
        CancellingTraining,
        Inferring
    }

    public sealed class TrainingProgress
    {
        public int Epoch { get; internal set; }
        public int Epochs { get; internal set; }
        public IReadOnlyDictionary<string, double> Logs { get; internal set; }
    }

    public sealed class TrainingResult
    {
        public int Epoch { get; internal set; }
        public IReadOnlyDictionary<string, double> Logs { get; internal set; }
    }

    public sealed class InferenceResult
    {
        public string ImageId { get; internal set; }
        public IReadOnlyList<double> Output { get; internal set; }
    }

    public sealed class ModelServerError
    {
        public string RequestType { get; internal set; }
        public string Code { get; internal set; }
        public string Message { get; internal set; }
    }
}
