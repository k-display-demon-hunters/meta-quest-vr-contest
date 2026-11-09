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
        public ArchiveClip[] Montage;
        public string[] Years = { "1998", "2012", "2015", "2018", "2020", "2022", "2024", "2026" };
        public string[] Labels = { "725", "사진 분류", "얼굴", "12가 3456", "양말 · 전선", "표지판 30", "확인 필요", "고양이" };
        public string Skip = "트리거를 길게 눌러 건너뛰기";
        public string Credits = "CNN 공장\n차세대디스플레이 경진대회";
        public string Replay = "트리거를 길게 눌러 다시 시작";
        public string Example = "구조 예시 · 설계실 결과 없음";
        public string Provenance = "1989 · 우체국 / 역사적 분위기를 재구성한 이미지";
        public string PostSign = "우 편 · 소 포";
        public string Calendar = "1989\n11월";
        public string FactoryTitle = "CNN 공장";
        public string LoadFailure = "장면을 열지 못했습니다. 트리거를 길게 눌러 다시 시도하세요.";
        public AudioClip Type, Hit, BigHit, Rewind, Ring, Stamp, Machine, Printer, Crowd, Footsteps, Radio, Clerk, CRT, Beep, Pad;
        [TextArea] public string AudioCredits;
    }
}
