using UnityEngine.UI;
namespace Tablet
{
    internal sealed class TabletLearningController
    {
        private readonly TabletUIBuilder _view;
        private int _currentStage;
        private int _completedMask;
        private bool _canTravel;
        private bool _hasState;
        public TabletLearningController(TabletUIBuilder view) { _view = view; }
        public void ShowTravelStarted() => _view.Status.text = "선택한 단계로 이동 중…";
        public void ShowTravelFailed() => _view.Status.text = "이동하지 못했습니다. 단계를 다시 선택하세요.";
        public void ShowTravelCompleted() => _view.Status.text = "";

        public void Refresh(int currentStage, int completedMask, bool canTravel)
        {
            if (_hasState && _currentStage == currentStage && _completedMask == completedMask && _canTravel == canTravel) { return; }
            _hasState = true;
            _currentStage = currentStage;
            _completedMask = completedMask;
            _canTravel = canTravel;
            int count = 0;
            for (int i = 0; i < _view.StageButtons.Length; i++)
            {
                bool complete = (completedMask & (1 << i)) != 0;
                bool current = i == currentStage;
                if (complete) { count++; }
                _view.StageStates[i].text = current ? (complete ? "현재 · 완료" : "현재 단계") : complete ? "완료" : "시작 전";
                _view.StageButtons[i].interactable = canTravel;
                _view.StageButtons[i].GetComponent<Image>().color = current ? TabletUIBuilder.AccentSoftColor : TabletUIBuilder.CardColor;
            }
            _view.ProgressLabel.text = (count * 25) + "%";
            _view.ProgressFill.fillAmount = count / 4f;
        }
    }
}
