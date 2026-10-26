using ModelDesignScene.Execution.Engine;

namespace ModelDesignScene.Execution
{
    internal static class CnnTrainingSettings
    {
        private const int Epochs = 10;
        private const int BatchSize = 16;
        private const float LearningRate = 0.001f;
        private const int Seed = 42;

        internal static CnnTrainingOptions CreateOptions()
        {
            return new CnnTrainingOptions(Epochs, BatchSize, LearningRate, Seed);
        }
    }
}
