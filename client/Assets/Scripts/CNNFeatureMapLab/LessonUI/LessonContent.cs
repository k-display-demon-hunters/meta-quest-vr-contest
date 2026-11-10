using System.Text;

namespace FeatureMapCorridor
{
    internal static class LessonContent
    {
        internal const string FilterHint = "필터를 눌러 펼쳐 보세요";
        internal const string OutputDone = "출력까지 모두 계산했어요!";
        internal const string DoorOpenHint = "문이 열렸어요";
        internal const string CompleteBody = "모든 체험을 마쳤어요. 복도 끝의 문이 열렸어요.";
        internal const string InputLabel = "입력 · 16×16";
        internal const string Filter = "필터";
        internal const string OutputLabel = "출력 · 100%를 점수대로 나눠 담아요";
        internal const string FullTank = "100%";
        internal const string Score = "점수";
        internal const string DoorClosed = "닫힘 · 체험을 마치면 열려요";
        internal const string Previous = "◀ 이전";
        internal const string Next = "다음 ▶";
        internal const string Understood = "알겠어요";
        internal const string Confirm = "확인";
        internal const string Complete = "체험 완료";
        internal const string CompleteTitle = "문이 열렸어요";
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
        internal static string WaitingHint(int layer, int map, int digit) => layer == 4
            ? $"숫자 {digit} 통을 눌러 보세요"
            : $"{FeatureMapLabels.MapId(layer, map)} 판을 눌러 보세요";

        internal static string WaitingBody(int layer, int map, int digit) => layer == 4
            ? $"출력의 숫자 {digit} 통을 눌러 보세요."
            : $"<b>{RoundNames[layer - 1]}</b>\n{FeatureMapLabels.MapId(layer, map)} 판을 눌러 필터를 살펴보세요.";

        internal static string PoolingBody(int stage, int channel, PoolingCalculationResult data)
        {
            switch (stage)
            {
                case 0:
                    return $"채널 {channel + 1}의 16×16 피처맵에서 대표 2×2 영역을 표시했어요. 양쪽 판을 눌러 채널을 바꿀 수 있어요.";
                case 1:
                    return $"네 칸의 값이에요.\n{data.TopLeft:0.###}   {data.TopRight:0.###}\n{data.BottomLeft:0.###}   {data.BottomRight:0.###}";
                case 2:
                    return $"가장 큰 값 <b>{data.Maximum:0.###}</b>을 결과판의 한 칸에 담았어요.";
                default:
                    return "같은 계산을 두 채널에 적용했어요. 격자는 <b>16×16 → 8×8</b>로 줄고 채널은 <b>두 장</b>이에요.";
            }
        }

        internal static string FilterIntro(int layer, int map, int digit) => layer == 4
        ? string.Format("출력 숫자 {0}의 점수는 이 필터 하나로 정해져요.", digit)
        : string.Format("{0}는 이 필터 하나로 만들어져요.", FeatureMapLabels.MapId(layer, map));
        internal static string FilterOpen(int layer, int count, int size)
        {
            string why = layer == 1 ? "입력이 R·G·B 3장" : layer == 4 ? $"마지막 Conv의 피처맵이 {count}장"
            : $"Pooling 결과가 {count}장";
            return string.Format("{0}이라서 필터도 <b>{1} 판 {2}장</b>이에요.\n<color=#8FA3B8>필터를 두 번 누르면 다시 겹쳐져요</color>", why, layer == 4 ? $"{size}×{size}" : "3×3", count);
        }

        internal static string Scan(int layer) => layer == 1 ? "R을 훑는 판, G를 훑는 판, B를 훑는 판이 각자 맡은 이미지를 처음부터 끝까지 훑어요."
        : layer == 4 ? "판마다 맡은 피처맵 위에 통째로 겹쳐져요." : "판마다 맡은 피처맵을 처음부터 끝까지 훑어요.";
        internal static string Explain(int layer, int digit) => layer == 4
        ? string.Format("<b>출력 {0}</b>는 입력이 숫자 {0}인지 판단해야 해요. 그래서 필터의 판마다 <b>숫자 {0}일 때 그 피처맵이 밝아지는 곳</b>이 밝게 그려져 있어요. 피처맵이 그 모양과 많이 겹칠수록 점수가 커져요.", digit) : "필터마다 가중치가 달라요. 같은 입력을 사용해도 위치마다 계산된 값이 어떻게 달라지는지 피처맵에서 살펴보세요.";
        internal static string Compare(int layer, System.Collections.Generic.IReadOnlyList<float> terms, float pre, float bias)
        {
            var message = new StringBuilder($"{terms.Count}장의 이미지의 <color=#FFE14A><b>노란 칸</b></color>만 볼게요.\n");
            for (int i = 0; i < terms.Count; i++)
            {
                float term = terms[i];
                string verdict = term == 0 ? "<color=#8FA3B8>0</color>"
                : term > 0 ? "<color=#62E4D9><b>밝게 (+)</b></color>" : "<color=#FF7A6B><b>어둡게 (−)</b></color>";
                message.Append(FeatureMapLabels.Marks[i]).Append(' ').Append(layer == 1 ? FeatureMapLabels.Channels[i] : $"{layer - 1}-{i + 1}")
                .Append(" 결과: ").AppendLine(verdict);
            }
            message.AppendLine($"편향 {bias:0.###}을 포함한 합은 {pre:0.###}이에요.");
            return message.Append(pre > 0 ? "ReLU를 지나 양수는 그대로 남아요." : "ReLU를 지나면 결과가 <b>0</b>이에요.").ToString();
        }

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
        internal static string FlyAnswer(int winner) => string.Format("가장 많이 차오른 통이 <b>AI의 답</b>이에요.\n입력 그림이 날아가 앉은 곳 → <b>이 그림은 숫자 {0}!</b>", winner);
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
