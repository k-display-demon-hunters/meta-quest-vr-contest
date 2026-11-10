using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FeatureMapCorridor
{
    public sealed class FeatureMapExecutionManager : MonoBehaviour
    {
        [FormerlySerializedAs("_modelPrefab")]
        [SerializeField] private MonoBehaviour _modelInstance;
        private ICnnModel _model;
        private readonly HashSet<string> _off = new HashSet<string>();
        private float[][] _input;
        private LayerOutput[][] _forward;
        internal int Digit
        {
            get;
            private set;
        }
        = 2;
        internal FeatureMapSnapshot Snapshot
        {
            get;
            private set;
        }

        internal void Initialize()
        {
            _model = (ICnnModel)_modelInstance;
            Recalculate();
        }

        internal void SelectDigit(int digit)
        {
            Digit = digit;
            Recalculate();
        }

        internal bool Toggle(MapId map)
        {
            bool enabled = _off.Remove(map.ModelKey);
            if (!enabled)
            {
                _off.Add(map.ModelKey);
            }
            Recalculate();
            return enabled;
        }

        internal void SetEnabled(MapId map, bool enabled)
        {
            if (enabled)
            {
                _off.Remove(map.ModelKey);
            }
            else
            {
                _off.Add(map.ModelKey);
            }
            Recalculate();
        }

        internal RoundCalculationResult CalculateRound(int layer, int map)
        {
            int index = layer == 4 ? 0 : StrongestCell(_forward[layer - 1][map].pre);
            return RoundCalculation.Calculate(_model, Digit, layer, map, index, _input, _forward);
        }

        internal PoolingCalculationResult CalculatePooling(int channel)
        {
            float[] source = _forward[0][channel].act;
            int inputSize = _model.LayerSize(1);
            int outputIndex = StrongestCell(_forward[1][channel].pre);
            return PoolingMath.ReadWindow(source, inputSize, outputIndex);
        }

        private static int StrongestCell(float[] values)
        {
            int best = 0;
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > values[best])
                {
                    best = i;
                }
            }
            return best;
        }

        private void Recalculate()
        {
            _input = _model.Input(Digit);
            _forward = _model.Forward(Digit, _off);
            var sizes = new int[_forward.Length];
            var scales = new float[_forward.Length][];
            for (int l = 0;l<_forward.Length;l++)
            {
                sizes[l] = _model.LayerSize(l + 1);
                scales[l] = new float[_forward[l].Length];
                for (int m = 0;m<scales[l].Length;m++)
                {
                    scales[l][m] = _model.Scale(l+1, m);
                }
            }
            Snapshot = new FeatureMapSnapshot(sizes, _input, _forward, scales, _model.Scores(_forward), _model.Probabilities(_forward));
        }
    }
}
