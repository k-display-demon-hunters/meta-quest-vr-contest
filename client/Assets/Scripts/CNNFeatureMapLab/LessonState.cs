namespace FeatureMapCorridor
{
    // Owned and updated by CNNFeatureMapLabManager. Controllers receive display data only.
    internal sealed class LessonState
    {
        internal LessonPhase Phase { get; set; } = LessonPhase.AwaitInput;
        internal System.Collections.Generic.HashSet<MapId> RevealedMaps { get; } = new System.Collections.Generic.HashSet<MapId>();
        internal int Digit { get; set; } = 2;
        internal int RequiredRoundIndex { get; set; }
        internal bool MandatoryRound { get; set; }
        internal MapId ActiveRound { get; set; }
        internal LessonStep Step { get; set; }
        internal float Progress { get; set; }
    }
}
