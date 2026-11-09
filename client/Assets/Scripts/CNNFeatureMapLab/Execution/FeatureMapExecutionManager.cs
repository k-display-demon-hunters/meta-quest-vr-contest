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

        internal RoundCalculationResult CalculateRound(int layer, int map, int index) => RoundCalculation.Calculate(_model, Digit, layer, map, index, _input, _forward);
        private void Recalculate()
        {
            _input = _model.Input(Digit);
            _forward = _model.Forward(Digit, _off);
            var scales = new float[_forward.Length][];
            for (int l = 0;l<_forward.Length;l++)
            {
                scales[l] = new float[_forward[l].Length];
                for (int m = 0;m<scales[l].Length;m++)
                {
                    scales[l][m] = _model.Scale(l+1, m);
                }
            }
            Snapshot = new FeatureMapSnapshot(Digit, _model.Size, _input, _forward, scales, _model.Scores(_forward), _model.Probabilities(_forward));
        }
    }
}
