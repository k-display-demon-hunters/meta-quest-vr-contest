using Learning;
using UnityEngine;
using UnityEngine.UI;
namespace Tablet
{
    internal sealed class TabletLearningController
    {
        private readonly TabletUIBuilder _view;
        private LearningProgressSnapshot _snapshot = LearningProgressSnapshot.Empty;
        public TabletLearningController(TabletUIBuilder view) { _view = view; }
        public void ShowTravelStarted() => _view.Status.text = "선택한 단계로 이동 중…";
        public void ShowTravelFailed() => _view.Status.text = "이동하지 못했습니다. 단계를 다시 선택하세요.";
        public void ShowTravelCompleted() => _view.Status.text = "";

        public void Refresh(LearningProgressSnapshot snapshot)
        {
            _snapshot = snapshot;
            RefreshView();
        }

        public void RefreshView()
        {
            for (int i = 0; i < LearningStageCatalog.ProgressStages.Count; i++)
            {
                LearningStageId stage = LearningStageCatalog.ProgressStages[i].Id;
                bool complete = _snapshot.IsCompleted(stage);
                bool current = stage == _snapshot.CurrentStage;
                _view.StageStates[i].text = current ? (complete ? "현재 · 완료" : "현재 단계") : complete ? "완료" : "시작 전";
                _view.StageButtons[i].interactable = _snapshot.CanTravel;
                _view.StageButtons[i].GetComponent<Image>().color = current ? TabletUIBuilder.AccentSoftColor : TabletUIBuilder.CardColor;
            }
            _view.ProgressLabel.text = Mathf.RoundToInt(_snapshot.Progress01 * 100f) + "%";
            _view.ProgressFill.fillAmount = _snapshot.Progress01;
        }
    }
}
