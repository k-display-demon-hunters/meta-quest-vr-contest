using System;
using ModelDesignScene.Model;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Execution
{
    internal static class ModelExecutionValidator
    {
        internal static bool TryPrepareTraining(ModelExecutionDefinition definition, TrainingData data,
            CnnTrainingOptions options, int?[] slotIndices,
            out CnnTrainingRequest request, out ValidationIssue issue)
        {
            request = null;
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (!TryValidateDefinition(definition, slotIndices, out issue))
            {
                return false;
            }
            if (data.Training.InputSize != definition.InputSize)
            {
                Debug.LogError("[ERR-CHECK] Dataset shape does not match model input.");
                issue = new ValidationIssue(null, ValidationIssueKind.Unmapped);
                return false;
            }
            request = new CnnTrainingRequest(definition, data, options);
            return true;
        }

        private static bool TryValidateDefinition(ModelExecutionDefinition definition,
            int?[] slotIndices, out ValidationIssue issue)
        {
            try
            {
                ValidateDefinition(definition);
                issue = null;
                return true;
            }
            catch (ModelExecutionValidationException exception)
            {
                int? slot = exception.LayerIndex.HasValue ? slotIndices[exception.LayerIndex.Value] : null;
                Debug.LogError($"[ERR-CHECK] {exception}");
                issue = new ValidationIssue(slot, exception.Kind);
                return false;
            }
        }

        private static void ValidateDefinition(ModelExecutionDefinition definition)
        {
            if (definition.Layers.Count < 2)
            {
                throw new ModelExecutionValidationException("Spatial layers require Flatten before Dense or Output.", kind: ValidationIssueKind.FlattenRequired);
            }
            int height = definition.Height;
            int width = definition.Width;
            bool flattened = false;
            for (int i = 0; i < definition.Layers.Count; i++)
            {
                CnnLayerDefinition layer = definition.Layers[i];
                if (!Enum.IsDefined(typeof(CnnActivation), layer.Activation))
                {
                    throw new ModelExecutionValidationException("Unsupported activation.", i);
                }
                switch (layer.Kind)
                {
                    case LayerKind.Conv2D:
                        if (flattened)
                        {
                            throw new ModelExecutionValidationException("Spatial layers cannot follow Dense or Flatten.", i, ValidationIssueKind.SpatialLayerAfterVector);
                        }
                        if (layer.Height > height || layer.Width > width)
                        {
                            throw new ModelExecutionValidationException("Invalid Conv2D shape or placement.", i);
                        }
                        height -= layer.Height - 1;
                        width -= layer.Width - 1;
                        break;
                    case LayerKind.MaxPooling2D:
                        if (flattened)
                        {
                            throw new ModelExecutionValidationException("Spatial layers cannot follow Dense or Flatten.", i, ValidationIssueKind.SpatialLayerAfterVector);
                        }
                        if (layer.Height > height || layer.Width > width)
                        {
                            throw new ModelExecutionValidationException("Invalid MaxPooling2D shape or placement.", i);
                        }
                        height /= layer.Height;
                        width /= layer.Width;
                        break;
                    case LayerKind.Flatten:
                        if (flattened)
                        {
                            throw new ModelExecutionValidationException("Flatten requires spatial input.", i, ValidationIssueKind.SpatialInputRequired);
                        }
                        flattened = true;
                        break;
                    case LayerKind.Dense:
                        if (!flattened)
                        {
                            throw new ModelExecutionValidationException("Spatial layers require Flatten before Dense or Output.", i, ValidationIssueKind.FlattenRequired);
                        }
                        break;
                    default:
                        throw new ModelExecutionValidationException("Unsupported layer type.", i);
                }
            }
            CnnLayerDefinition final = definition.Layers[definition.Layers.Count - 1];
            if (final.Kind != LayerKind.Dense || final.Units != LayerLimits.OutputClassCount ||
                final.Activation != CnnActivation.Softmax)
            {
                throw new ModelExecutionValidationException($"The output must be Dense({LayerLimits.OutputClassCount}, Softmax).");
            }
        }
    }

    internal sealed class ModelExecutionValidationException : ArgumentException
    {
        internal int? LayerIndex { get; }

        internal ValidationIssueKind Kind { get; }

        internal ModelExecutionValidationException(string message, int? layerIndex = null,
            ValidationIssueKind kind = ValidationIssueKind.Unmapped) : base(message)
        {
            LayerIndex = layerIndex;
            Kind = kind;
        }
    }
}
