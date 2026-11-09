using UnityEngine;

namespace FeatureMapCorridor
{
    internal enum LessonPhase
    {
        Start, Tour, AwaitRound, Round, Recap, AwaitOff, OffResult, AwaitOn, OnResult, Complete
    }
    internal struct LessonContext
    {
        internal LessonPhase Phase;
        internal int Digit, TourStage, Layer, Map, Stage, MapCount, Winner, WinnerPercent;
        internal bool FirstTour, Mandatory, WaitingForFilter, FilterPending;
        internal Vector3 HintPosition;
        internal MapId Switch;
        internal RoundCalculationResult Calculation;
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
