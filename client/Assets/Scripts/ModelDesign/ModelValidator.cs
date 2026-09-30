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
            bool hasFlatten = false;
            for (int i = 0; i < model.Layers.Count; i++)
            {
                LayerDefinition layer = model.Layers[i];
                string path = "/layers/" + i;
                if (layer.Type == LayerType.Flatten)
                {
                    if (hasFlatten)
                    {
                        issues.Add(new ValidationIssue(path, "Flatten must appear exactly once."));
                    }
                    hasFlatten = true;
                }
                else if (layer.Type == LayerType.Dense)
                {
                    if (!hasFlatten)
                    {
                        issues.Add(new ValidationIssue(path, "Dense requires Flatten before it."));
                    }
                    if (i < model.Layers.Count - 1 && layer.Activation == ActivationType.Softmax)
                    {
                        issues.Add(new ValidationIssue(path + "/activation", "Hidden Dense cannot use softmax."));
                    }
                }
                else if (hasFlatten)
                {
                    issues.Add(new ValidationIssue(path, "Only Dense may follow Flatten."));
                }
            }

            if (!hasFlatten)
            {
                issues.Add(new ValidationIssue("/layers", "Flatten is required."));
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
