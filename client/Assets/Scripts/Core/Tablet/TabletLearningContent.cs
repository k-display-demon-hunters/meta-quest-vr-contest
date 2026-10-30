using System;
using System.Collections.Generic;
namespace Tablet
{
    internal static class TabletLearningContent
    {
        public static IReadOnlyList<string> StageNames { get; } = Array.AsReadOnly(new[] { "픽셀", "합성곱", "특징 맵", "모델 제작" });
    }
}
