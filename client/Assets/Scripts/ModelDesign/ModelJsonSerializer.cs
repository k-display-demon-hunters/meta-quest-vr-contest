using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MetaQuest.ModelDesign
{
    public static class ModelJsonSerializer
    {
        public static bool TrySerialize(ModelDefinition model, out string json,
            out IReadOnlyList<ValidationIssue> issues)
        {
            issues = ModelValidator.Validate(model);
            json = null;
            if (issues.Count != 0)
            {
                return false;
            }

            // The schema contains only fixed tokens and validated integers, no user strings.
            // Writing only the relevant fields avoids polymorphic Unity serialization metadata.
            StringBuilder builder = new StringBuilder("{\"schemaVersion\":1,\"input\":{\"shape\":[28,28,1]},\"layers\":[");
            for (int i = 0; i < model.Layers.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }
                AppendLayer(builder, model.Layers[i]);
            }
            builder.Append("]}");
            json = builder.ToString();
            return true;
        }

        public static string Serialize(ModelDefinition model)
        {
            if (!TrySerialize(model, out string json, out IReadOnlyList<ValidationIssue> issues))
            {
                throw new InvalidOperationException(issues[0].Path + ": " + issues[0].Message);
            }
            return json;
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
