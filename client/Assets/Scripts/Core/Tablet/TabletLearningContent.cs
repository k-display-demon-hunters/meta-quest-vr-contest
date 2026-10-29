using System;
using System.Collections.Generic;

namespace Tablet
{
    internal static class TabletLearningContent
    {
        public static IReadOnlyList<string> PhaseNames { get; } = Array.AsReadOnly(new[]
        {
            "미션 안내", "픽셀·합성곱", "피처맵", "모델 제작", "복구 마무리"
        });

        public static IReadOnlyList<string> StageNames { get; } = Array.AsReadOnly(new[]
        {
            "미션 안내", "Pixel Lab", "Feature Map Lab", "Model Design Lab", "복구 마무리"
        });

        public static IReadOnlyList<string> TaskLabels { get; } = Array.AsReadOnly(new[]
        {
            "양피지 수집 0/5", "방 체험 완료 0/3", "미션 안내 읽기"
        });
    }
}
