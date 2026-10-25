using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletLearningController
    {
        private float _progress;
        private string _mission = "양피지를 수집하고 세 방의 체험을 완료하세요.";

        private readonly string[] _phaseNames = { "미션 안내", "픽셀·합성곱", "피처맵", "모델 제작", "복구 마무리" };
        private readonly int[] _phaseStageIndices = { 0, 1, 2, 3, 4 };
        private readonly string[] _stageNames = { "미션 안내", "Pixel Lab", "Feature Map Lab", "Model Design Lab", "복구 마무리" };
        private readonly TabletTask[] _tasks =
        {
            new TabletTask("양피지 수집 0/5", false),
            new TabletTask("방 체험 완료 0/3", false),
            new TabletTask("미션 안내 읽기", false)
        };
        private int _selectedStageIndex;

        private readonly TabletUIBuilder.LearningView _view;
        private readonly TabletVisualResources _resources;
        private readonly Action<string, string> _feedback;

        public IReadOnlyList<string> PhaseNames => _phaseNames;
        public IReadOnlyList<int> PhaseStageIndices => _phaseStageIndices;
        public IReadOnlyList<string> StageNames => _stageNames;
        public int TaskCount => _tasks.Length;
        public string TaskLabel(int index) => _tasks[index].Label;
        public int SelectedIndex => _selectedStageIndex;
        public string SelectedName => _stageNames[_selectedStageIndex];

        public TabletLearningController(Transform tabletTransform, Transform[] stageStartPoints,
            TabletUIBuilder.LearningView view, TabletVisualResources resources,
            Action<int> stageMoveRequested, Action<string, string> feedback)
        {
            _view = view;
            _resources = resources;
            _feedback = feedback;
        }

        public void RestoreSelection(int index)
        {
            _selectedStageIndex = Mathf.Clamp(index, 0, _stageNames.Length - 1);
        }

        public void RefreshFocus(Selectable focusedControl, bool mainGroup)
        {
            for (int index = 0; index < _view.PhaseSurfaces.Count; index++)
            {
                bool isCurrent = index == _selectedStageIndex;
                bool isFocused = mainGroup && focusedControl != null &&
                    focusedControl.gameObject == _view.PhaseSurfaces[index].gameObject;
                _view.PhaseSurfaces[index].color = new Color(1f, 1f, 1f, isCurrent || isFocused ? 0.85f : 0.015f);
            }
        }

        public void RefreshTasks()
        {
            int completedCount = 0;
            for (int index = 0; index < _tasks.Length; index++)
            {
                _view.TaskTexts[index].text = _tasks[index].Label;
                bool isDone = _tasks[index].IsCompleted;
                _view.TaskChecks[index].sprite = isDone ? _resources.BlueCircleSprite : _resources.MutedCircleSprite;
                _view.TaskChecks[index].color = CardColor;
                _view.TaskCheckmarks[index].gameObject.SetActive(isDone);
                _view.TaskTexts[index].color = isDone ? AccentColor : MutedTextColor;
                _view.TaskRows[index].sprite = _resources.SelectedSprite;
                _view.TaskRows[index].color = new Color(1f, 1f, 1f, isDone ? 0.65f : 0f);
                if (isDone)
                {
                    completedCount++;
                }
            }
            _view.TaskCount.text = completedCount + "/" + _tasks.Length;
        }

        public void RefreshStage()
        {
            string stageName = _stageNames[_selectedStageIndex];
            if (_view.SettingsStageLabel != null)
            {
                _view.SettingsStageLabel.text = stageName;
            }
            for (int index = 0; index < _view.StageSurfaces.Count; index++)
            {
                bool isCurrent = index == _selectedStageIndex;
                _view.StageSurfaces[index].color = isCurrent ? AccentSoftColor : CardColor;
                _view.StageCheckmarks[index].gameObject.SetActive(isCurrent);
            }
            int phaseIndex = _selectedStageIndex;
            for (int index = 0; index < _view.PhaseSurfaces.Count; index++)
            {
                bool isCurrent = index == phaseIndex;
                _view.PhaseSurfaces[index].sprite = isCurrent ? _resources.SelectedSprite : _resources.PanelSprite;
                _view.PhaseSurfaces[index].color = new Color(1f, 1f, 1f, isCurrent ? 0.85f : 0.015f);
                _view.PhaseCircles[index].sprite = isCurrent ? _resources.BlueCircleSprite : _resources.MutedCircleSprite;
                _view.PhaseCircles[index].rectTransform.sizeDelta = Vector2.one * (isCurrent ? 54f : 42f);
                _view.PhaseLabels[index].fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal;
                _view.PhaseLabels[index].color = isCurrent ? TextColor : MutedTextColor;
                _view.PhaseChevrons[index].gameObject.SetActive(isCurrent);
            }
        }

        public void RefreshProgress()
        {
            int percent = Mathf.RoundToInt(_progress * 100f);
            if (_view.ProgressPercent != null)
            {
                _view.ProgressPercent.text = "전체 진행도 <b><color=#315A96>" + percent + "%</color></b>";
            }
            if (_view.ProgressFill != null)
            {
                _view.ProgressFill.rectTransform.sizeDelta = new Vector2(ProgressTrackWidth * _progress, 14f);
            }
        }

        public void SetFactoryMission(int phase, int collected, int completed, string mission)
        {
            _selectedStageIndex = Mathf.Clamp(phase, 0, _stageNames.Length - 1);
            _progress = Mathf.Clamp01((collected + completed) / 8f);
            _mission = mission;
            _tasks[0].Label = "양피지 수집 " + collected + "/5";
            _tasks[0].IsCompleted = collected == 5;
            _tasks[1].Label = "방 체험 완료 " + completed + "/3";
            _tasks[1].IsCompleted = completed == 3;
            _tasks[2].Label = _stageNames[_selectedStageIndex];
            _tasks[2].IsCompleted = completed == 3 && collected == 5;
            RefreshTasks();
            RefreshProgress();
            RefreshStage();
        }

        public void ConfirmCurrentTask()
        {
            _feedback("현재 미션 · " + SelectedName, _mission +
                "\n\n진행률 = 양피지 5개 + 방 체험 3개, 총 8개 목표의 완료 비율입니다.\n미수집 양피지는 지도에서 찾아 돌아가 수집할 수 있어요.");
        }

        public void MoveToStage(int stageIndex)
        {
            _feedback("이동 안내", _stageNames[Mathf.Clamp(stageIndex, 0, _stageNames.Length - 1)] +
                "\n공장 지도를 확인하고 바닥 안내선을 따라 직접 이동하세요.\n현재 미션: " + _mission);
        }

        [Serializable]
        private sealed class TabletTask
        {
            public TabletTask(string label, bool isCompleted)
            {
                Label = label;
                IsCompleted = isCompleted;
            }

            public string Label { get; set; }
            public bool IsCompleted { get; set; }
        }
    }
}
