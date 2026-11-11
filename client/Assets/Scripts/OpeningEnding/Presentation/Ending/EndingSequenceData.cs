using System;

namespace Cinematic
{
    [Serializable]
    public sealed class EndingSequenceData
    {
        [Serializable]
        public sealed class MontageEntry
        {
            public CinematicText.ArchiveClip Clip;
            public string Year;
            public string Label;
            public int PropIndex;
            public double Duration;
        }

        public float IntroImpact = 2.8f;
        public float MontageStart = 3f;
        public MontageEntry[] Montage;
        public double RecognitionStart = 15f;
        public double ModelStart = 16.6f;
        public double ModelEnd = 18f;
        public float Duration = 20f;
    }
}
