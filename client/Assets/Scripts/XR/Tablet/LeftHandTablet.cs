using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XR
{
    public sealed class LeftHandTablet : MonoBehaviour
    {
        private const float CanvasWidth = 500f;
        private const float CanvasHeight = 340f;
        private const float DefaultProgress = 0.93f;
        private const string SaveBrightnessKey = "Tablet.Brightness";
        private const string SaveVolumeKey = "Tablet.Volume";
        private const string SaveVibrationKey = "Tablet.Vibration";
        private const string SaveStageKey = "Tablet.Stage";
        private const string SaveInventoryKey = "Tablet.Inventory";

        private static readonly Color SurfaceColor = new Color(0.025f, 0.033f, 0.047f, 1f);
        private static readonly Color CardColor = new Color(0.067f, 0.082f, 0.11f, 0.96f);
        private static readonly Color ElevatedColor = new Color(0.105f, 0.125f, 0.158f, 0.98f);
        private static readonly Color AccentColor = new Color(0.22f, 0.79f, 0.88f, 1f);
        private static readonly Color AccentSoftColor = new Color(0.12f, 0.43f, 0.49f, 0.8f);
        private static readonly Color WarningColor = new Color(1f, 0.78f, 0.24f, 1f);
        private static readonly Color DangerColor = new Color(1f, 0.27f, 0.31f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.97f, 1f, 1f);
        private static readonly Color MutedTextColor = new Color(0.62f, 0.69f, 0.76f, 1f);

        [SerializeField, HideInInspector] private Text _message;
        [SerializeField] private Font _font;
        [SerializeField] private Transform[] _stageStartPoints;
        [SerializeField] private StageMoveEvent _stageMoveRequested = new StageMoveEvent();
        [SerializeField] private UnityEvent _saveRequested = new UnityEvent();
        [SerializeField] private UnityEvent _exitRequested = new UnityEvent();

        private readonly string[] _itemNames = { "렌즈", "필터", "칩", "센서", "리포트" };
        private readonly string[] _itemCodes = { "L", "F", "C", "S", "R" };
        private readonly string[] _stageNames =
        {
            "1. 튜토리얼",
            "2. 오염 확인",
            "3. 필터 관찰",
            "4. 커널 적용",
            "5. 마무리"
        };
        private readonly TabletTask[] _tasks =
        {
            new TabletTask("오염 픽셀 위치 확인", true),
            new TabletTask("RGB 채널별 값 비교", false),
            new TabletTask("필터 커널 적용", false)
        };

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Font _resolvedFont;
        private Sprite _roundedSprite;
        private Sprite _circleSprite;
        private InputAction _xButton;
        private InputAction _yButton;
        private InputAction _trigger;
        private XRRayInteractor _rightRay;
        private XRInputButtonReader _previousUIPress;
        private XRInputButtonReader _uiPress;
        private bool _previousUIInteraction;
        private bool _previousHoveredActivate;
        private bool _previousHitClosestOnly;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedUIInputModule;
        private Text _stageLabel;
        private Text _progressPercent;
        private Image _progressFill;
        private Text _feedbackTitle;
        private Text _feedbackBody;
        private Text _selectedItemText;
        private readonly List<Image> _inventoryGlows = new List<Image>();
        private readonly List<Text> _taskTexts = new List<Text>();
        private readonly List<Image> _taskChecks = new List<Image>();
        private GameObject _settingsOverlay;
        private Text _brightnessValue;
        private Text _volumeValue;
        private Text _vibrationValue;
        private Text _settingsStageLabel;
        private Image _vibrationIndicator;
        private float _brightness = 0.8f;
        private float _volume = 0.49f;
        private int _selectedItemIndex;
        private int _selectedStageIndex = 1;
        private bool _isVibrationEnabled = true;
        private int _completedTaskIndex = 1;

        private void Awake()
        {
            _xButton = new InputAction("Tablet X", InputActionType.Button,
                "<XRController>{LeftHand}/primaryButton");
            _yButton = new InputAction("Tablet Y", InputActionType.Button,
                "<XRController>{LeftHand}/secondaryButton");
            _trigger = new InputAction("Tablet Trigger", InputActionType.Button,
                "<XRController>{LeftHand}/triggerPressed");
            ResolveFont();
            LoadSettings();
            BuildInterface();
            RefreshAll();
        }

        private void OnEnable()
        {
            _xButton.Enable();
            _yButton.Enable();
            _trigger.Enable();
            PrepareUIInput();
            BindRightHand();
        }

        private void Update()
        {
            if (_xButton.WasPressedThisFrame())
            {
                SelectNextItem();
            }
            if (_yButton.WasPressedThisFrame())
            {
                ToggleSettings();
            }
            if (_trigger.WasPressedThisFrame())
            {
                ConfirmCurrentTask();
            }
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

        private void LoadSettings()
        {
            _brightness = PlayerPrefs.GetFloat(SaveBrightnessKey, _brightness);
            _volume = PlayerPrefs.GetFloat(SaveVolumeKey, _volume);
            _isVibrationEnabled = PlayerPrefs.GetInt(SaveVibrationKey, _isVibrationEnabled ? 1 : 0) == 1;
            _selectedStageIndex = Mathf.Clamp(PlayerPrefs.GetInt(SaveStageKey, _selectedStageIndex), 0, _stageNames.Length - 1);
            _selectedItemIndex = Mathf.Clamp(PlayerPrefs.GetInt(SaveInventoryKey, 0), 0, _itemNames.Length - 1);
            AudioListener.volume = _volume;
            Screen.brightness = _brightness;
        }

        private void BuildInterface()
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas == null)
            {
                GameObject canvasObject = new GameObject("Tablet Canvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(transform, false);
                _canvas = canvasObject.GetComponent<Canvas>();
            }
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;
            if (_canvas.GetComponent<GraphicRaycaster>() == null)
            {
                _canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            if (_canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                _canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            _canvasRect = _canvas.GetComponent<RectTransform>();
            _canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            _canvasRect.localScale = Vector3.one * 0.0005f;
            CreateRuntimeSprites();
            ClearCanvas();

            Image background = CreateImage(_canvasRect, "Tablet Surface", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), SurfaceColor);
            AddOutline(background, new Color(1f, 1f, 1f, 0.04f), new Vector2(1f, 1f));

            BuildHeader();
            BuildInventory();
            BuildMainFeedback();
            BuildRightStatus();
            BuildBottomCards();
            BuildSettingsOverlay();
        }

        private void CreateRuntimeSprites()
        {
            _roundedSprite = CreateRoundedSprite(64, 14);
            _circleSprite = CreateRoundedSprite(64, 32);
        }

        private void ClearCanvas()
        {
            for (int index = _canvasRect.childCount - 1; index >= 0; index--)
            {
                Destroy(_canvasRect.GetChild(index).gameObject);
            }
            _inventoryGlows.Clear();
            _taskTexts.Clear();
            _taskChecks.Clear();
        }

        private void BuildHeader()
        {
            Image header = CreateImage(_canvasRect, "Header", new Vector2(0f, 145f),
                new Vector2(468f, 38f), new Color(0.05f, 0.063f, 0.084f, 0.98f));
            AddOutline(header, new Color(1f, 1f, 1f, 0.05f), new Vector2(1f, 1f));
            CreateText(header.rectTransform, "Team Title", "케디헌 XR DISPLAY", new Vector2(-204f, 7f),
                new Vector2(180f, 18f), 15, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            CreateText(header.rectTransform, "Lesson Title", "픽셀 구조 · CNN 필터 교육 태블릿", new Vector2(-190f, -10f),
                new Vector2(210f, 15f), 9, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft);

            Image statusChip = CreateImage(header.rectTransform, "Progress Chip", new Vector2(176f, 0f),
                new Vector2(76f, 24f), new Color(0.12f, 0.17f, 0.21f, 0.95f));
            _progressPercent = CreateText(statusChip.rectTransform, "Progress Percent", "93%", Vector2.zero,
                new Vector2(62f, 18f), 13, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            AddOutline(statusChip, new Color(0.45f, 0.91f, 1f, 0.18f), new Vector2(1f, 1f));
        }

        private void BuildInventory()
        {
            Image panel = CreateImage(_canvasRect, "Inventory Panel", new Vector2(-220f, 18f),
                new Vector2(56f, 228f), CardColor);
            CreateText(panel.rectTransform, "Inventory Label", "ITEM", new Vector2(0f, 97f),
                new Vector2(48f, 14f), 8, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleCenter);
            for (int index = 0; index < _itemNames.Length; index++)
            {
                float y = 67f - index * 38f;
                Image glow = CreateImage(panel.rectTransform, "Item Glow " + index, new Vector2(0f, y),
                    new Vector2(45f, 33f), new Color(1f, 0.82f, 0.19f, 0.16f));
                AddOutline(glow, new Color(1f, 0.86f, 0.2f, 0.65f), new Vector2(2.4f, 2.4f));
                _inventoryGlows.Add(glow);

                Image slot = CreateImage(panel.rectTransform, "Item Slot " + index, new Vector2(0f, y),
                    new Vector2(37f, 27f), ElevatedColor);
                AddOutline(slot, new Color(1f, 1f, 1f, 0.08f), new Vector2(1f, 1f));
                CreateText(slot.rectTransform, "Item Code " + index, _itemCodes[index], Vector2.zero,
                    new Vector2(32f, 22f), 13, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            }
            _selectedItemText = CreateText(panel.rectTransform, "Selected Item", "", new Vector2(0f, -101f),
                new Vector2(50f, 14f), 8, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleCenter);
        }

        private void BuildMainFeedback()
        {
            Image panel = CreateImage(_canvasRect, "Concept Feedback Panel", new Vector2(-50f, 28f),
                new Vector2(260f, 178f), CardColor);
            AddOutline(panel, new Color(0.45f, 0.91f, 1f, 0.1f), new Vector2(1f, 1f));
            CreateText(panel.rectTransform, "Concept Caption", "개념 설명 · 실시간 피드백", new Vector2(-78f, 68f),
                new Vector2(150f, 16f), 10, FontStyle.Bold, AccentColor, TextAnchor.MiddleLeft);
            _feedbackTitle = CreateText(panel.rectTransform, "Feedback Title", "", new Vector2(-3f, 40f),
                new Vector2(220f, 26f), 18, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _feedbackBody = CreateText(panel.rectTransform, "Feedback Body", "", new Vector2(0f, -18f),
                new Vector2(218f, 78f), 12, FontStyle.Normal, new Color(0.82f, 0.89f, 0.94f, 1f),
                TextAnchor.UpperLeft);

            Image action = CreateImage(panel.rectTransform, "Action Pill", new Vector2(-55f, -72f),
                new Vector2(126f, 25f), AccentSoftColor);
            CreateText(action.rectTransform, "Action Text", "Trigger: 임무 확인", Vector2.zero,
                new Vector2(112f, 17f), 9, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
        }

        private void BuildRightStatus()
        {
            Image minimap = CreateImage(_canvasRect, "Mini Map", new Vector2(180f, 54f),
                new Vector2(128f, 128f), new Color(0.018f, 0.022f, 0.028f, 1f));
            AddOutline(minimap, new Color(0.45f, 0.91f, 1f, 0.12f), new Vector2(1f, 1f));
            CreateText(minimap.rectTransform, "Map Label", "MINI MAP", new Vector2(-38f, 51f),
                new Vector2(68f, 12f), 8, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleLeft);
            BuildMapLines(minimap.rectTransform);

            Image progressCard = CreateImage(_canvasRect, "Progress Card", new Vector2(180f, -63f),
                new Vector2(128f, 75f), CardColor);
            CreateText(progressCard.rectTransform, "Progress Label", "전체 진행률", new Vector2(-28f, 24f),
                new Vector2(62f, 13f), 8, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleLeft);
            CreateText(progressCard.rectTransform, "Progress Right Number", "93%", new Vector2(43f, 23f),
                new Vector2(44f, 18f), 13, FontStyle.Bold, TextColor, TextAnchor.MiddleRight);
            Image progressBackground = CreateImage(progressCard.rectTransform, "Progress Background", new Vector2(0f, -6f),
                new Vector2(104f, 11f), new Color(0.12f, 0.15f, 0.18f, 1f));
            _progressFill = CreateImage(progressBackground.rectTransform, "Progress Fill", new Vector2(-3.5f, 0f),
                new Vector2(96f, 7f), AccentColor);
            _progressFill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.anchoredPosition = new Vector2(4f, 0f);
            CreateText(progressCard.rectTransform, "Progress Hint", "오염 픽셀 복구 단계", new Vector2(0f, -25f),
                new Vector2(108f, 13f), 8, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleCenter);
        }

        private void BuildMapLines(RectTransform parent)
        {
            CreateLine(parent, "Road A", new Vector2(-21f, 16f), new Vector2(92f, 2f), 23f, new Color(0.32f, 0.38f, 0.42f, 1f));
            CreateLine(parent, "Road B", new Vector2(3f, -7f), new Vector2(102f, 2f), -18f, new Color(0.28f, 0.33f, 0.37f, 1f));
            CreateLine(parent, "Road C", new Vector2(-34f, -29f), new Vector2(72f, 2f), 8f, new Color(0.35f, 0.41f, 0.45f, 1f));
            CreateLine(parent, "Road D", new Vector2(23f, 20f), new Vector2(2f, 70f), -8f, new Color(0.27f, 0.32f, 0.36f, 1f));
            CreateLine(parent, "Road E", new Vector2(-41f, 3f), new Vector2(2f, 76f), 11f, new Color(0.31f, 0.36f, 0.4f, 1f));
            CreateLine(parent, "Route", new Vector2(-4f, 2f), new Vector2(56f, 3f), 16f, new Color(0.96f, 0.78f, 0.24f, 0.85f));
            Image user = CreateImage(parent, "User Marker", new Vector2(-8f, -4f), new Vector2(12f, 12f), DangerColor);
            user.sprite = _circleSprite;
            Image target = CreateImage(parent, "Target Marker", new Vector2(22f, 11f), new Vector2(9f, 9f), WarningColor);
            target.sprite = _circleSprite;
            CreateText(parent, "Map Goal", "GET BACK TO ALTAR", new Vector2(0f, -52f),
                new Vector2(112f, 12f), 7, FontStyle.Bold, new Color(0.86f, 0.9f, 0.93f, 1f), TextAnchor.MiddleCenter);
        }

        private void BuildBottomCards()
        {
            Image stage = CreateImage(_canvasRect, "Current Stage Card", new Vector2(-157f, -124f),
                new Vector2(128f, 66f), CardColor);
            CreateText(stage.rectTransform, "Stage Label", "현재 단계", new Vector2(-36f, 19f),
                new Vector2(58f, 13f), 8, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleLeft);
            _stageLabel = CreateText(stage.rectTransform, "Current Stage", "", new Vector2(0f, -7f),
                new Vector2(106f, 31f), 13, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);

            Image tasks = CreateImage(_canvasRect, "Task Card", new Vector2(0f, -124f),
                new Vector2(170f, 66f), CardColor);
            CreateText(tasks.rectTransform, "Task Label", "해야 하는 임무", new Vector2(-48f, 20f),
                new Vector2(78f, 13f), 8, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleLeft);
            for (int index = 0; index < _tasks.Length; index++)
            {
                BuildTaskRow(tasks.rectTransform, index, 6f - index * 17f);
            }

            CreateTextButton(_canvasRect, "Settings Button", "설정", new Vector2(150f, -124f),
                new Vector2(48f, 66f), ToggleSettings, AccentSoftColor);
            CreateTextButton(_canvasRect, "Exit Button", "나가기", new Vector2(213f, -124f),
                new Vector2(58f, 66f), RequestExit, new Color(0.24f, 0.1f, 0.11f, 0.95f));
        }

        private void BuildTaskRow(RectTransform parent, int index, float y)
        {
            Image check = CreateImage(parent, "Task Check " + index, new Vector2(-72f, y),
                new Vector2(9f, 9f), new Color(0.1f, 0.13f, 0.16f, 1f));
            AddOutline(check, new Color(1f, 1f, 1f, 0.2f), new Vector2(1f, 1f));
            _taskChecks.Add(check);
            _taskTexts.Add(CreateText(parent, "Task Text " + index, _tasks[index].Label, new Vector2(5f, y),
                new Vector2(136f, 13f), 8, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft));
        }

        private void BuildSettingsOverlay()
        {
            _settingsOverlay = new GameObject("Settings Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _settingsOverlay.layer = _canvas.gameObject.layer;
            _settingsOverlay.transform.SetParent(_canvasRect, false);
            RectTransform overlayRect = _settingsOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            Image overlay = _settingsOverlay.GetComponent<Image>();
            overlay.color = new Color(0.04f, 0.045f, 0.052f, 0.82f);

            Image modal = CreateImage(overlayRect, "Settings Dialog", Vector2.zero,
                new Vector2(360f, 248f), new Color(0.09f, 0.105f, 0.132f, 0.98f));
            AddOutline(modal, new Color(0.78f, 0.86f, 0.92f, 0.14f), new Vector2(1f, 1f));
            CreateText(modal.rectTransform, "Settings Title", "설정", new Vector2(-150f, 100f),
                new Vector2(60f, 22f), 17, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _settingsStageLabel = CreateText(modal.rectTransform, "Settings Stage", "", new Vector2(-32f, 101f),
                new Vector2(190f, 18f), 10, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft);

            CreateTextButton(modal.rectTransform, "Save Button", "저장하기", new Vector2(126f, 98f),
                new Vector2(78f, 26f), SaveSettings, AccentSoftColor);
            BuildStageMovePanel(modal.rectTransform);
            BuildSettingSlider(modal.rectTransform, "Brightness", "밝기", new Vector2(-7f, 4f), _brightness,
                value => SetBrightness(value), out _brightnessValue);
            BuildSettingSlider(modal.rectTransform, "Volume", "볼륨", new Vector2(-7f, -43f), _volume,
                value => SetVolume(value), out _volumeValue);

            Image vibration = CreateImage(modal.rectTransform, "Vibration Row", new Vector2(-74f, -91f),
                new Vector2(164f, 30f), ElevatedColor);
            CreateText(vibration.rectTransform, "Vibration Label", "진동", new Vector2(-46f, 0f),
                new Vector2(45f, 15f), 10, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _vibrationIndicator = CreateImage(vibration.rectTransform, "Vibration Indicator", new Vector2(45f, 0f),
                new Vector2(40f, 18f), AccentSoftColor);
            _vibrationValue = CreateText(_vibrationIndicator.rectTransform, "Vibration Value", "", Vector2.zero,
                new Vector2(35f, 13f), 8, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            Button vibrationButton = vibration.gameObject.AddComponent<Button>();
            vibrationButton.targetGraphic = vibration;
            vibrationButton.onClick.AddListener(ToggleVibration);

            CreateTextButton(modal.rectTransform, "Close Settings", "닫기", new Vector2(87f, -91f),
                new Vector2(64f, 30f), ToggleSettings, new Color(0.15f, 0.17f, 0.2f, 1f));
            CreateTextButton(modal.rectTransform, "Exit Settings", "나가기", new Vector2(155f, -91f),
                new Vector2(64f, 30f), RequestExit, new Color(0.3f, 0.11f, 0.12f, 1f));
            _settingsOverlay.SetActive(false);
        }

        private void BuildStageMovePanel(RectTransform parent)
        {
            Image panel = CreateImage(parent, "Stage Move Panel", new Vector2(-10f, 55f),
                new Vector2(316f, 46f), new Color(0.075f, 0.09f, 0.115f, 1f));
            CreateText(panel.rectTransform, "Stage Move Label", "단계 이동", new Vector2(-125f, 11f),
                new Vector2(60f, 13f), 8, FontStyle.Bold, MutedTextColor, TextAnchor.MiddleLeft);
            for (int index = 0; index < _stageNames.Length; index++)
            {
                int stageIndex = index;
                float x = -109f + index * 54f;
                CreateTextButton(panel.rectTransform, "Stage Button " + index, (index + 1).ToString(), new Vector2(x, -8f),
                    new Vector2(40f, 22f), () => MoveToStage(stageIndex), new Color(0.12f, 0.145f, 0.18f, 1f));
            }
            CreateText(panel.rectTransform, "Stage Move Hint", "버튼을 누르면 해당 단계 시작 위치로 이동", new Vector2(83f, 12f),
                new Vector2(150f, 12f), 7, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
        }

        private void BuildSettingSlider(RectTransform parent, string name, string label, Vector2 position,
            float initialValue, UnityAction<float> onValueChanged, out Text valueText)
        {
            Image row = CreateImage(parent, name + " Row", position, new Vector2(316f, 34f), ElevatedColor);
            CreateText(row.rectTransform, name + " Label", label, new Vector2(-128f, 0f),
                new Vector2(44f, 15f), 10, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Slider slider = row.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            Image background = CreateImage(row.rectTransform, name + " Slider Background", new Vector2(4f, 0f),
                new Vector2(156f, 6f), new Color(0.18f, 0.2f, 0.23f, 1f));
            Image fill = CreateImage(background.rectTransform, name + " Slider Fill", new Vector2(0f, 0f),
                new Vector2(148f, 4f), AccentColor);
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(4f, 0f);
            Image handle = CreateImage(background.rectTransform, name + " Slider Handle", new Vector2(0f, 0f),
                new Vector2(12f, 12f), TextColor);
            handle.sprite = _circleSprite;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(initialValue);
            slider.onValueChanged.AddListener(onValueChanged);
            valueText = CreateText(row.rectTransform, name + " Value", "", new Vector2(128f, 0f),
                new Vector2(42f, 15f), 10, FontStyle.Bold, TextColor, TextAnchor.MiddleRight);
        }

        private Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = _canvas.gameObject.layer;
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = imageObject.GetComponent<Image>();
            image.sprite = _roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size,
            int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.layer = _canvas.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = _resolvedFont;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button CreateTextButton(Transform parent, string name, string label, Vector2 position, Vector2 size,
            UnityAction onClick, Color color)
        {
            Image image = CreateImage(parent, name, position, size, color);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.78f, 0.9f, 1f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            CreateText(image.rectTransform, name + " Label", label, Vector2.zero,
                new Vector2(size.x - 8f, size.y - 8f), 9, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            return button;
        }

        private void CreateLine(RectTransform parent, string name, Vector2 position, Vector2 size, float angle, Color color)
        {
            Image line = CreateImage(parent, name, position, size, color);
            line.sprite = _roundedSprite;
            line.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private Sprite CreateRoundedSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Tablet Rounded UI";
            texture.wrapMode = TextureWrapMode.Clamp;
            Color white = Color.white;
            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, x - (size - radius - 1), 0);
                    float dy = Mathf.Max(radius - y, y - (size - radius - 1), 0);
                    bool isInside = dx * dx + dy * dy <= radius * radius;
                    texture.SetPixel(x, y, isInside ? white : clear);
                }
            }
            texture.Apply();
            Vector4 border = Vector4.one * radius;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        private void RefreshAll()
        {
            RefreshInventory();
            RefreshTasks();
            RefreshStage();
            RefreshProgress();
            RefreshSettingsValues();
            UpdateFeedback("필터 렌즈 준비 완료",
                "선택한 아이템으로 오염된 픽셀을 관찰하세요.\n잘못된 조작이나 다음 행동 안내는 이 영역에 표시됩니다.");
        }

        private void RefreshInventory()
        {
            for (int index = 0; index < _inventoryGlows.Count; index++)
            {
                _inventoryGlows[index].gameObject.SetActive(index == _selectedItemIndex);
            }
            if (_selectedItemText != null)
            {
                _selectedItemText.text = _itemNames[_selectedItemIndex];
            }
        }

        private void RefreshTasks()
        {
            for (int index = 0; index < _tasks.Length; index++)
            {
                bool isDone = _tasks[index].IsCompleted;
                _taskChecks[index].color = isDone ? AccentColor : new Color(0.1f, 0.13f, 0.16f, 1f);
                _taskTexts[index].color = isDone ? MutedTextColor : TextColor;
            }
        }

        private void RefreshStage()
        {
            string stageName = _stageNames[_selectedStageIndex];
            if (_stageLabel != null)
            {
                _stageLabel.text = stageName;
            }
            if (_settingsStageLabel != null)
            {
                _settingsStageLabel.text = "현재 위치: " + stageName;
            }
        }

        private void RefreshProgress()
        {
            int percent = Mathf.RoundToInt(DefaultProgress * 100f);
            if (_progressPercent != null)
            {
                _progressPercent.text = percent + "%";
            }
            if (_progressFill != null)
            {
                _progressFill.rectTransform.sizeDelta = new Vector2(96f * DefaultProgress, 7f);
            }
        }

        private void RefreshSettingsValues()
        {
            if (_brightnessValue != null)
            {
                _brightnessValue.text = Mathf.RoundToInt(_brightness * 100f) + "%";
            }
            if (_volumeValue != null)
            {
                _volumeValue.text = Mathf.RoundToInt(_volume * 100f) + "%";
            }
            if (_vibrationValue != null)
            {
                _vibrationValue.text = _isVibrationEnabled ? "ON" : "OFF";
            }
            if (_vibrationIndicator != null)
            {
                _vibrationIndicator.color = _isVibrationEnabled
                    ? AccentSoftColor
                    : new Color(0.19f, 0.2f, 0.22f, 1f);
            }
        }

        private void SelectNextItem()
        {
            _selectedItemIndex = (_selectedItemIndex + 1) % _itemNames.Length;
            RefreshInventory();
            UpdateFeedback(_itemNames[_selectedItemIndex] + " 선택됨",
                "선택된 아이템은 노란 테두리로 강조됩니다.\n오른손 레이로 버튼을 누르거나 Trigger로 다음 임무를 확인하세요.");
        }

        private void ToggleSettings()
        {
            if (_settingsOverlay == null)
            {
                return;
            }
            bool isOpen = !_settingsOverlay.activeSelf;
            _settingsOverlay.SetActive(isOpen);
            UpdateFeedback(isOpen ? "설정 패널 열림" : "설정 패널 닫힘",
                isOpen ? "단계 이동, 저장, 밝기, 볼륨, 진동 설정을 조정할 수 있습니다." :
                "학습 화면으로 돌아왔습니다. 현재 임무를 계속 진행하세요.");
        }

        private void ConfirmCurrentTask()
        {
            if (_completedTaskIndex < _tasks.Length)
            {
                _tasks[_completedTaskIndex].IsCompleted = true;
                _completedTaskIndex++;
                RefreshTasks();
                UpdateFeedback("임무 확인 완료",
                    "체크리스트가 갱신되었습니다.\n다음 단계에서는 RGB 채널과 필터 커널의 영향을 비교하세요.");
                return;
            }
            UpdateFeedback("모든 현재 임무 확인됨",
                "진행률은 전체 교육 플로우 기준 93%입니다.\n설정에서 저장하거나 단계 이동을 선택할 수 있습니다.");
        }

        private void MoveToStage(int stageIndex)
        {
            _selectedStageIndex = Mathf.Clamp(stageIndex, 0, _stageNames.Length - 1);
            RefreshStage();
            _stageMoveRequested.Invoke(_selectedStageIndex);

            Transform target = null;
            if (_stageStartPoints != null && _selectedStageIndex < _stageStartPoints.Length)
            {
                target = _stageStartPoints[_selectedStageIndex];
            }
            if (target != null)
            {
                XROrigin origin = GetComponentInParent<XROrigin>();
                Transform rig = origin != null ? origin.transform : transform.root;
                rig.SetPositionAndRotation(target.position, target.rotation);
                UpdateFeedback("단계 이동 완료", _stageNames[_selectedStageIndex] + " 시작 지점으로 이동했습니다.");
            }
            else
            {
                UpdateFeedback("단계 이동 선택됨",
                    _stageNames[_selectedStageIndex] + " 시작 지점이 아직 연결되지 않았습니다.\n씬에서 Stage Start Transform을 연결하면 실제 이동이 실행됩니다.");
            }
        }

        private void SetBrightness(float value)
        {
            _brightness = Mathf.Clamp01(value);
            Screen.brightness = _brightness;
            RefreshSettingsValues();
        }

        private void SetVolume(float value)
        {
            _volume = Mathf.Clamp01(value);
            AudioListener.volume = _volume;
            RefreshSettingsValues();
        }

        private void ToggleVibration()
        {
            _isVibrationEnabled = !_isVibrationEnabled;
            RefreshSettingsValues();
            UpdateFeedback("진동 " + (_isVibrationEnabled ? "사용" : "해제"),
                "피드백 진동 설정이 변경되었습니다.");
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(SaveBrightnessKey, _brightness);
            PlayerPrefs.SetFloat(SaveVolumeKey, _volume);
            PlayerPrefs.SetInt(SaveVibrationKey, _isVibrationEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SaveStageKey, _selectedStageIndex);
            PlayerPrefs.SetInt(SaveInventoryKey, _selectedItemIndex);
            PlayerPrefs.Save();
            _saveRequested.Invoke();
            UpdateFeedback("저장 완료",
                "현재 단계와 태블릿 설정을 저장했습니다.\n다음 접속 때 같은 상태로 복원됩니다.");
        }

        private void RequestExit()
        {
            _exitRequested.Invoke();
            UpdateFeedback("나가기 요청",
                "연결된 종료 이벤트가 있으면 실행됩니다.\n아직 연결 전이면 팀의 씬 전환 로직에 이 버튼을 연결하세요.");
        }

        private void UpdateFeedback(string title, string body)
        {
            if (_feedbackTitle != null)
            {
                _feedbackTitle.text = title;
            }
            if (_feedbackBody != null)
            {
                _feedbackBody.text = body;
            }
        }

        private void PrepareUIInput()
        {
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Tablet UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedUIInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void BindRightHand()
        {
            foreach (XRRayInteractor ray in FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None))
            {
                if (ray.handedness == InteractorHandedness.Right && ray.isActiveAndEnabled)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null)
            {
                return;
            }
            _previousUIPress = _rightRay.uiPressInput;
            _previousUIInteraction = _rightRay.enableUIInteraction;
            _previousHoveredActivate = _rightRay.allowHoveredActivate;
            _previousHitClosestOnly = _rightRay.hitClosestOnly;
            _uiPress = new XRInputButtonReader("Tablet UI", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = new InputAction("Tablet UI Press", InputActionType.Button,
                    "<XRController>{RightHand}/triggerPressed"),
                inputActionValue = new InputAction("Tablet UI Value", InputActionType.Value,
                    "<XRController>{RightHand}/trigger")
            };
            _rightRay.uiPressInput = _uiPress;
            _rightRay.enableUIInteraction = true;
            _rightRay.allowHoveredActivate = true;
            _rightRay.hitClosestOnly = true;
        }

        private void OnDisable()
        {
            _xButton?.Disable();
            _yButton?.Disable();
            _trigger?.Disable();
            if (_rightRay != null)
            {
                _rightRay.uiPressInput = _previousUIPress;
                _rightRay.enableUIInteraction = _previousUIInteraction;
                _rightRay.allowHoveredActivate = _previousHoveredActivate;
                _rightRay.hitClosestOnly = _previousHitClosestOnly;
                _rightRay = null;
            }
            if (_uiPress != null)
            {
                _uiPress.inputActionPerformed.Dispose();
                _uiPress.inputActionValue.Dispose();
                _uiPress = null;
            }
            if (_ownedEventSystem != null)
            {
                Destroy(_ownedEventSystem);
                _ownedEventSystem = null;
            }
            if (_addedUIInputModule != null)
            {
                Destroy(_addedUIInputModule);
                _addedUIInputModule = null;
            }
        }

        private void OnDestroy()
        {
            _xButton?.Dispose();
            _yButton?.Dispose();
            _trigger?.Dispose();
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

        [Serializable]
        private sealed class StageMoveEvent : UnityEvent<int>
        {
        }
    }
}
