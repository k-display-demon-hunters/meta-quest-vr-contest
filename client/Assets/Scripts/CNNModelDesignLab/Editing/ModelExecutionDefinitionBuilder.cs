using System;
using System.Collections.Generic;
using ModelDesignScene.Execution.Engine;

namespace ModelDesignScene.Editing
{
    internal static class ModelExecutionDefinitionBuilder
    {
        public static bool TryBuild(ModelWorkspace workspace, CnnTrainingOptions options,
            out ModelExecutionDefinition definition, out IReadOnlyList<ValidationIssue> issues)
        {
            definition = null;
            issues = ModelValidator.ValidateConnectedChain(workspace);
            if (issues.Count != 0)
            {
                return false;
            }

            ModelDefinition model = workspace.InputModel;
            List<CnnLayerDefinition> layers = new List<CnnLayerDefinition>();
            bool reverse = ReferenceEquals(model.Layers[model.Layers.Count - 1], workspace.Input);
            for (int index = 1; index < model.Layers.Count; index++)
            {
                LayerDefinition layer = model.Layers[reverse ? model.Layers.Count - 1 - index : index];
                switch (layer.Type)
                {
                    case LayerType.Conv2D:
                        layers.Add(new CnnLayerDefinition(LayerKind.Conv2D, layer.Filters,
                            layer.Height, layer.Width, ConvertActivation(layer.Activation)));
                        break;
                    case LayerType.MaxPooling2D:
                        layers.Add(new CnnLayerDefinition(LayerKind.MaxPooling2D,
                            height: layer.Height, width: layer.Width));
                        break;
                    case LayerType.Flatten:
                        layers.Add(new CnnLayerDefinition(LayerKind.Flatten));
                        break;
                    case LayerType.Dense:
                    case LayerType.Output:
                        layers.Add(new CnnLayerDefinition(LayerKind.Dense, layer.Units,
                            activation: ConvertActivation(layer.Activation)));
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported model block.");
                }
            }

            try
            {
                // Only value snapshots cross the scene/engine boundary; editing cannot mutate a run.
                ModelExecutionDefinition snapshot = new ModelExecutionDefinition(28, 28, 1, layers);
                snapshot.Validate(options ?? new CnnTrainingOptions());
                definition = snapshot;
                return true;
            }
            catch (ArgumentException exception)
            {
                issues = new List<ValidationIssue>
                {
                    new ValidationIssue(model, "/blocks", exception.Message)
                }.AsReadOnly();
                return false;
            }
        }

        public static ModelExecutionDefinition Build(ModelWorkspace workspace, CnnTrainingOptions options = null)
        {
            if (!TryBuild(workspace, options, out ModelExecutionDefinition definition,
                out IReadOnlyList<ValidationIssue> issues))
            {
                throw new InvalidOperationException(issues[0].Path + ": " + issues[0].Message);
            }
            return definition;
        }

        public static bool Matches(ModelExecutionDefinition first, ModelExecutionDefinition second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            if (first == null || second == null || first.Height != second.Height
                || first.Width != second.Width || first.Channels != second.Channels
                || first.Layers.Count != second.Layers.Count)
            {
                return false;
            }
            for (int index = 0; index < first.Layers.Count; index++)
            {
                CnnLayerDefinition left = first.Layers[index];
                CnnLayerDefinition right = second.Layers[index];
                if (left.Kind != right.Kind || left.Units != right.Units || left.Height != right.Height
                    || left.Width != right.Width || left.Activation != right.Activation)
                {
                    return false;
                }
            }
            return true;
        }

        private static CnnActivation ConvertActivation(ActivationType activation)
        {
            switch (activation)
            {
                case ActivationType.Linear: return CnnActivation.Linear;
                case ActivationType.ReLU: return CnnActivation.ReLU;
                case ActivationType.Sigmoid: return CnnActivation.Sigmoid;
                case ActivationType.Tanh: return CnnActivation.Tanh;
                case ActivationType.Softmax: return CnnActivation.Softmax;
                default: throw new ArgumentOutOfRangeException(nameof(activation));
            }
        }
    }
}
