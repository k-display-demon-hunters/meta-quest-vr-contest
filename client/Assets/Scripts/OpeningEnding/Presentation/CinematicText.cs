using System;
using UnityEngine;
using UnityEngine.Video;

namespace Cinematic
{
    [CreateAssetMenu(menuName = "CNN/Cinematic text and media")]
    public sealed class CinematicText : ScriptableObject
    {
        public enum CaptionAnchor
        {
            Start,
            PostOffice,
            Envelope,
            Pixels,
            Doors,
            Recognition,
            Model,
            Closing
        }

        [Serializable]
        public sealed class Line
        {
            public CaptionAnchor Anchor;
            public double Start;
            public double End;
            [TextArea] public string Text;
            public AudioClip Voice;
        }

        [Serializable]
        public sealed class ArchiveClip
        {
            public string Id;
            public VideoClip Video;
            public Texture2D Still;
            public Rect UV = new Rect(0, 0, 1, 1);
            public double StartSeconds;
            public float Duration = 20;
            [TextArea] public string Source;
        }

        public Line[] Opening;
        public Line[] Ending;
        public ArchiveClip PostOffice;
        public AudioClip OpeningCrowd, OpeningScore;
        public string OpeningLocation = "1989년, 미국의 한 우체국";
        public string OpeningResearch = "1989 · 얀 르쿤 연구팀의 손글씨 우편번호 인식 연구";
        public OpeningSequenceData OpeningSchedule = new OpeningSequenceData();
        public EndingSequenceData EndingSchedule = new EndingSequenceData();
        public string Provenance = "1989 · 우체국 / 역사적 분위기를 재구성한 이미지";
        public string FactoryTitle = "CNN 공장";
        public string LoadFailure = "장면을 열지 못했습니다. 트리거를 길게 눌러 다시 시도하세요.";
        public AudioClip Type, Hit, BigHit, Rewind, Stamp, Beep;

        internal double GetCaptionOrigin(CaptionAnchor anchor)
        {
            switch (anchor)
            {
                case CaptionAnchor.PostOffice: return OpeningSchedule.ArrivalTime;
                case CaptionAnchor.Envelope: return OpeningSchedule.EnvelopeTime;
                case CaptionAnchor.Pixels: return OpeningSchedule.PixelTime;
                case CaptionAnchor.Doors: return OpeningSchedule.DoorsTime;
                default: return 0;
            }
        }
    }
}
