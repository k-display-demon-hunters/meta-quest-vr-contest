using System.Text;

namespace FeatureMapCorridor
{
    internal static class LessonContent
    {
        internal const string FilterHint = "필터를 눌러 펼치세요";
        internal const string OutputDone = "판정을 마쳤어요. 점수가 어떻게 만들어졌는지 더 살펴볼 수 있어요.";
        internal const string ViewScore = "점수 계산 보기";
        internal const string BackToVerdict = "판정 결과로";
        internal const string AdditionalFiltersChoice = "피처맵 3-1과 3-2를 비교해 보세요.\n필터 3-3과 3-4도 체험해 볼까요?";
        internal const string AdditionalFiltersBody = "필터 3-3과 3-4를 눌러 피처맵을 만들어 보세요.";
        internal const string TryAdditionalFilters = "추가 체험";
        internal const string GoToVerdict = "판정으로";
        internal const string PoolingSource = "Pooling 결과 두 장이 입력이에요.";
        internal const string Filter = "필터";
        internal const string OutputLabel = "출력 · 100%를 점수대로 나눠 담아요";
        internal const string FullTank = "100%";
        internal const string Score = "점수";
        internal const string Previous = "◀ 이전";
        internal const string Next = "다음 ▶";
        internal const string Understood = "알겠어요";
        internal const string Complete = "체험 완료";
        private static readonly string[] RoundNames =
        {
            "첫 번째 Conv2D", "MaxPooling", "두 번째 Conv2D", "숫자 판정"
        };

        internal static string Layer(int layer) => $"은닉층 {layer}";
        internal static string MapCount(int count) => $"피처맵 {count}장";
        internal static string Template(int digit) => $"숫자 {digit}의 틀";
        internal static string OutputScore(int digit) => $"숫자 {digit} 점수";
        internal static string RoundHeader(int round) => $"[{round}/4 · {RoundNames[round - 1]}]";
        internal const string InputPictureBody = "숫자 버튼으로 입력을 선택하세요.\n이미지를 누르면 R·G·B로 나뉘어요.";

        internal static string WaitingBody(int layer, int map)
        {
            string particle = layer == 2 && map == 1 ? "를" : "을";
            string prompt = $"<b>{FeatureMapLabels.Operation(layer, map)}</b>{particle} 눌러 보세요.";
            return layer switch
            {
                1 => prompt,
                2 => $"Max Pooling은 2×2마다 최댓값만 남겨요.\n{prompt}",
                _ => $"{PoolingSource}\n{prompt}"
            };
        }

        internal static string PoolingBody(LessonStep step)
        {
            return step switch
            {
                LessonStep.PoolingWindow => "강조된 영역에서 가장\n밝은 칸을 찾아보세요.",
                LessonStep.PoolingMaximum => "강한 반응이 작은 결과판으로\n옮겨져 남아요.",
                _ => "핵심 특징을 남기면서 크기와 계산량을 줄여요.\n입력과 결과를 비교해 보세요."
            };
        }

        internal static string FilterIntro(int layer, int map, int digit) => layer == 4
        ? string.Format("출력 숫자 {0}의 점수는 이 필터 하나로 정해져요.", digit)
        : string.Format("{0}는 이 필터 하나로 만들어져요.", FeatureMapLabels.MapId(layer, map));
        internal static string FilterOpen(int layer, int count, int size)
        {
            if (layer == 1)
                return "각 부분은 같은 색의\n입력 채널에 적용돼요.";
            if (layer == 3)
                return "두 채널의 Pooling 결과에\n필터를 한 부분씩 적용해요.";
            string why = $"마지막 Conv의 피처맵이 {count}장";
            return $"{why}이라서 필터도 <b>{size}×{size} 판 {count}장</b>이에요.\n<color=#8FA3B8>필터를 두 번 누르면 다시 겹쳐져요</color>";
        }

        internal static string Scan(int layer) => layer switch
        {
            1 => "필터가 RGB 입력의 같은 위치를 훑으며,\n채널별로 입력값과 가중치를 곱해 더해요.",
            4 => "판마다 맡은 피처맵 위에 통째로 겹쳐져요.",
            _ => "필터가 훑으며 찾은 특징이\n결과판에 나타나요."
        };
        internal static string Explain(int layer, int digit) => layer == 4
        ? $"숫자 {digit}의 틀과 피처맵이 잘 맞을수록 점수가 높아져요."
        : "밝은 칸은 이 필터가\n강하게 반응한 위치예요.";
        internal static string Merge(int layer, int map, int digit, int count, int winner)
        {
            if (layer != 4)
            {
                return string.Format("{0}장을 한 장으로 더하면 → <b>{1} 완성!</b>", count, FeatureMapLabels.MapId(layer, map));
            }
            return string.Format(winner == digit ? "{0}장을 모두 더한 값이 <b>숫자 {1}의 점수</b>예요. 세 숫자 중 <b>가장 높아요</b>."
            : "{0}장을 모두 더한 값이 <b>숫자 {1}의 점수</b>예요. 다른 숫자보다 <b>낮아요</b>.", count, digit);
        }

        internal static string MatchTemplates(int count) => $"피처맵 {count}장을 숫자별 틀과\n비교해 점수를 매겨요.";
        internal static string ShareLight(int winner, int percent) =>
            $"숫자 {winner}의 확률이 <b>{percent}%</b>로 가장 높아요.";
        internal static string FlyAnswer(int winner) => $"<b>AI의 판정 · 숫자 {winner}</b>";
        internal static string Recap(int layer, int map, bool mandatory)
        {
            if (layer == 4)
            {
                return OutputDone;
            }
            if (mandatory && map == 1)
            {
                return "두 피처맵을 비교해 보세요. 필터마다 밝아지는 위치가 달라요.";
            }
            return $"<b>{FeatureMapLabels.MapId(layer, map)}</b> 피처맵이 완성됐어요.";
        }

    }
}
