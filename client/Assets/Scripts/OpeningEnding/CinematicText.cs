using System;
using UnityEngine;
using UnityEngine.Video;

namespace Cinematic
{
    [CreateAssetMenu(menuName = "CNN/Cinematic text and media")]
    public sealed class CinematicText : ScriptableObject
    {
        [Serializable]
        public sealed class Line
        {
            public float Start;
            public float End;
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
        public ArchiveClip PostOfficeStamp;
        public ArchiveClip PostOfficeEnvelope;
        public AudioClip OpeningCrowd, OpeningScore;
        public string OpeningLocation = "1989년, 미국의 한 우체국";
        public string OpeningResearch = "1989 · 얀 르쿤 연구팀의 손글씨 우편번호 인식 연구";
        public ArchiveClip[] Montage;
        public string[] Years = { "1998", "2012", "2015", "2018", "2020", "2022", "2024", "2026" };
        public string[] Labels = { "725", "사진 분류", "얼굴", "12가 3456", "양말 · 전선", "표지판 30", "확인 필요", "고양이" };
        public string Example = "구조 예시 · 설계실 결과 없음";
        public string Provenance = "1989 · 우체국 / 역사적 분위기를 재구성한 이미지";
        public string FactoryTitle = "CNN 공장";
        public string LoadFailure = "장면을 열지 못했습니다. 트리거를 길게 눌러 다시 시도하세요.";
        public AudioClip Type, Hit, BigHit, Rewind, Stamp, Beep, Pad;
    }

    internal static class OpeningTiming
    {
        public const float Rewind = 3.5f;
        public const float Arrival = 7f;
        public const float Zoom = 12.6f;
        public const float StampShot = 14f;
        public const float StampContact = 15.4f;
        public const float Envelope = 16.8f;
        public const float Digit = 20.8f;
        public const float Pixels = 22f;
        public const float Factory = 30.6f;
        public const float Doors = 31f;
        public const float Duration = 35f;
    }
}
