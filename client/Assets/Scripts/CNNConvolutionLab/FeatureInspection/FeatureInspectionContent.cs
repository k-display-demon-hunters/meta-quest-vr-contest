namespace CNNConvolution
{
    public sealed class FeatureInspectionContent
    {
        public const string CompletedTitle = "특징 판독 완료";
        public const string CompletedMessage = "특징맵 한 장은 한 필터가 이미지의 각 위치에서 얼마나 반응했는지를 보여줘요.";

        public InspectionFilter Filter { get; }
        public string MapLabel { get; }
        public string SpecimenLabel { get; }
        public string Guidance { get; }
        public string StateLabel { get; }
        public string RotationButtonLabel { get; }
        public bool CanSwitchFilter { get; }
        public bool ShowRotation { get; }
        public bool CanFinish { get; }

        private FeatureInspectionContent(InspectionFilter filter, bool rotated, bool completed)
        {
            bool redFilter = filter == InspectionFilter.RedResponse;
            Filter = filter;
            MapLabel = redFilter ? "A · 빨강 반응  /  4×4" : "B · 세로 경계  /  4×4";
            SpecimenLabel = rotated ? "비교 표본 · 6×6  /  가로 이음새" : "비교 표본 · 6×6  /  세로 이음새";
            Guidance = rotated
                ? "색은 그대로예요. 이음새가 가로로 누우면서 B의 밝은 줄이 사라져요."
                : redFilter
                    ? "A 맵은 빨강 성분이 강한 쪽에 넓게 반응해요."
                    : "같은 표본에서 B 맵은 좌우 밝기가 갈리는 곳을 한 줄로 보여줘요.";
            StateLabel = rotated
                ? "선택 실험 · 필터 B 고정"
                : completed ? "비교 완료 · 필터를 다시 바꾸거나 체험을 마칠 수 있어요." : "같은 표본 · 다른 필터  /  필터 B를 눌러 비교하세요.";
            RotationButtonLabel = rotated ? "표본 되돌리기" : "선택 · 표본 90° 돌리기";
            CanSwitchFilter = !rotated;
            ShowRotation = !redFilter;
            CanFinish = completed;
        }

        public static FeatureInspectionContent ForState(InspectionFilter filter, bool rotated, bool completed)
        {
            return new FeatureInspectionContent(filter, rotated, completed);
        }

        public static string RotationMessage(bool rotated)
        {
            return rotated
                ? "선택 실험 · 표본 회전 중 · 필터 B 고정"
                : "선택 실험 · 표본 복원 중 · 필터 B 고정";
        }
    }
}
