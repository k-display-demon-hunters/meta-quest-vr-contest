using System;
using UnityEngine;

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
            public Rect Focus;
            public double Duration;
            public bool ShowUpscaleComparison;
        }

        public MontageEntry[] Montage;
        public double IntroDuration = 3.2;
        public double OutroDuration = 7.6;
        public double QuietDuration = 1.5;
        public CinematicText.Line Intro;
        public CinematicText.Line Outro;
    }
}
