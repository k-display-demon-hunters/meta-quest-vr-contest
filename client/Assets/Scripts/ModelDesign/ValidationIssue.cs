namespace ModelDesign
{
    public sealed class ValidationIssue
    {
        public ModelDefinition Model { get; }
        public string Path { get; }
        public string Message { get; }

        public ValidationIssue(ModelDefinition model, string path, string message)
        {
            Model = model;
            Path = path;
            Message = message;
        }
    }
}
