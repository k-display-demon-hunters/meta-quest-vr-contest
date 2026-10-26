using System;

namespace ModelDesignScene.Execution.Engine
{
    public sealed class CnnTrainingOptions
    {
        public int Epochs { get; }
        public int BatchSize { get; }
        public float LearningRate { get; }
        public int Seed { get; }
        public int MaxParameters { get; }
        public long MaxMultiplyAdds { get; }
        public int MaxActivationElements { get; }
        public int MaxLayers { get; }

        public CnnTrainingOptions(int epochs = 3, int batchSize = 16, float learningRate = 0.001f,
            int seed = 42, int maxParameters = 100000, long maxMultiplyAdds = 2000000,
            int maxActivationElements = 32768, int maxLayers = 16)
        {
            if (epochs < 1 || epochs > 100 || batchSize < 1 || batchSize > 256)
            {
                throw new ArgumentException("Epochs must be 1–100 and batch size 1–256.");
            }
            if (float.IsNaN(learningRate) || float.IsInfinity(learningRate) || learningRate <= 0 || learningRate > 1)
            {
                throw new ArgumentException("Learning rate must be finite and in (0, 1].");
            }
            // Hard ceilings prevent settings mistakes from allocating unbounded Quest memory.
            if (maxParameters < 1 || maxParameters > 2000000 || maxMultiplyAdds < 1 || maxMultiplyAdds > 100000000 ||
                maxActivationElements < 1 || maxActivationElements > 200000 || maxLayers < 3 || maxLayers > 32)
            {
                throw new ArgumentException("Model resource limits exceed supported bounds.");
            }
            Epochs = epochs;
            BatchSize = batchSize;
            LearningRate = learningRate;
            Seed = seed;
            MaxParameters = maxParameters;
            MaxMultiplyAdds = maxMultiplyAdds;
            MaxActivationElements = maxActivationElements;
            MaxLayers = maxLayers;
        }
    }
}
