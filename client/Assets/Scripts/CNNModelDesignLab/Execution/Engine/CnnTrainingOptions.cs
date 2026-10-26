using System;

namespace ModelDesignScene.Execution.Engine
{
    public sealed class CnnTrainingOptions
    {
        public int Epochs { get; }
        public int BatchSize { get; }
        public float LearningRate { get; }
        public int Seed { get; }

        public CnnTrainingOptions(int epochs = 3, int batchSize = 16, float learningRate = 0.001f,
            int seed = 42)
        {
            if (epochs < 1 || batchSize < 1)
            {
                throw new ArgumentException("Epochs and batch size must be positive.");
            }
            if (float.IsNaN(learningRate) || float.IsInfinity(learningRate) || learningRate <= 0 || learningRate > 1)
            {
                throw new ArgumentException("Learning rate must be finite and in (0, 1].");
            }
            Epochs = epochs;
            BatchSize = batchSize;
            LearningRate = learningRate;
            Seed = seed;
        }
    }
}
