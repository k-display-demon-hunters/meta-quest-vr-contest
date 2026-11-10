using UnityEngine;

namespace FeatureMapCorridor
{
    internal static class RoundCalculation
    {
        internal static RoundCalculationResult Calculate(ICnnModel model, int digit, int layer, int map, int index, float[][] input, LayerOutput[][] forward)
        {
            bool output = layer == 4;
            int size = model.LayerSize(output ? 3 : layer);
            float[][] sources = layer == 1 ? input : null;
            int count = layer == 1 ? input.Length : forward[layer-2].Length;
            var values = new float[count][];
            var templates = new float[count][];
            var kernels = new float[count][, ];
            var scales = new float[count];
            var terms = new float[count];
            float denominator = 0;
            for (int i = 0;i<count;i++)
            {
                templates[i] = output ? (float[])model.OutputTemplate(digit, i).Clone() : System.Array.Empty<float>();
                if (output)
                {
                    foreach (float value in templates[i])
                    {
                        denominator += value*value;
                    }
                }
                else
                {
                    kernels[i] = (float[, ])model.Kernel(layer, map, i).Clone();
                }
            }
            for (int i = 0;i<count;i++)
            {
                float[] source = layer == 1 ? sources[i] : forward[layer-2][i].act;
                values[i] = new float[size*size];
                if (output)
                {
                    float scale = model.Scale(3, i)*Mathf.Max(.001f, denominator);
                    for (int j = 0;j<values[i].Length;j++)
                    {
                        values[i][j] = source[j]*templates[i][j]/scale;
                    }
                    scales[i] = 1/Mathf.Max(.001f, denominator);
                }
                else
                {
                    float max = .001f;
                    for (int j = 0;j<values[i].Length;j++)
                    {
                        values[i][j] = ConvolutionMath.Contribution(source, kernels[i], size, j);
                        max = Mathf.Max(max, Mathf.Abs(values[i][j]));
                    }
                    scales[i] = max;
                    terms[i] = values[i][index];
                }
            }
            return new RoundCalculationResult(layer, map, digit, index, size, values, templates, kernels, scales, terms, output ? 0 : forward[layer-1][map].pre[index], output ? 0 : model.Bias(layer, map), !output && forward[layer-1][map].dead);
        }
    }
}
