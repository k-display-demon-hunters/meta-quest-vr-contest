using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ModelDesign
{
    public static class ModelJsonSerializer
    {
        public static bool TrySerialize(ModelWorkspace workspace, out string json,
            out IReadOnlyList<ValidationIssue> issues)
        {
            issues = ModelValidator.ValidateForTransmission(workspace);
            json = null;
            if (issues.Count != 0)
            {
                return false;
            }

            ModelDefinition model = workspace.InputModel;

            // The schema contains only fixed tokens and validated integers, no user strings.
            // Writing only the relevant fields avoids polymorphic Unity serialization metadata.
            StringBuilder builder = new StringBuilder("{\"modelType\":\"");
            builder.Append(DetermineModelType(model));
            builder.Append("\",\"input\":{\"shape\":[28,28,1]},\"layers\":[");
            bool reverse = ReferenceEquals(model.Layers[model.Layers.Count - 1], workspace.Input);
            for (int i = 1; i < model.Layers.Count; i++)
            {
                if (i > 1)
                {
                    builder.Append(',');
                }
                AppendLayer(builder, model.Layers[reverse ? model.Layers.Count - 1 - i : i]);
            }
            builder.Append("]}");
            json = builder.ToString();
            return true;
        }

        public static string Serialize(ModelWorkspace workspace)
        {
            if (!TrySerialize(workspace, out string json, out IReadOnlyList<ValidationIssue> issues))
            {
                throw new InvalidOperationException(issues[0].Path + ": " + issues[0].Message);
            }
            return json;
        }

        private static string DetermineModelType(ModelDefinition model)
        {
            foreach (LayerDefinition layer in model.Layers)
            {
                if (layer.Type == LayerType.Conv2D || layer.Type == LayerType.MaxPooling2D)
                {
                    return "cnn";
                }
            }
            return "mlp";
        }

        private static void AppendLayer(StringBuilder builder, LayerDefinition layer)
        {
            switch (layer.Type)
            {
                case LayerType.Conv2D:
                    builder.Append("{\"type\":\"conv2d\",\"filters\":");
                    AppendInteger(builder, layer.Filters);
                    builder.Append(",\"kernelSize\":");
                    AppendSize(builder, layer.Height, layer.Width);
                    AppendActivation(builder, layer.Activation);
                    break;
                case LayerType.MaxPooling2D:
                    builder.Append("{\"type\":\"max_pooling2d\",\"poolSize\":");
                    AppendSize(builder, layer.Height, layer.Width);
                    break;
                case LayerType.Flatten:
                    builder.Append("{\"type\":\"flatten\"");
                    break;
                case LayerType.Output:
                case LayerType.Dense:
                    builder.Append("{\"type\":\"dense\",\"units\":");
                    AppendInteger(builder, layer.Units);
                    AppendActivation(builder, layer.Activation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layer));
            }
            builder.Append('}');
        }

        private static void AppendSize(StringBuilder builder, int height, int width)
        {
            builder.Append('[');
            AppendInteger(builder, height);
            builder.Append(',');
            AppendInteger(builder, width);
            builder.Append(']');
        }

        private static void AppendInteger(StringBuilder builder, int value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendActivation(StringBuilder builder, ActivationType activation)
        {
            builder.Append(",\"activation\":\"");
            builder.Append(activation.ToString().ToLowerInvariant());
            builder.Append('"');
        }
    }
}
