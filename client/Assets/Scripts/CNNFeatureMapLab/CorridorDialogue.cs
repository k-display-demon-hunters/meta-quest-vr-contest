using System;
using System.Text;

namespace FeatureMapCorridor
{
    internal static class CorridorDialogue
    {
        internal const string StartHint = "입력 그림을 눌러 보세요";
        internal const string StartBody = "입력 그림을 눌러 보세요.";
        internal const string SwitchHint = "스위치를 눌러 꺼 보세요";
        internal const string SwitchBody = "이번엔 피처맵 하나를 꺼 볼게요. 뒤쪽 층과 출력이 어떻게 바뀔까요?";
        internal const string TourHeader = "[흰 테두리]";
        internal const string TourIntro = "입력 그림은 <b>R·G·B 3장</b>이 겹친 거예요. <color=#8FA3B8>(두 번 누르면 다시 모여요)</color>\n";
        internal const string FilterHint = "필터를 눌러 펼쳐 보세요";
        internal const string OutputDone = "출력까지 모두 계산했어요!";
        internal const string RestoreSwitchHint = "다시 켜 보세요";
        internal const string RestoreSwitchBody = "스위치를 다시 켜 보세요.";
        internal const string RestoredSwitch = "다시 켜니 원래대로 돌아왔어요.";
        internal const string DoorOpenHint = "문이 열렸어요";
        internal const string CompleteBody = "모든 체험을 마쳤어요. 복도 끝의 문이 열렸어요.";
        internal const string InputLabel = "입력 · 16×16";
        internal const string InputR = "입력 R";
        internal const string InputG = "입력 G";
        internal const string InputB = "입력 B";
        internal const string SourceRgb = "← 입력 R·G·B로 만들어짐";
        internal const string SourceFirst = "← 피처맵 1-1·1-2로 만들어짐";
        internal const string SourceSecond = "← 피처맵 2-1·2-2·2-3으로 만들어짐";
        internal const string On = "켜짐";
        internal const string Off = "꺼짐";
        internal const string Filter = "필터";
        internal const string OutputLabel = "출력 · 100%를 점수대로 나눠 담아요";
        internal const string FullTank = "100%";
        internal const string Score = "점수";
        internal const string DoorClosed = "닫힘 · 체험을 마치면 열려요";
        internal const string DoorOpen = "열림 · 다음 방으로";
        internal const string Previous = "◀ 이전";
        internal const string Next = "다음 ▶";
        internal const string Understood = "알겠어요";
        internal const string Confirm = "확인";
        internal const string Complete = "체험 완료";
        internal const string CompleteTitle = "문이 열렸어요";
        internal const string Arrow = "안내";
        internal static readonly string[] Channels = { "R", "G", "B" };
        internal static readonly string[] InputChannels = { InputR, InputG, InputB };
        internal static readonly string[] Marks = { "①", "②", "③", "④", "⑤" };
        internal static readonly string[] Sources = { SourceRgb, SourceFirst, SourceSecond };
        internal static readonly string[] RoundNames = { "입력 → 은닉층 1", "은닉층 1 → 은닉층 2", "은닉층 2 → 은닉층 3", "은닉층 3 → 출력" };
        private static readonly string[] Materials = { "입력 R·G·B", "피처맵 1-1·1-2", "피처맵 2-1·2-2·2-3" };
        private static readonly string[][] Names = {
            new[] { "숫자 칸 찾기", "배경 칸 찾기" },
            new[] { "세로 선 찾기", "가로 선 찾기", "숫자 덩어리" },
            new[] { "꺾인 곳 찾기", "긴 가로 선", "긴 세로 선", "선 끝 찾기", "사선 찾기" }
        };
        internal static readonly string[] Tour = {
            "<b>흰 테두리</b>는 지금 누른 입력 칸이에요. 이 칸 하나가 뒤쪽 층 어디까지 영향을 주는지 볼게요.",
            "<color=#FFB24A><b>주황 창</b></color>(3×3 필터)이 입력을 훑어요. 창이 한 번 멈출 때마다 은닉층 1의 칸이 하나씩 만들어져요.\n그런데 누른 칸이 창 안에 들어오는 건 <b>9번</b>뿐이에요. 그 9번으로 만들어진 칸들이 <b>흰 3×3</b>이에요.",
            "은닉층 2도 똑같아요. <color=#FFB24A><b>주황 창</b></color>이 은닉층 1을 훑다가 <b>흰 3×3에 한 칸이라도 걸치면</b>, 그 결과 칸도 누른 칸의 영향을 받아요.\n가장자리에 살짝 걸치는 위치까지 세면 → <b>5×5</b>.",
            "한 번 더 반복하면 → <b>7×7</b>. 층을 지날 때마다 창이 가장자리에 걸치는 만큼 <b>양옆으로 한 칸씩</b> 넓어져요.\n그래서 깊은 층의 한 칸은 입력의 넓은 범위를 보고, 꺾인 곳·긴 선 같은 <b>큰 모양</b>을 찾을 수 있어요."
        };
        private static readonly string[][] Explanations = {
            new[] {
                "<b>피처맵 1-1 : 숫자 칸 찾기</b>는 숫자가 그려진 <b>주황 칸만 밝게</b> 만들어야 해요. 주황은 R이 강하고 B가 약해요. 그래서 필터가 이렇게 짜여 있어요.\n① R을 훑는 판: 모두 + → R이 강한 주황 칸을 밝게\n② G를 훑는 판: 거의 0 → 주황·청록 모두 G가 비슷해서 구분에 도움이 안 돼요\n③ B를 훑는 판: 모두 − → B가 강한 청록 배경을 어둡게",
                "<b>피처맵 1-2 : 배경 칸 찾기</b>는 <b>청록 배경만 밝게</b> 만들어야 해요. 청록은 G·B가 강하고 R이 약해요. 그래서 1-1과 반대로 짜여 있어요.\n① R을 훑는 판: 모두 − → 주황 숫자를 어둡게\n② G를 훑는 판, ③ B를 훑는 판: + → 청록 배경을 밝게"
            },
            new[] {
                "<b>피처맵 2-1 : 세로 선 찾기</b>는 <b>가운데는 숫자, 양옆은 배경</b>인 세로 모양을 찾아야 해요. 그래서 필터도 그 모양으로 짜여 있어요.\n① 피처맵 1-1(숫자 칸)을 훑는 판: 가운데 세로줄 +, 양옆 − → 숫자 칸이 세로로 가늘게 있으면 밝게\n② 피처맵 1-2(배경 칸)를 훑는 판: 반대 부호 → 가운데가 배경이면 깎기",
                "<b>피처맵 2-2 : 가로 선 찾기</b>는 <b>가운데는 숫자, 위아래는 배경</b>인 가로 모양을 찾아야 해요. 그래서 필터도 그 모양으로 짜여 있어요.\n① 피처맵 1-1(숫자 칸)을 훑는 판: 가운데 가로줄 +, 위아래 − → 숫자 칸이 가로로 가늘게 있으면 밝게\n② 피처맵 1-2(배경 칸)를 훑는 판: 반대 부호 → 가운데가 배경이면 깎기",
                "<b>피처맵 2-3 : 숫자 덩어리</b>는 <b>숫자가 있는 영역 전체</b>를 모아야 해요. 그래서 필터가 이렇게 짜여 있어요.\n① 피처맵 1-1(숫자 칸)을 훑는 판: 모두 + → 숫자 칸을 더하고\n② 피처맵 1-2(배경 칸)를 훑는 판: 모두 − → 배경을 빼요"
            },
            new[] {
                "<b>피처맵 3-1 : 꺾인 곳 찾기</b>는 <b>세로 선과 가로 선이 만나는 곳</b>을 찾아야 해요. 그래서 둘 다 있어야만 켜지게 짜여 있어요.\n① 피처맵 2-1(세로 선)을 훑는 판: +\n② 피처맵 2-2(가로 선)를 훑는 판: +\n③ 피처맵 2-3을 훑는 판: 모두 0 (필요 없어요)\n마지막에 일정량을 빼서, 선이 하나만 있는 곳은 꺼지고 둘 다 있는 곳만 남아요.",
                "<b>피처맵 3-2 : 긴 가로 선</b>은 <b>가로 선이 옆으로 길게 이어진 곳</b>을 찾아야 해요. 그래서 필터가 이렇게 짜여 있어요.\n② 피처맵 2-2(가로 선)를 훑는 판: 가운데 가로줄만 + → 옆으로 이어질수록 밝게\n① 피처맵 2-1(세로 선)을 훑는 판: − → 세로 선은 깎기\n③ 피처맵 2-3을 훑는 판: 모두 0",
                "<b>피처맵 3-3 : 긴 세로 선</b>은 <b>세로 선이 위아래로 길게 이어진 곳</b>을 찾아야 해요. 그래서 필터가 이렇게 짜여 있어요.\n① 피처맵 2-1(세로 선)을 훑는 판: 가운데 세로줄만 + → 위아래로 이어질수록 밝게\n② 피처맵 2-2(가로 선)를 훑는 판: − → 가로 선은 깎기\n③ 피처맵 2-3을 훑는 판: 모두 0",
                "<b>피처맵 3-4 : 선 끝 찾기</b>는 <b>선이 끝나서 주변보다 튀어나온 곳</b>을 찾아야 해요. 그래서 필터가 이렇게 짜여 있어요.\n③ 피처맵 2-3(숫자 덩어리)을 훑는 판: 가운데 +, 주변 − → 주변이 비어 있을수록 밝게\n①·② 판: 모두 0",
                "<b>피처맵 3-5 : 사선 찾기</b>는 <b>비스듬한 선</b>을 찾아야 해요. 그래서 필터가 이렇게 짜여 있어요.\n③ 피처맵 2-3(숫자 덩어리)을 훑는 판: 비스듬한 칸만 + → 사선 방향으로 이어지면 밝게\n①·② 판: 모두 0"
            }
        };

        internal static string MapId(int layer, int map) => $"피처맵 {layer}-{map + 1}";
        internal static string MapName(int layer, int map) => $"{MapId(layer, map)} : {Names[layer - 1][map]}";
        internal static string Layer(int layer) => $"은닉층 {layer}";
        internal static string MapCount(int count) => $"피처맵 {count}장";
        internal static string Template(int digit) => $"숫자 {digit}의 틀";
        internal static string Answer(int digit) => $"이 그림은 숫자 {digit}!";
        internal static string OutputScore(int digit) => $"숫자 {digit} 점수";
        internal static string FilterTitle(int layer, int count, int size) => $"필터 · {size}×{size} 판 {count}장";
        internal static string RoundHeader(int round) => $"[{round}/4 · {RoundNames[round - 1]}]";
        internal static string WaitingHint(int round, int digit) => round == 4
            ? string.Format("숫자 {0} 통을 눌러 보세요", digit)
            : string.Format("피처맵 {0}-1의 밝은 칸을 눌러 보세요", round);
        internal static string WaitingBody(int round, int digit) => round == 4
            ? string.Format("<b>4/4 · 은닉층 3 → 출력</b>\n출력의 숫자 {0} 통을 눌러 보세요.", digit)
            : string.Format("<b>{0}/4 · {1}</b>\n피처맵 {0}-1의 밝은 칸 하나를 눌러 보세요.", round, RoundNames[round - 1]);
        internal static string FilterIntro(int layer, int map, int digit) => layer == 4
            ? string.Format("출력 숫자 {0}의 점수는 이 필터 하나로 정해져요.", digit)
            : string.Format("{0}는 이 필터 하나로 만들어져요.", MapId(layer, map));

        internal static string FilterOpen(int layer, int count, int size)
        {
            string why = layer == 1 ? "입력이 R·G·B 3장" : layer == 4 ? "은닉층 3의 피처맵이 5장"
                : string.Format("은닉층 {0}의 피처맵이 {1}장", layer - 1, count);
            return string.Format("{0}이라서 필터도 <b>{1} 판 {2}장</b>이에요.\n<color=#8FA3B8>필터를 두 번 누르면 다시 겹쳐져요</color>", why, layer == 4 ? $"{size}×{size}" : "3×3", count);
        }

        internal static string Scan(int layer) => layer == 1 ? "R을 훑는 판, G를 훑는 판, B를 훑는 판이 각자 맡은 이미지를 처음부터 끝까지 훑어요."
            : layer == 4 ? "판마다 맡은 피처맵 위에 통째로 겹쳐져요." : "판마다 맡은 피처맵을 처음부터 끝까지 훑어요.";

        internal static string Explain(int layer, int map, int digit) => layer == 4
            ? string.Format("<b>출력 {0}</b>는 입력이 숫자 {0}인지 판단해야 해요. 그래서 필터의 판마다 <b>숫자 {0}일 때 그 피처맵이 밝아지는 곳</b>이 밝게 그려져 있어요. 피처맵이 그 모양과 많이 겹칠수록 점수가 커져요.", digit) : Explanations[layer - 1][map];

        internal static string Compare(int layer, float[] terms, float pre, bool dead)
        {
            float maximum = 0;
            foreach (float term in terms) { maximum = Math.Max(maximum, Math.Abs(term)); }
            var message = new StringBuilder($"{terms.Length}장의 이미지의 <color=#FFE14A><b>노란 칸</b></color>만 볼게요.\n");
            for (int i = 0; i < terms.Length; i++)
            {
                float term = terms[i];
                string verdict = term == 0 || Math.Abs(term) < maximum * .15f ? "<color=#8FA3B8>거의 0</color>"
                    : term > 0 ? "<color=#62E4D9><b>밝게 (+)</b></color>" : "<color=#FF7A6B><b>어둡게 (−)</b></color>";
                message.Append(Marks[i]).Append(' ').Append(layer == 1 ? Channels[i] : $"{layer - 1}-{i + 1}")
                    .Append(" 결과: ").AppendLine(verdict);
            }
            return message.Append(dead ? "이 피처맵은 스위치로 꺼져 있어서 결과 칸도 꺼져요." : pre > 0 ? "모두 더하면 <b>밝게 하는 쪽이 더 커요</b> → 결과 칸이 밝아져요." : "모두 더하면 <b>어둡게 하는 쪽이 더 커요</b>. 이렇게 마이너스가 나오면 0으로 바꾸기로 해서 → 결과 칸은 <b>검정(꺼짐)</b>이에요.").ToString();
        }

        internal static string Merge(int layer, int map, int digit, int count, int winner)
        {
            if (layer != 4) { return string.Format("{0}장을 한 장으로 더하면 → <b>{1} 완성!</b>", count, MapId(layer, map)); }
            return string.Format(winner == digit ? "{0}장을 모두 더한 값이 <b>숫자 {1}의 점수</b>예요. 세 숫자 중 <b>가장 높아요</b>."
                : "{0}장을 모두 더한 값이 <b>숫자 {1}의 점수</b>예요. 다른 숫자보다 <b>낮아요</b>.", count, digit);
        }

        internal const string MatchTemplates = "은닉층 3의 피처맵 5장을 <b>한 덩어리로 쌓았어요</b>. 통 위에는 숫자마다 \"이 숫자라면 이렇게 생겨야 해\" 하는 <b>틀</b>이 있어요.\n덩어리를 틀마다 겹쳐 보고, <b>잘 맞을수록 틀이 빛나고 점수(빛 기둥)가 높아져요</b>.";
        internal static string ShareLight(int winner, int percent) => string.Format("이제 위의 <b>빛 탱크에 담긴 100%</b>를 점수에 따라 세 통에 나눠 담아요.\n점수가 조금만 높아도 <b>훨씬 많이</b> 가져가요. 숫자 {0} 통이 <b>{1}%</b>까지 차올라요.", winner, percent);
        internal static string FlyAnswer(int winner) => string.Format("가장 많이 차오른 통이 <b>AI의 답</b>이에요.\n입력 그림이 날아가 앉은 곳 → <b>이 그림은 숫자 {0}!</b>", winner);
        internal static string Recap(int layer, int map, int count, bool mandatory)
        {
            if (layer == 4) { return OutputDone; }
            if (!mandatory) { return string.Format("<b>{0}</b>가 만들어졌어요.", MapId(layer, map)); }
            var other = new string[count - 1];
            for (int i = 1; i < count; i++) { other[i - 1] = MapId(layer, i); }
            return string.Format("같은 층의 {0}도 <b>똑같이 {1}로</b> 만들어져요. 재료는 같고 <b>필터만 달라서</b> 찾는 특징이 달라요.\n<color=#8FA3B8>궁금하면 다른 피처맵도 눌러서 직접 볼 수 있어요 (선택)</color>", string.Join(", ", other), Materials[layer - 1]);
        }

        internal static string FirstOff(int layer, int map)
        {
            if (layer == 1 && map == 0) { return "숫자 칸 찾기를 끄니 뒤쪽이 거의 다 꺼졌어요!\n재료 하나가 사라지면 뒤쪽 맵과 출력까지 바뀌어요. <color=#8FA3B8>(피처맵 10장 모두 끌 수 있어요)</color>"; }
            if (layer == 2 && map == 0) { return "세로 선 찾기를 끄니 그걸 쓰던 뒤쪽 맵이 흐려졌어요.\n재료 하나가 사라지면 뒤쪽 맵과 출력까지 바뀌어요. <color=#8FA3B8>(피처맵 10장 모두 끌 수 있어요)</color>"; }
            if (layer == 2 && map == 1) { return "가로 선 찾기를 끄니 그걸 쓰던 뒤쪽 맵이 흐려졌어요.\n재료 하나가 사라지면 뒤쪽 맵과 출력까지 바뀌어요. <color=#8FA3B8>(피처맵 10장 모두 끌 수 있어요)</color>"; }
            return string.Format("<b>{0}</b>를 껐어요. 이걸 재료로 쓰던 뒤쪽 피처맵과 출력이 어떻게 바뀌었는지 보세요.\n재료 하나가 사라지면 뒤쪽 맵과 출력까지 바뀌어요. <color=#8FA3B8>(피처맵 10장 모두 끌 수 있어요)</color>", MapName(layer, map));
        }
    }
}
