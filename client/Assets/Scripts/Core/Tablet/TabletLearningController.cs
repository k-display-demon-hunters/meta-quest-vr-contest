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
        private const float DefaultProgress = 0.75f;

        private readonly string[] _phaseNames = { "튜토리얼", "실습하기", "응용하기", "정리하기", "퀴즈" };
        private readonly int[] _phaseStageIndices = { 0, 2, 3, 4, 5 };
        private readonly string[] _stageNames =
        {
            "1. 튜토리얼",
            "2. 기본 오프닝",
            "3. 필터 조작",
            "4. MLP (간략)",
            "5. CNN",
            "6. AI 업스케일링"
        };
        private readonly TabletTask[] _tasks =
        {
            new TabletTask("오염 픽셀 위치 확인", true),
            new TabletTask("RGB 채널값 비교", false),
            new TabletTask("필터 개념 적용", false)
        };
        private int _selectedStageIndex;
        private int _completedTaskIndex = 1;

        private readonly Transform _tabletTransform;
        private readonly Transform[] _stageStartPoints;
        private readonly TabletUIBuilder.LearningView _view;
        private readonly TabletVisualResources _resources;
        private readonly Action<int> _stageMoveRequested;
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
            _tabletTransform = tabletTransform;
            _stageStartPoints = stageStartPoints;
            _view = view;
            _resources = resources;
            _stageMoveRequested = stageMoveRequested;
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
                bool isCurrent = index == Mathf.Max(0, _selectedStageIndex - 1);
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
            int phaseIndex = Mathf.Max(0, _selectedStageIndex - 1);
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
            int percent = Mathf.RoundToInt(DefaultProgress * 100f);
            if (_view.ProgressPercent != null)
            {
                _view.ProgressPercent.text = "전체 진행도 <b><color=#315A96>" + percent + "%</color></b>";
            }
            if (_view.ProgressFill != null)
            {
                _view.ProgressFill.rectTransform.sizeDelta = new Vector2(ProgressTrackWidth * DefaultProgress, 14f);
            }
        }

        public void ConfirmCurrentTask()
        {
            if (_completedTaskIndex < _tasks.Length)
            {
                _tasks[_completedTaskIndex].IsCompleted = true;
                _completedTaskIndex++;
                RefreshTasks();
                _feedback("임무 확인 완료",
                    "체크리스트가 갱신되었습니다.\n다음 단계에서는 RGB 채널과 필터 커널의 영향을 비교하세요.");
                return;
            }
            _feedback("모든 현재 임무 확인됨",
                "현재 임무를 모두 확인했습니다.\n설정에서 저장하거나 단계 이동을 선택할 수 있습니다.");
        }

        public void MoveToStage(int stageIndex)
        {
            _selectedStageIndex = Mathf.Clamp(stageIndex, 0, _stageNames.Length - 1);
            RefreshStage();
            _stageMoveRequested(_selectedStageIndex);

            Transform target = null;
            if (_stageStartPoints != null && _selectedStageIndex < _stageStartPoints.Length)
            {
                target = _stageStartPoints[_selectedStageIndex];
            }
            if (target != null)
            {
                XROrigin origin = _tabletTransform.GetComponentInParent<XROrigin>();
                Transform rig = origin != null ? origin.transform : _tabletTransform.root;
                rig.SetPositionAndRotation(target.position, target.rotation);
                _feedback("단계 이동 완료", _stageNames[_selectedStageIndex] + " 시작 지점으로 이동했습니다.");
            }
            else
            {
                _feedback("단계 이동 선택됨",
                    _stageNames[_selectedStageIndex] + " 시작 지점이 아직 준비되지 않았습니다.");
            }
        }

        [Serializable]
        private sealed class TabletTask
        {
            public TabletTask(string label, bool isCompleted)
            {
                Label = label;
                IsCompleted = isCompleted;
            }

            public string Label { get; }
            public bool IsCompleted { get; set; }
        }
    }
}
