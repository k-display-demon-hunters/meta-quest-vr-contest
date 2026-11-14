using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FeatureMapCorridor
{
    internal sealed class FeatureMapSnapshot
    {
        private readonly ReadOnlyCollection<IReadOnlyList<float>> _input;
        private readonly ReadOnlyCollection<float>[][] _maps;
        private readonly float[][] _scales;
        private readonly float[][][] _templates;
        private readonly int[] _sizes;
        internal int Size(int layer) => _sizes[layer - 1];
        internal int LayerCount => _maps.Length;
        internal IReadOnlyList<float> Scores
        {
            get;
        }

        internal IReadOnlyList<float> Probabilities
        {
            get;
        }

        internal IReadOnlyList<IReadOnlyList<float>> Input => _input;
        internal int MapCount(int layer) => _maps[layer - 1].Length;
        internal IReadOnlyList<float> Values(int layer, int map) => _maps[layer - 1][map];
        internal float TemplateWeight(int digit, int map, int cell) => _templates[digit][map][cell];
        internal float Scale(int layer, int map) => _scales[layer - 1][map];
        internal FeatureMapSnapshot(int[] sizes, float[][] input, float[][][] maps, float[][] scales, float[] scores, float[] probabilities, float[][][] templates)
        {
            _sizes = sizes;
            _scales = scales;
            _templates = templates;
            var inputs = new IReadOnlyList<float>[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                inputs[i] = System.Array.AsReadOnly(input[i]);
            }
            _input = System.Array.AsReadOnly(inputs);
            _maps = new ReadOnlyCollection<float>[maps.Length][];
            for (int l = 0; l < maps.Length; l++)
            {
                _maps[l] = new ReadOnlyCollection<float>[maps[l].Length];
                for (int m = 0; m < maps[l].Length; m++)
                {
                    _maps[l][m] = System.Array.AsReadOnly(maps[l][m]);
                }
            }
            Scores = System.Array.AsReadOnly(scores);
            Probabilities = System.Array.AsReadOnly(probabilities);
        }
    }
}
