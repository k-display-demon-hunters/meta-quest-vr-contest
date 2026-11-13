using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FeatureMapCorridor
{
    internal sealed class RoundCalculationResult
    {
        private readonly ReadOnlyCollection<float>[] _values;
        private readonly ReadOnlyCollection<float>[] _templates;
        private readonly float[] _scales;
        private readonly float[][] _kernels;
        internal int Layer
        {
            get;
        }

        internal int Map
        {
            get;
        }

        internal int Digit
        {
            get;
        }

        internal int Index
        {
            get;
        }

        internal int Size
        {
            get;
        }

        internal int Count => _values.Length;
        internal bool IsOutput => Layer == 4;
        internal float Pre
        {
            get;
        }

        internal float Bias { get; }
        internal IReadOnlyList<float> Terms
        {
            get;
        }

        internal IReadOnlyList<float> Values(int source) => _values[source];
        internal IReadOnlyList<float> Template(int source) => _templates[source];
        internal float Scale(int source) => _scales[source];
        internal float Weight(int source, int cell) => _kernels[source][cell];
        internal RoundCalculationResult(int layer, int map, int digit, int index, int size, float[][] values, float[][] templates,
            float[] scales, float[] terms, float pre, float bias, float[][] kernels = null)
        {
            Layer = layer;
            Map = map;
            Digit = digit;
            Index = index;
            Size = size;
            _values = new ReadOnlyCollection<float>[values.Length];
            _templates = Array.ConvertAll(templates, template => Array.AsReadOnly(template));
            for (int i = 0;i<values.Length;i++)
            {
                _values[i] = Array.AsReadOnly(values[i]);
            }
            _scales = scales;
            _kernels = kernels;
            Terms = Array.AsReadOnly(terms);
            Pre = pre;
            Bias = bias;
        }
    }
}
