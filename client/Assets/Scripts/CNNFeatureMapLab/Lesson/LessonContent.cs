namespace CNNFeatureMaps.Lesson
{
    public sealed class LessonContent
    {
        public string Title { get; }
        public string Instruction { get; }
        public string Readout { get; }

        public LessonContent(string title, string instruction, string readout = "")
        {
            Title = title;
            Instruction = instruction;
            Readout = readout;
        }

        public static readonly LessonContent BuildFeatureMap = new LessonContent(
            "1 / 4 — feature map 만들기",
            "민트색 손잡이를 이미지 위에서 움직이거나, 입력 픽셀을 눌러 보세요.\n3×3 영역에서 출력 뉴런 하나가 만들어집니다. 자동 이동으로 전체를 둘러보세요.");

        public static readonly LessonContent CompareFilters = new LessonContent(
            "2 / 4 — 같은 입력, 다른 필터",
            "같은 이미지도 필터가 다르면 서로 다른 feature map이 됩니다.\n필터를 바꿔 비교하고, 맵의 테두리를 잡아 가까이 끌어와 보세요.");

        public static readonly LessonContent TraceReceptiveField = new LessonContent(
            "3 / 4 — 한 픽셀의 영향 따라가기",
            "입력 픽셀 하나를 누르고, 각 층의 노란 뉴런들을 따라가 보세요.\n깊은 층의 뉴런일수록 넓게 봅니다: 입력 기준 최대 3×3 → 5×5 → 7×7.",
            "노란색은 수용 영역을 표시하며, 가중치가 0인 위치도 포함합니다.\n맵의 밝기는 ReLU 계산값입니다. 뉴런을 움직여도 계산 연결은 유지됩니다.");

        public static readonly LessonContent EditInput = new LessonContent(
            "4 / 4 — 입력을 바꿔 보기",
            "입력 픽셀을 눌러 흑백을 바꾸고, feature map의 변화를 보세요.\n잡고·돌리고·확대하고, 신경망 사이를 걸어 다니며 살펴보세요.",
            "고정된 예제 필터 / 한 픽셀씩 이동 / 바깥은 0 / ReLU 적용\n예시 반응은 고양이·강아지 인식이 아닙니다. 처음 상태로 입력을 복원할 수 있습니다.");

        public const string EnlargeButtonText = "확대";
        public const string ShrinkButtonText = "축소";
        public const string NextButtonText = "다음으로";
        public const string FinishButtonText = "단계 마치기";
        public const string ScanButtonText = "자동 이동";
        public const string StopButtonText = "멈추기";
        public const string SpreadButtonFormat = "펼치기 {0}";

        public const string HorizontalFilterName = "좌우 변화 감지";
        public const string VerticalFilterName = "위아래 변화 감지";
        public const string HorizontalFilterWeights = "-1/3    0    +1/3\n-1/3    0    +1/3\n-1/3    0    +1/3";
        public const string VerticalFilterWeights = "-1/3   -1/3   -1/3\n  0       0       0\n+1/3  +1/3  +1/3";

        public const string PatchFormat = "입력 영역 ({0}, {1}): 곱한 값의 합 = {2:0.000} → ReLU = {3:0.000}\n{4} / 64 위치 체험 / ReLU는 음수를 0으로 바꿉니다.";
        public const string InputEditedFormat = "입력 픽셀을 {0}회 바꿨습니다. 전체 CNN이 다시 계산됩니다.\nA·B는 학습된 분류 결과가 아니라 예시 반응입니다.";
        public const string AffectedPixelFormat = "선택 픽셀 ({0}, {1}) → 첫 feature map마다 {2}개 뉴런\n노란색: 이 픽셀이 수용 영역에 포함됨 / 계산값의 크기와는 다릅니다.";
        public const string ReleaseBeforeActionText = "잡은 물체를 놓은 뒤 체험 단계를 바꿔 주세요.";
        public const string StepSkippedText = "입력 변경 활동을 건너뛰었습니다. ";
        public const string StepFinishedText = "현재 단계를 마쳤습니다. ";
        public const string MoveToModelDesignText = "바닥 안내선을 따라 모델 제작 방으로 이동하세요.";
        public const string SelectScaleTargetText = "뉴런·맵·층을 Trigger로 선택하거나 잡았다 놓은 뒤 확대·축소를 누르세요.";
    }
}
