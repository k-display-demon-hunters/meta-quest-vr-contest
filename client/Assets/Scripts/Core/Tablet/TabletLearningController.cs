using UnityEngine.UI;
namespace Tablet
{
    internal sealed class TabletLearningController
    {
        private readonly TabletUIBuilder _view;
        public TabletLearningController(TabletUIBuilder view) { _view = view; }
        public void Refresh(int currentStage, int completedMask, bool canTravel)
        {
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
