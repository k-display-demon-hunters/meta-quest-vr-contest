using System;
using Learning;
using Parchment;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Tablet
{
    [DefaultExecutionOrder(-400)]
    public sealed class TabletManager : MonoBehaviour
    {
        [SerializeField] private TabletUIBuilder _interfacePrefab;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private MeshFilter _body;
        [SerializeField] private GameObject _visualRoot;
        private TabletUIBuilder _ui;
        private TabletVisualResources _resources;
        private TabletInputController _input;
        private TabletLearningController _learning;
        private TabletParchmentController _parchment;
        private TabletSettingsController _settings;
        private TabletDetailController _detail;
        private ParchmentManager _reader;
        private TabletMapData _mapData;
        private int _inputResumeFrame = -1;
        private bool _interactionBeforeReader;
        private int _focusBeforeReader;
        private bool _visible = true;
        public bool IsAvailable => gameObject.activeSelf;
        public event Action<LearningStageId> StageMoveRequested;

        private TabletInputController.FocusGroup ActiveGroup => _detail.IsOpen
            ? TabletInputController.FocusGroup.Detail : _settings.IsOpen
            ? TabletInputController.FocusGroup.Settings : TabletInputController.FocusGroup.Main;

        private void Awake()
        {
            _resources = new TabletVisualResources();
            _resources.CreateRuntimeSprites();
            _resources.BuildRoundedBody(_body);
            RectTransform canvasRect = (RectTransform)_canvas.transform;
            canvasRect.sizeDelta = new Vector2(TabletUIBuilder.CanvasWidth, TabletUIBuilder.CanvasHeight);
            canvasRect.localScale = Vector3.one * (TabletUIBuilder.TabletWorldWidth / TabletUIBuilder.CanvasWidth);
            _canvas.worldCamera = Camera.main;
            _ui = Instantiate(_interfacePrefab, canvasRect, false);
            _reader = FindFirstObjectByType<ParchmentManager>();
            _input = new TabletInputController(GetComponentInParent<ContinuousMoveProvider>(), () => ActiveGroup, () => canvasRect);
            _settings = new TabletSettingsController(_ui);
            _detail = new TabletDetailController(_ui);
            _learning = new TabletLearningController(_ui);
            _parchment = new TabletParchmentController(_ui, _reader.HasCollected);
            _ui.Initialize(_resources, _input, _canvas.worldCamera.transform, _reader.HasCollected);
            _parchment.RestoreSelection(_settings.Load());
            _parchment.RefreshInventory();
            SetLearningState(LearningProgressSnapshot.Empty);
        }
        private void OnEnable()
        {
            _ui.AppRequested += ShowApp;
            _ui.StageRequested += RequestStage;
            _ui.PaperRequested += _parchment.SelectItem;
            _parchment.OpenRequested += OpenParchment;
            _reader.Opened += SuspendForParchment;
            _reader.Closed += RestoreAfterParchment;
            _reader.Collected += RefreshCollection;
            _input.InteractionRequested += SetInteraction;
            _input.VisibilityToggleRequested += ToggleVisibility;
            _input.SettingsRequested += ToggleSettings;
            _input.BackRequested += GoBack;
            _ui.SettingsButton.onClick.AddListener(ToggleSettings);
            _ui.SettingsClose.onClick.AddListener(ToggleSettings);
            _ui.Brightness.onValueChanged.AddListener(_settings.SetBrightness);
            _ui.Volume.onValueChanged.AddListener(_settings.SetVolume);
            _ui.Vibration.onClick.AddListener(_settings.ToggleVibration);
            _ui.Save.onClick.AddListener(SaveSettings);
            _ui.Quit.onClick.AddListener(ShowQuit);
            _ui.QuitCancel.onClick.AddListener(CloseQuit);
            _ui.QuitConfirm.onClick.AddListener(ConfirmQuit);
            _input.Enable();
            _learning.RefreshView();
            _parchment.RefreshInventory();
            if (_reader.IsOpen) { SuspendForParchment(); }
        }
        private void OnDisable()
        {
            _ui.AppRequested -= ShowApp;
            _ui.StageRequested -= RequestStage;
            _ui.PaperRequested -= _parchment.SelectItem;
            _parchment.OpenRequested -= OpenParchment;
            _reader.Opened -= SuspendForParchment;
            _reader.Closed -= RestoreAfterParchment;
            _reader.Collected -= RefreshCollection;
            _input.InteractionRequested -= SetInteraction;
            _input.VisibilityToggleRequested -= ToggleVisibility;
            _input.SettingsRequested -= ToggleSettings;
            _input.BackRequested -= GoBack;
            _ui.SettingsButton.onClick.RemoveListener(ToggleSettings);
            _ui.SettingsClose.onClick.RemoveListener(ToggleSettings);
            _ui.Brightness.onValueChanged.RemoveListener(_settings.SetBrightness);
            _ui.Volume.onValueChanged.RemoveListener(_settings.SetVolume);
            _ui.Vibration.onClick.RemoveListener(_settings.ToggleVibration);
            _ui.Save.onClick.RemoveListener(SaveSettings);
            _ui.Quit.onClick.RemoveListener(ShowQuit);
            _ui.QuitCancel.onClick.RemoveListener(CloseQuit);
            _ui.QuitConfirm.onClick.RemoveListener(ConfirmQuit);
            _input.Disable();
        }
        private void Update() { if (!_reader.IsOpen && Time.frameCount > _inputResumeFrame) { _input.Tick(_visible); } }

        public void SetAvailable(bool available)
        {
            // OnEnable/OnDisable manage input and subscriptions together with the visible hierarchy.
            gameObject.SetActive(available);
        }

        public void SetLearningState(LearningProgressSnapshot snapshot) => _learning.Refresh(snapshot);
        public void ShowTravelStarted() => _learning.ShowTravelStarted();
        public void ShowTravelFailed() => _learning.ShowTravelFailed();
        public void ShowTravelCompleted() => _learning.ShowTravelCompleted();
        public void SetMapData(TabletMapData data) { _mapData = data; _ui.Minimap.SetMapData(data); }
        public void ClearMapData(TabletMapData data) { if (_mapData == data) { SetMapData(null); } }
        private void RequestStage(LearningStageId stage)
        {
            StageMoveRequested?.Invoke(stage);
            _input.ResetFocus(_ui.SelectedApp);
        }
        private void ShowApp(int index) { _ui.ShowApp(index); _input.ResetFocus(index); }
        private void SetInteraction(bool active) => _input.SetInteraction(active, _ui.SelectedApp);
        private void ToggleVisibility()
        {
            _visible = !_visible;
            if (!_visible && _detail.IsOpen) { CloseQuit(); }
            _visualRoot.SetActive(_visible);
            if (!_visible) { SetInteraction(false); }
        }

        private void ToggleSettings()
        {
            _detail.Close();
            _settings.SetVisible(!_settings.IsOpen);
            _input.SetInteraction(true, _settings.IsOpen ? 0 : _ui.SelectedApp);
        }
        private void GoBack()
        {
            if (_detail.IsOpen) { CloseQuit(); }
            else if (_input.IsAdjustingSlider) { _input.StopAdjustingSlider(); }
            else if (_settings.IsOpen) { ToggleSettings(); }
            else { _input.CancelInteraction(_ui.SelectedApp); }
        }
        private void SaveSettings() => _settings.Save(_parchment.SelectedIndex);
        private void ShowQuit() { _detail.Show(); _input.ResetFocus(0); }
        private void CloseQuit() { _detail.Close(); _input.ResetFocus(0); }
        private void ConfirmQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OpenParchment(int index, Pose origin) => _reader.OpenCollected(index, _canvas.worldCamera, origin);
        private void RefreshCollection(int index) => _parchment.RefreshInventory();
        private void SuspendForParchment()
        {
            _interactionBeforeReader = _input.IsInteracting;
            _focusBeforeReader = _input.FocusIndex;
            _input.Disable();
        }
        private void RestoreAfterParchment()
        {
            _inputResumeFrame = Time.frameCount;
            _input.SetInteraction(_interactionBeforeReader, _focusBeforeReader);
            _input.Enable();
        }
        private void OnDestroy() { _input?.Dispose(); _resources?.Dispose(); }
    }
}
