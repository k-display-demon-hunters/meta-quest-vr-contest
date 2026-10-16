using System;
using System.Collections.Generic;

namespace ModelDesign
{
    public static class ModelValidator
    {
        public static IReadOnlyList<ValidationIssue> ValidateEditing(ModelDefinition model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            for (int i = 0; i < model.Layers.Count; i++)
            {
                if (model.Layers[i].Type == LayerType.Input)
                {
                    return ValidateDirection(model, i != 0);
                }
                if (model.Layers[i].Type == LayerType.Output)
                {
                    return ValidateDirection(model, i == 0);
                }
            }
            IReadOnlyList<ValidationIssue> forward = ValidateDirection(model, false);
            IReadOnlyList<ValidationIssue> reverse = ValidateDirection(model, true);
            return forward.Count <= reverse.Count ? forward : reverse;
        }

        private static IReadOnlyList<ValidationIssue> ValidateDirection(ModelDefinition model, bool reverse)
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();
            // Detached fragments have unknown input shape until their preceding chain is connected.
            bool? isVector = null;
            for (int i = 0; i < model.Layers.Count; i++)
            {
                int blockIndex = reverse ? model.Layers.Count - 1 - i : i;
                LayerDefinition layer = model.Layers[blockIndex];
                string path = "/blocks/" + blockIndex;
                switch (layer.Type)
                {
                    case LayerType.Input:
                        if (i != 0)
                        {
                            issues.Add(new ValidationIssue(model, path, "Input must be the first block."));
                        }
                        isVector = false;
                        break;
                    case LayerType.Output:
                        if (i != model.Layers.Count - 1)
                        {
                            issues.Add(new ValidationIssue(model, path, "Output must be the last block."));
                        }
                        goto case LayerType.Dense;
                    case LayerType.Dense:
                        bool autoFlatten = i == 1 && model.Layers[reverse ? model.Layers.Count - 1 : 0].Type == LayerType.Input;
                        if (isVector == false && !autoFlatten)
                        {
                            issues.Add(new ValidationIssue(model, path, "Spatial layers require Flatten before Dense or Output."));
                        }
                        isVector = true;
                        break;
                    case LayerType.Flatten:
                        if (isVector == true)
                        {
                            issues.Add(new ValidationIssue(model, path, "Flatten requires spatial input."));
                        }
                        isVector = true;
                        break;
                    case LayerType.Conv2D:
                    case LayerType.MaxPooling2D:
                        if (isVector == true)
                        {
                            issues.Add(new ValidationIssue(model, path, "Spatial layers cannot follow Dense or Flatten."));
                        }
                        isVector = false;
                        break;
                }
            }
            return issues.AsReadOnly();
        }

        public static IReadOnlyList<ValidationIssue> ValidateForTransmission(ModelWorkspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }
            ModelDefinition model = workspace.InputModel;
            List<ValidationIssue> issues = new List<ValidationIssue>(ValidateEditing(model));
            bool inputAtStart = ReferenceEquals(model.Layers[0], workspace.Input);
            bool inputAtEnd = ReferenceEquals(model.Layers[model.Layers.Count - 1], workspace.Input);
            if (!inputAtStart && !inputAtEnd)
            {
                issues.Add(new ValidationIssue(model, "/blocks", "Input must be at a chain endpoint."));
            }
            LayerDefinition oppositeEndpoint = model.Layers[inputAtStart ? model.Layers.Count - 1 : 0];
            if (!ReferenceEquals(oppositeEndpoint, workspace.Output))
            {
                issues.Add(new ValidationIssue(model, "/blocks", "Connect the Input chain to the fixed Output block before sending."));
            }
            return issues.AsReadOnly();
        }
    }
}
