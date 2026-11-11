using System.Text;

namespace FeatureMapCorridor
{
    internal static class LessonContent
    {
        internal const string FilterHint = "필터를 눌러 펼쳐 보세요";
        internal const string OutputDone = "판정을 마쳤어요. 점수가 어떻게 만들어졌는지 더 살펴볼 수 있어요.";
        internal const string ViewScore = "점수 계산 보기";
        internal const string BackToVerdict = "판정 결과로";
        internal const string InputLabel = "입력 · 16×16";
        internal const string Filter = "필터";
        internal const string OutputLabel = "출력 · 100%를 점수대로 나눠 담아요";
        internal const string FullTank = "100%";
        internal const string Score = "점수";
        internal const string Previous = "◀ 이전";
        internal const string Next = "다음 ▶";
        internal const string Understood = "알겠어요";
        internal const string Confirm = "확인";
        internal const string Complete = "체험 완료";
        internal const string Arrow = "안내";
        private static readonly string[] RoundNames =
        {
            "첫 번째 Conv2D", "MaxPooling", "두 번째 Conv2D", "숫자 판정"
        };

        internal static string Layer(int layer) => $"은닉층 {layer}";
        internal static string MapCount(int count) => $"피처맵 {count}장";
        internal static string Template(int digit) => $"숫자 {digit}의 틀";
        internal static string OutputScore(int digit) => $"숫자 {digit} 점수";
        internal static string RoundHeader(int round) => $"[{round}/4 · {RoundNames[round - 1]}]";
        internal const string InputPictureBody = "입력 이미지를 눌러 빨강, 초록, 파랑 채널로 나눠 보세요.";
        internal const string InputPictureHint = "입력 이미지 누르기";

        internal static string WaitingHint(int layer, int map) =>
            $"{FeatureMapLabels.Operation(layer, map)} 장치를 눌러 보세요";

        internal static string WaitingBody(int layer, int map) => layer == 4
            ? "이 이미지에서 찾은 특징으로 어떤 숫자인지 알아볼까요? 가운데 판정 시작 버튼을 누르세요."
            : layer == 2
                ? "<b>Pooling은 중요한 특징을 보존하면서 크기를 줄입니다.</b>\nPooling 장치를 눌러 강한 반응이 어떻게 남는지 살펴보세요."
                : $"<b>필터는 특징을 추출합니다.</b>\n{FeatureMapLabels.Operation(layer, map)} 장치를 눌러 피처맵을 만들어 보세요.";

        internal static string PoolingBody(LessonStep step, int channel, PoolingCalculationResult data)
        {
            return step switch
            {
                LessonStep.PoolingWindow => "Pooling은 중요한 특징을 보존하면서 크기를 줄입니다. 강조된 영역의 밝은 반응을 살펴보세요.",
                LessonStep.PoolingMaximum => "강한 반응이 작은 결과판으로 옮겨져 남아요.",
                _ => "크기가 줄어도 중요한 특징은 남아 있어요. 앞의 피처맵과 비교해 보세요."
            };
        }

        internal static string FilterIntro(int layer, int map, int digit) => layer == 4
        ? string.Format("출력 숫자 {0}의 점수는 이 필터 하나로 정해져요.", digit)
        : string.Format("{0}는 이 필터 하나로 만들어져요.", FeatureMapLabels.MapId(layer, map));
        internal static string FilterOpen(int layer, int count, int size)
        {
            if (layer == 1)
                return "필터는 특징을 추출합니다. 색깔별 이미지에서 어떤 부분이 드러나는지 살펴보세요.";
            if (layer == 3)
                return "Pooling을 거친 두 피처맵이 입력이에요. 앞에서 찾은 특징을 조합해 새로운 특징을 추출해요.";
            string why = $"마지막 Conv의 피처맵이 {count}장";
            return $"{why}이라서 필터도 <b>{size}×{size} 판 {count}장</b>이에요.\n<color=#8FA3B8>필터를 두 번 누르면 다시 겹쳐져요</color>";
        }

        internal static string Scan(int layer) => layer == 4
            ? "판마다 맡은 피처맵 위에 통째로 겹쳐져요."
            : "필터가 지나가며 찾아낸 특징이 결과판에 드러나요.";
        internal static string Explain(int layer, int digit) => layer == 4
        ? string.Format("<b>출력 {0}</b>는 입력이 숫자 {0}인지 판단해야 해요. 그래서 필터의 판마다 <b>숫자 {0}일 때 그 피처맵이 밝아지는 곳</b>이 밝게 그려져 있어요. 피처맵이 그 모양과 많이 겹칠수록 점수가 커져요.", digit) : "필터마다 찾아내는 특징이 달라요. 같은 이미지에서 어디가 밝게 드러났는지 비교해 보세요.";
        internal static string Merge(int layer, int map, int digit, int count, int winner)
        {
            if (layer != 4)
            {
                return string.Format("{0}장을 한 장으로 더하면 → <b>{1} 완성!</b>", count, FeatureMapLabels.MapId(layer, map));
            }
            return string.Format(winner == digit ? "{0}장을 모두 더한 값이 <b>숫자 {1}의 점수</b>예요. 세 숫자 중 <b>가장 높아요</b>."
            : "{0}장을 모두 더한 값이 <b>숫자 {1}의 점수</b>예요. 다른 숫자보다 <b>낮아요</b>.", count, digit);
        }

        internal static string MatchTemplates(int count) => $"마지막 Conv의 피처맵 {count}장을 <b>한 덩어리로 쌓았어요</b>. 통 위에는 숫자마다 \"이 숫자라면 이렇게 생겨야 해\" 하는 <b>틀</b>이 있어요.\n덩어리를 틀마다 겹쳐 보고, <b>잘 맞을수록 틀이 빛나고 점수(빛 기둥)가 높아져요</b>.";
        internal static string ShareLight(int winner, int percent) => string.Format("이제 위의 <b>빛 탱크에 담긴 100%</b>를 점수에 따라 세 통에 나눠 담아요.\n점수가 조금만 높아도 <b>훨씬 많이</b> 가져가요. 숫자 {0} 통이 <b>{1}%</b>까지 차올라요.", winner, percent);
        internal static string FlyAnswer(int winner) => string.Format("가장 많이 차오른 통이 <b>AI의 답</b>이에요.\n<b>AI의 판정 · 숫자 {0}</b>", winner);
        internal static string Recap(int layer, int map, bool mandatory)
        {
            if (layer == 4)
            {
                return OutputDone;
            }
            if (mandatory && map == 1)
            {
                return "두 피처맵을 비교해 보세요. 같은 재료에 서로 다른 필터를 적용하면 밝아지는 위치가 달라져요.";
            }
            return $"<b>{FeatureMapLabels.MapId(layer, map)}</b> 피처맵이 완성됐어요.";
        }

    }
}
