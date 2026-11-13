using UnityEngine;

namespace FeatureMapCorridor
{
    internal enum LessonPhase
    {
        AwaitInput, AwaitRound, Round, Recap, Complete
    }
    internal struct LessonContext
    {
        internal LessonPhase Phase;
        internal int Digit, Layer, Map, Winner, WinnerPercent;
        internal LessonStep Step;
        internal bool CanGoBack, Mandatory, WaitingForFilter, FilterPending, IsLastStep, ShowingOutputDetails;
        internal bool OfferAdditionalFilters, ExploringAdditionalFilters;
        internal Vector3 HintPosition;
        internal Vector3 LessonPanelPosition;
        internal RoundCalculationResult Calculation;
        internal PoolingCalculationResult Pooling;
    }
    internal readonly struct RecapInfo
    {
        internal Vector3 Position
        {
            get;
        }

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

        internal int SourceCount
        {
            get;
        }

        internal RecapInfo(Vector3 position, int layer, int map, int digit, int sourceCount)
        {
            Position = position;
            Layer = layer;
            Map = map;
            Digit = digit;
            SourceCount = sourceCount;
        }
    }
}
