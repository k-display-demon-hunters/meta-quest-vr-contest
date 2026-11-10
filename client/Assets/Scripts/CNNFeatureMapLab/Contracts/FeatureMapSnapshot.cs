using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FeatureMapCorridor
{
    internal sealed class FeatureMapSnapshot
    {
        private readonly ReadOnlyCollection<IReadOnlyList<float>> _input;
        private readonly ReadOnlyCollection<float>[][] _maps;
        private readonly bool[][] _off;
        private readonly float[][] _scales;
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
        internal float Scale(int layer, int map) => _scales[layer - 1][map];
        internal bool IsOff(int layer, int map) => _off[layer - 1][map];
        internal FeatureMapSnapshot(int[] sizes, float[][] input, LayerOutput[][] forward, float[][] scales, float[] scores, float[] probabilities)
        {
            _sizes = sizes;
            _scales = scales;
            var inputs = new IReadOnlyList<float>[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                inputs[i] = System.Array.AsReadOnly((float[])input[i].Clone());
            }
            _input = System.Array.AsReadOnly(inputs);
            _maps = new ReadOnlyCollection<float>[forward.Length][];
            _off = new bool[forward.Length][];
            for (int l = 0; l < forward.Length; l++)
            {
                _maps[l] = new ReadOnlyCollection<float>[forward[l].Length];
                _off[l] = new bool[forward[l].Length];
                for (int m = 0; m < forward[l].Length; m++)
                {
                    _maps[l][m] = System.Array.AsReadOnly((float[])forward[l][m].act.Clone());
                    _off[l][m] = forward[l][m].dead;
                }
            }
            Scores = System.Array.AsReadOnly(scores);
            Probabilities = System.Array.AsReadOnly(probabilities);
        }
    }
}
