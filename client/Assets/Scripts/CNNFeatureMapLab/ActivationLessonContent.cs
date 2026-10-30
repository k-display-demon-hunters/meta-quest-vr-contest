namespace CNNFeatureMaps.Activation
{
    internal static class ActivationLessonContent
    {
        internal const string Intro = "이번에는 활성화 함수 ReLU예요!\n\n양피지 4의 피쳐맵에 같은 규칙을 적용해요.\n음수는 파랑, 양수는 주황, 0은 회색으로 표시합니다.\n\nReLU 적용을 누르고, 전후의 같은 칸을 비교해 보세요.";
        internal const string Applied = "음수는 0, 양수는 그대로!\n\nReLU는 각 칸에 max(0, x)를 적용해요.\n칸의 위치와 피쳐맵 크기는 바뀌지 않아요.\n\n확인을 누른 뒤 파란 칸과 주황 칸을 눌러 비교해 보세요.";
        internal const string Completed = "양피지 5 체험을 완료했어요!\n\nReLU는 음수를 0으로 바꾸고 양수를 유지합니다.\n이런 비선형 함수를 층 사이에 적용하면\n더 복잡한 관계를 학습할 수 있어요.";
        internal static string MapName(int channel) => channel == 0 ? "빨강 검출 피쳐맵" : "세로 경계 피쳐맵";
    }
}
