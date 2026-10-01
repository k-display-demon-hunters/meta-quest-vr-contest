using System;
using System.Collections.Generic;

namespace ModelDesign
{
    public sealed class ValidationIssue
    {
        public string Path { get; }
        public string Message { get; }

        public ValidationIssue(string path, string message)
        {
            Path = path;
            Message = message;
        }
    }

    public static class ModelValidator
    {
        public static IReadOnlyList<ValidationIssue> Validate(ModelDefinition model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            List<ValidationIssue> issues = new List<ValidationIssue>();
            // A Dense-first model uses the server's automatic input flattening.
            bool isVector = model.Layers.Count > 0 && model.Layers[0].Type == LayerType.Dense;
            for (int i = 0; i < model.Layers.Count; i++)
            {
                LayerDefinition layer = model.Layers[i];
                string path = "/layers/" + i;
                if (layer.Type == LayerType.Flatten)
                {
                    if (isVector)
                    {
                        issues.Add(new ValidationIssue(path, "Flatten requires spatial input."));
                    }
                    isVector = true;
                }
                else if (layer.Type == LayerType.Dense)
                {
                    if (!isVector)
                    {
                        issues.Add(new ValidationIssue(path, "Dense requires Flatten before it."));
                    }
                }
                else if (isVector)
                {
                    issues.Add(new ValidationIssue(path, "Only Dense may follow vector input."));
                }
            }

            if (model.Layers.Count == 0)
            {
                issues.Add(new ValidationIssue("/layers", "An output Dense layer is required."));
            }
            else
            {
                int lastIndex = model.Layers.Count - 1;
                LayerDefinition output = model.Layers[lastIndex];
                if (output.Type != LayerType.Dense || output.Units != 10 || output.Activation != ActivationType.Softmax)
                {
                    issues.Add(new ValidationIssue("/layers/" + lastIndex,
                        "The final layer must be Dense with 10 units and softmax."));
                }
            }
            return issues.AsReadOnly();
        }
    }
}
