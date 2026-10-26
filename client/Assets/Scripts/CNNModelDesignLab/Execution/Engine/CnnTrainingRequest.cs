namespace ModelDesignScene.Execution.Engine
{
    // Execution validates these immutable inputs before passing the request to the engine.
    public sealed class CnnTrainingRequest
    {
        internal ModelExecutionDefinition Definition { get; }
        internal TrainingData Data { get; }
        internal CnnTrainingOptions Options { get; }

        public CnnTrainingRequest(ModelExecutionDefinition definition, TrainingData data, CnnTrainingOptions options)
        {
            Definition = definition;
            Data = data;
            Options = options;
        }
    }
}
