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
        [SerializeField] private Font _parchmentTitleFont;
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
        private TabletDetailController _detail;
        private TabletMapData _mapData;
        private int _detailReturnFocusIndex;
        private bool _initialized;
        internal Font ParchmentFont => _parchmentFont != null ? _parchmentFont : _resolvedFont;
        internal Font ParchmentTitleFont => _parchmentTitleFont != null ? _parchmentTitleFont : ParchmentFont;

        private TabletInputController.FocusGroup ActiveGroup => _detail.IsOpen
            ? TabletInputController.FocusGroup.Detail : _ui.Shell.StageMenu.activeSelf
            ? TabletInputController.FocusGroup.Stage : _settings.IsOpen
            ? TabletInputController.FocusGroup.Settings : TabletInputController.FocusGroup.Main;

        private void Awake()
        {
            ResolveFont();
            _resources = new TabletVisualResources();
            _input = new TabletInputController(GetComponentInParent<ContinuousMoveProvider>(),
                () => ActiveGroup, () => _ui.CanvasRect);
            _ui = new TabletUIBuilder(transform, _resources, _resolvedFont, _parchmentFont,
                _parchmentTitleFont, _parchmentSprite, _input);
            _parchment = new TabletParchmentController(_ui, _resources);
            _settings = new TabletSettingsController(this, _ui.Settings);
            _learning = new TabletLearningController(_ui.Learning, _resources);
            _detail = new TabletDetailController(_ui.Shell);
            _settings.LoadSettings(out int stageIndex, out int itemIndex);
            _learning.RestoreSelection(stageIndex);
            _parchment.RestoreSelection(itemIndex);
            _resources.CreateRuntimeSprites();
            _ui.Build(_settings.Brightness, _settings.Volume, _parchment.HasParchment);
            RefreshAll();
            _initialized = true;
        }

        private void OnEnable()
        {
            SubscribeEvents();
            _input.Enable();
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
            _mapData = data;
            _ui.SetMapData(data);
        }

        public void ClearMapData(TabletMapData data)
        {
            // An unloading scene must not clear a newer scene's map.
            if (_mapData == data)
            {
                SetMapData(null);
            }
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
                "방으로 가는 길에서 양피지를 찾아\n오른손 Trigger 또는 Grip으로 열어보세요.\n\n세 페이지를 읽고 완료를 누르면\n오른쪽 아이템함에 보관됩니다.");
        }

        internal void ShowFactoryMap()
        {
            ShowDetail("공장 안내도", "", false);
            _detail.ShowMap();
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
            _detailReturnFocusIndex = _input.FocusIndex;
            _detail.Show(title, body, showImage);
            _input.ResetFocus(0);
        }

        internal void CloseDetail()
        {
            _detail.Close();
            _input.ResetFocus(_detailReturnFocusIndex);
        }

        internal void RequestApplicationQuit()
        {
            ShowDetail("앱 종료", "체험을 종료하고 Quest 홈으로 돌아갈까요?\n\n현재 체험 진행과 학습 결과는 유지되지 않습니다.", false);
            _detail.ShowQuitConfirmation();
            _input.ResetFocus(0);
        }

        internal void ConfirmApplicationQuit()
        {
            if (!_detail.IsQuitConfirmationOpen)
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
            _detail.Close();
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
                _detail.Close();
            }
            _input.SetInteraction(active, _parchment.SelectedIndex);
        }

        private void GoBack()
        {
            if (_detail.IsOpen)
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

        private void SubscribeEvents()
        {
            _ui.ParchmentSelected += _parchment.SelectItem;
            _ui.ParchmentPageRequested += _parchment.ChangeParchmentPage;
            _ui.ParchmentCloseRequested += _parchment.CloseParchment;
            _ui.FeatureMapToggleRequested += _parchment.ToggleFeatureMap;
            _ui.CurrentTaskRequested += _learning.ConfirmCurrentTask;
            _ui.StageRequested += _learning.MoveToStage;
            _ui.BrightnessChanged += _settings.SetBrightness;
            _ui.VolumeChanged += _settings.SetVolume;
            _ui.VibrationRequested += _settings.ToggleVibration;
            _input.InteractionRequested += SetInteraction;
            _input.SettingsRequested += ToggleSettings;
            _input.BackRequested += GoBack;
            _input.FocusChanged += _learning.RefreshFocus;
            _ui.SettingsRequested += ToggleSettings;
            _ui.LearningInfoRequested += ShowLearningInfo;
            _ui.FactoryMapRequested += ShowFactoryMap;
            _ui.HintRequested += ShowHint;
            _ui.ImageDetailRequested += ShowImageDetail;
            _ui.SaveRequested += SaveSettings;
            _ui.ExitRequested += RequestExit;
            _ui.DetailCloseRequested += CloseDetail;
            _ui.QuitConfirmationRequested += RequestApplicationQuit;
            _ui.QuitRequested += ConfirmApplicationQuit;
            _ui.StageMenuRequested += ShowStageMenu;
            _parchment.FocusRequested += _input.ResetFocus;
            _parchment.FeedbackVisibilityChanged += SetFeedbackVisible;
            _parchment.FeedbackRequested += UpdateFeedback;
            _settings.Saved += HandleSettingsSaved;
            _settings.FeedbackRequested += UpdateFeedback;
            _learning.FeedbackRequested += UpdateFeedback;
        }

        private void UnsubscribeEvents()
        {
            _ui.ParchmentSelected -= _parchment.SelectItem;
            _ui.ParchmentPageRequested -= _parchment.ChangeParchmentPage;
            _ui.ParchmentCloseRequested -= _parchment.CloseParchment;
            _ui.FeatureMapToggleRequested -= _parchment.ToggleFeatureMap;
            _ui.CurrentTaskRequested -= _learning.ConfirmCurrentTask;
            _ui.StageRequested -= _learning.MoveToStage;
            _ui.BrightnessChanged -= _settings.SetBrightness;
            _ui.VolumeChanged -= _settings.SetVolume;
            _ui.VibrationRequested -= _settings.ToggleVibration;
            _input.InteractionRequested -= SetInteraction;
            _input.SettingsRequested -= ToggleSettings;
            _input.BackRequested -= GoBack;
            _input.FocusChanged -= _learning.RefreshFocus;
            _ui.SettingsRequested -= ToggleSettings;
            _ui.LearningInfoRequested -= ShowLearningInfo;
            _ui.FactoryMapRequested -= ShowFactoryMap;
            _ui.HintRequested -= ShowHint;
            _ui.ImageDetailRequested -= ShowImageDetail;
            _ui.SaveRequested -= SaveSettings;
            _ui.ExitRequested -= RequestExit;
            _ui.DetailCloseRequested -= CloseDetail;
            _ui.QuitConfirmationRequested -= RequestApplicationQuit;
            _ui.QuitRequested -= ConfirmApplicationQuit;
            _ui.StageMenuRequested -= ShowStageMenu;
            _parchment.FocusRequested -= _input.ResetFocus;
            _parchment.FeedbackVisibilityChanged -= SetFeedbackVisible;
            _parchment.FeedbackRequested -= UpdateFeedback;
            _settings.Saved -= HandleSettingsSaved;
            _settings.FeedbackRequested -= UpdateFeedback;
            _learning.FeedbackRequested -= UpdateFeedback;
        }

        private void SetFeedbackVisible(bool visible)
        {
            _ui.Shell.FeedbackRoot.SetActive(visible);
        }

        private void HandleSettingsSaved()
        {
            _saveRequested.Invoke();
        }

        private void ShowImageDetail()
        {
            ShowDetail("원본 이미지", "오염 픽셀과 주변 픽셀의 밝기·색을 비교하세요.", true);
        }

        private void OnDisable()
        {
            _input?.Disable();
            if (_initialized)
            {
                SetInteraction(false);
            }
            _settings?.StopAnimation();
            UnsubscribeEvents();
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
