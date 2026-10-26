using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Tablet
{
    [DefaultExecutionOrder(-400)]
    public sealed class TabletManager : MonoBehaviour
    {
        [SerializeField, HideInInspector] private Text _message;
        [SerializeField] private Font _font;
        [SerializeField] private Font _parchmentFont;
        [SerializeField] private Sprite _parchmentSprite;
        [SerializeField] private Transform[] _stageStartPoints;
        [SerializeField] private StageMoveEvent _stageMoveRequested = new StageMoveEvent();
        [SerializeField] private UnityEvent _saveRequested = new UnityEvent();
        [SerializeField] private UnityEvent _exitRequested = new UnityEvent();

        private Font _resolvedFont;
        private TabletVisualResources _resources;
        private TabletUIBuilder _ui;
        private TabletInputController _input;
        private TabletParchmentController _parchment;
        private TabletSettingsController _settings;
        private TabletLearningController _learning;
        private int _detailReturnFocusIndex;
        private bool _initialized;
        internal TabletMapData MapData { get; private set; }

        private TabletInputController.FocusGroup ActiveGroup => _ui.Shell.DetailOverlay.activeSelf
            ? TabletInputController.FocusGroup.Detail : _ui.Shell.StageMenu.activeSelf
            ? TabletInputController.FocusGroup.Stage : _settings.IsOpen
            ? TabletInputController.FocusGroup.Settings : TabletInputController.FocusGroup.Main;

        private void Awake()
        {
            ResolveFont();
            _resources = new TabletVisualResources();
            _input = new TabletInputController(GetComponentInParent<ContinuousMoveProvider>(),
                () => ActiveGroup, () => _ui.CanvasRect, SetInteraction, ToggleSettings, GoBack,
                (control, mainGroup) => _learning.RefreshFocus(control, mainGroup));
            _ui = new TabletUIBuilder(transform, _resources, _resolvedFont, _parchmentFont, _parchmentSprite, _input);
            _parchment = new TabletParchmentController(_ui, _resources, _input.ResetFocus,
                visible => _ui.Shell.FeedbackRoot.SetActive(visible), UpdateFeedback);
            _settings = new TabletSettingsController(this, _ui.Settings, () => _saveRequested.Invoke(), UpdateFeedback);
            _learning = new TabletLearningController(transform, _stageStartPoints, _ui.Learning,
                _resources, HandleStageMoveRequested, UpdateFeedback);
            _settings.LoadSettings(out int stageIndex, out int itemIndex);
            _learning.RestoreSelection(stageIndex);
            _parchment.RestoreSelection(itemIndex);
            _resources.CreateRuntimeSprites();
            _ui.Build(this, _parchment, _settings, _learning);
            RefreshAll();
            _initialized = true;
        }

        private void OnEnable()
        {
            _input?.Enable();
        }

        private void Update()
        {
            _input.Tick();
        }

        // Keep the pickup, minimap and serialized component links on the existing entry point.
        public bool HasParchment(int index) => _parchment.HasParchment(index);
        public bool CollectParchment(int index) => _parchment.CollectParchment(index);

        public void SetFactoryMission(int phase, int collected, int completed, string mission)
        {
            _learning.SetFactoryMission(phase, collected, completed, mission);
        }

        public void SetMapData(TabletMapData data)
        {
            MapData = data;
            foreach (TabletMinimapController map in GetComponentsInChildren<TabletMinimapController>(true))
            {
                map.RebuildMap();
            }
        }

        public void ClearMapData(TabletMapData data)
        {
            // An unloading scene must not clear a newer scene's map.
            if (MapData == data)
            {
                SetMapData(null);
            }
        }

        private void HandleStageMoveRequested(int stageIndex)
        {
            _stageMoveRequested.Invoke(stageIndex);
            _ui.Shell.StageMenu.SetActive(false);
            _settings.SetSettingsVisible(false);
            _input.ResetFocus(SettingsFocusIndex());
        }

        internal void SaveSettings()
        {
            _settings.SaveSettings(_learning.SelectedIndex, _parchment.SelectedIndex);
        }

        private int SettingsFocusIndex()
        {
            return _input.IndexOfMainControl(_ui.Shell.SettingsButton, _parchment.SelectedIndex);
        }

        private void ResolveFont()
        {
            _resolvedFont = _font;
            if (_resolvedFont == null && _message != null)
            {
                _resolvedFont = _message.font;
            }
            if (_resolvedFont == null)
            {
                _resolvedFont = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK KR", "Malgun Gothic", "Arial" }, 16);
            }
            if (_resolvedFont == null)
            {
                _resolvedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        private void RefreshAll()
        {
            _parchment.RefreshInventory();
            _learning.RefreshTasks();
            _learning.RefreshStage();
            _learning.RefreshProgress();
            _settings.RefreshSettingsValues();
            UpdateFeedback("교육 문서 수집",
                "방으로 가는 길에서 양피지를 찾아\n오른손 Trigger 또는 Grip으로 획득하세요.\n\n획득한 문서는 오른쪽 아이템함에서\n선택하면 이곳에서 읽을 수 있습니다.");
        }

        internal void ShowFactoryMap()
        {
            ShowDetail("공장 안내도", "", false);
            _ui.Shell.DetailBody.gameObject.SetActive(false);
            _ui.Shell.FactoryMapView.SetActive(true);
        }

        internal void ShowHint()
        {
            ShowDetail("필터 관찰 힌트",
                "밝은 픽셀과 붉은 픽셀의 RGB 값을 비교해보세요.\n\n" +
                "흰색은 R·G·B 값이 모두 높고, 붉은색은 R 값이 상대적으로 높습니다.\n\n" +
                "필터는 관찰하는 픽셀뿐 아니라 주변 픽셀의 값도 함께 사용합니다.", false);
        }

        internal void ShowLearningInfo()
        {
            ShowDetail("태블릿은 미션 보조 도구", "할 일을 모를 때: 현재 미션\n길을 찾을 때: 지도\n원리를 복습할 때: 수집한 양피지\n진행률: 양피지 5개 + 방 체험 3개\n\n" +
                "현재 단계: " + _learning.SelectedName + "\n선택한 양피지: " + TabletParchmentController.ItemName(_parchment.SelectedIndex) +
                "\n\n케디헌", false);
        }

        internal void ShowDetail(string title, string body, bool showImage)
        {
            _ui.Shell.ConfirmQuitButton.gameObject.SetActive(false);
            _ui.Shell.DetailCloseButton.GetComponentInChildren<Text>().text = "완료";
            _ui.Shell.FactoryMapView.SetActive(false);
            _ui.Shell.DetailBody.gameObject.SetActive(true);
            _detailReturnFocusIndex = _input.FocusIndex;
            _ui.Shell.DetailTitle.text = title;
            _ui.Shell.DetailBody.text = body;
            _ui.Shell.DetailBody.fontSize = showImage ? 23 : 27;
            _ui.Shell.DetailBody.rectTransform.anchoredPosition = new Vector2(0f, showImage ? 211f : 60f);
            _ui.Shell.DetailBody.rectTransform.sizeDelta = new Vector2(700f, showImage ? 54f : 330f);
            _ui.Shell.DetailImage.gameObject.SetActive(showImage);
            _ui.Shell.DetailOverlay.SetActive(true);
            _input.ResetFocus(0);
        }

        internal void CloseDetail()
        {
            _ui.Shell.DetailOverlay.SetActive(false);
            _input.ResetFocus(_detailReturnFocusIndex);
        }

        internal void RequestApplicationQuit()
        {
            ShowDetail("앱 종료", "체험을 종료하고 Quest 홈으로 돌아갈까요?\n\n현재 체험 진행과 학습 결과는 유지되지 않습니다.", false);
            _ui.Shell.DetailCloseButton.GetComponentInChildren<Text>().text = "취소";
            _ui.Shell.ConfirmQuitButton.gameObject.SetActive(true);
            _input.ResetFocus(0);
        }

        internal void ConfirmApplicationQuit()
        {
            if (!_ui.Shell.DetailOverlay.activeSelf || !_ui.Shell.ConfirmQuitButton.gameObject.activeSelf)
            {
                return;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        internal void ToggleSettings()
        {
            if (!_input.IsInteracting)
            {
                SetInteraction(true);
            }
            bool open = !_settings.IsOpen;
            _ui.Shell.DetailOverlay.SetActive(false);
            _ui.Shell.StageMenu.SetActive(false);
            _settings.SetSettingsVisible(open);
            _input.ResetFocus(open ? 0 : SettingsFocusIndex());
        }

        internal void ShowStageMenu(bool open)
        {
            _ui.Shell.StageMenu.SetActive(open);
            _input.ResetFocus(open ? _learning.SelectedIndex : 0);
        }

        internal void RequestExit()
        {
            SetInteraction(false);
            _exitRequested.Invoke();
            UpdateFeedback("태블릿 조작 종료", "현재 교육 화면으로 돌아왔습니다.");
        }

        private void UpdateFeedback(string title, string body)
        {
            if (_ui.Shell.FeedbackTitle != null)
            {
                _ui.Shell.FeedbackTitle.text = title;
            }
            if (_ui.Shell.FeedbackBody != null)
            {
                _ui.Shell.FeedbackBody.text = body;
            }
        }

        private void SetInteraction(bool active)
        {
            if (!active)
            {
                _settings.SetSettingsVisible(false);
                _ui.Shell.StageMenu.SetActive(false);
                if (_ui.Shell.DetailOverlay != null)
                {
                    _ui.Shell.DetailOverlay.SetActive(false);
                }
            }
            _input.SetInteraction(active, _parchment.SelectedIndex);
        }

        private void GoBack()
        {
            if (_ui.Shell.DetailOverlay.activeSelf)
            {
                CloseDetail();
            }
            else if (_input.IsAdjustingSlider)
            {
                _input.StopAdjustingSlider();
            }
            else if (_input.IsInteracting && _ui.Shell.StageMenu.activeSelf)
            {
                ShowStageMenu(false);
            }
            else if (_settings.IsOpen)
            {
                ToggleSettings();
            }
            else if (_parchment.IsOpen)
            {
                _parchment.CloseParchment();
            }
            else
            {
                SetInteraction(false);
            }
        }

        private void OnDisable()
        {
            _input?.Disable();
            if (_initialized)
            {
                SetInteraction(false);
            }
            _settings?.StopAnimation();
        }

        private void OnDestroy()
        {
            _settings?.StopAnimation();
            _input?.Dispose();
            _resources?.Dispose();
        }

        [Serializable]
        private sealed class StageMoveEvent : UnityEvent<int>
        {
        }
    }
}
