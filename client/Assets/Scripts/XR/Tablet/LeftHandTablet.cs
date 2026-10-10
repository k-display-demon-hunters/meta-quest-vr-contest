using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XR
{
    [DefaultExecutionOrder(-50)]
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

        private static readonly Color SurfaceColor = new Color(0.94f, 0.95f, 0.96f, 1f);
        private static readonly Color CardColor = Color.white;
        private static readonly Color ElevatedColor = new Color(0.88f, 0.91f, 0.93f, 1f);
        private static readonly Color AccentColor = new Color(0.02f, 0.48f, 0.56f, 1f);
        private static readonly Color AccentSoftColor = new Color(0.82f, 0.93f, 0.94f, 1f);
        private static readonly Color WarningColor = new Color(1f, 0.78f, 0.24f, 1f);
        private static readonly Color DangerColor = new Color(1f, 0.27f, 0.31f, 1f);
        private static readonly Color TextColor = new Color(0.1f, 0.13f, 0.15f, 1f);
        private static readonly Color MutedTextColor = new Color(0.34f, 0.39f, 0.43f, 1f);

        [SerializeField, HideInInspector] private Text _message;
        [SerializeField] private Font _font;
        [SerializeField] private Transform[] _stageStartPoints;
        [SerializeField] private StageMoveEvent _stageMoveRequested = new StageMoveEvent();
        [SerializeField] private UnityEvent _saveRequested = new UnityEvent();
        [SerializeField] private UnityEvent _exitRequested = new UnityEvent();

        private readonly string[] _itemNames = { "렌즈", "필터", "칩", "센서", "리포트" };
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
        private InputAction _stick;
        private ContinuousMoveProvider _moveProvider;
        private XRInputValueReader<Vector2> _previousMoveInput;
        private readonly XRInputValueReader<Vector2> _blockedMoveInput =
            new XRInputValueReader<Vector2>("Tablet Focus", XRInputValueReader.InputSourceMode.ManualValue);
        private readonly List<FocusTarget> _mainTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _settingsTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _stageTargets = new List<FocusTarget>();
        private List<FocusTarget> _buildingTargets;
        private GameObject _stageMenu;
        private Text _saveLabel;
        private bool _movementCaptured;
        private bool _isInteracting;
        private bool _isAdjustingSlider;
        private bool _stickMustCenter;
        private Vector2 _lastDirection;
        private float _nextRepeatTime;
        private int _focusIndex;

        private Text _stageLabel;
        private Text _progressPercent;
        private Image _progressFill;
        private Text _feedbackTitle;
        private Text _feedbackBody;
        private readonly List<Image> _inventoryGlows = new List<Image>();
        private readonly List<Text> _taskTexts = new List<Text>();
        private readonly List<Image> _taskChecks = new List<Image>();
        private GameObject _settingsOverlay;
        private Text _brightnessValue;
        private Text _volumeValue;
        private Text _vibrationValue;
        private Text _settingsStageLabel;
        private float _brightness = 0.8f;
        private float _volume = 0.49f;
        private int _selectedItemIndex;
        private int _selectedStageIndex;
        private bool _isVibrationEnabled = true;
        private int _completedTaskIndex = 1;

        private List<FocusTarget> ActiveTargets => _stageMenu.activeSelf ? _stageTargets :
            _settingsOverlay.activeSelf ? _settingsTargets : _mainTargets;

        private void Awake()
        {
            _xButton = new InputAction("Tablet X", InputActionType.Button,
                "<XRController>{LeftHand}/primaryButton");
            _yButton = new InputAction("Tablet Y", InputActionType.Button,
                "<XRController>{LeftHand}/secondaryButton");
            _trigger = new InputAction("Tablet Trigger", InputActionType.Button,
                "<XRController>{LeftHand}/triggerPressed");
            _stick = new InputAction("Tablet Navigation", InputActionType.Value,
                "<XRController>{LeftHand}/primary2DAxis");
            _moveProvider = GetComponentInParent<ContinuousMoveProvider>();
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
            _stick.Enable();
        }

        private void Update()
        {
            if (_trigger.WasPressedThisFrame())
            {
                SetInteraction(!_isInteracting);
            }
            else if (_yButton.WasPressedThisFrame())
            {
                GoBackOrOpenSettings();
            }
            else if (_xButton.WasPressedThisFrame())
            {
                if (!_isInteracting)
                {
                    SetInteraction(true);
                }
                if (ActiveTargets[_focusIndex].Control is Button button)
                {
                    button.onClick.Invoke();
                }
                else if (ActiveTargets[_focusIndex].Control is Slider && !_isAdjustingSlider)
                {
                    _isAdjustingSlider = true;
                    _stickMustCenter = true;
                    RefreshFocus();
                }
            }

            Vector2 stick = _stick.ReadValue<Vector2>();
            if (!_isInteracting)
            {
                // Wait for neutral so a navigation gesture cannot turn into walking.
                if (stick.magnitude < 0.25f)
                {
                    ReleaseMovement();
                }
                return;
            }
            Navigate(stick);
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
            _brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(SaveBrightnessKey, _brightness));
            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(SaveVolumeKey, _volume));
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
            foreach (GraphicRaycaster raycaster in _canvas.GetComponents<GraphicRaycaster>())
            {
                raycaster.enabled = false;
            }
            TrackedDeviceGraphicRaycaster trackedRaycaster = _canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (trackedRaycaster != null)
            {
                trackedRaycaster.enabled = false;
            }
            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.dynamicPixelsPerUnit = 4f;

            _canvasRect = _canvas.GetComponent<RectTransform>();
            _canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            _canvasRect.localScale = Vector3.one * 0.0005f;
            CreateRuntimeSprites();
            ClearCanvas();

            Image background = CreateImage(_canvasRect, "Tablet Surface", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), SurfaceColor);
            AddOutline(background, new Color(1f, 1f, 1f, 0.04f), new Vector2(1f, 1f));

            BuildHeader();
            BuildLeftStatus();
            _buildingTargets = _mainTargets;
            BuildInventory();
            BuildMainFeedback();
            CreateTextButton(_canvasRect, "Settings Button", "설정", new Vector2(207f, -115f),
                new Vector2(62f, 28f), ToggleSettings, ElevatedColor);
            CreateTextButton(_canvasRect, "Exit Button", "나가기", new Vector2(207f, -149f),
                new Vector2(62f, 28f), RequestExit, new Color(0.98f, 0.88f, 0.87f));
            BuildSettingsOverlay();
            _buildingTargets = null;
        }

        private void CreateRuntimeSprites()
        {
            _roundedSprite = CreateRoundedSprite(32, 2);
            _circleSprite = CreateRoundedSprite(64, 32);
        }

        private void ClearCanvas()
        {
            for (int index = _canvasRect.childCount - 1; index >= 0; index--)
            {
                _canvasRect.GetChild(index).gameObject.SetActive(false);
                Destroy(_canvasRect.GetChild(index).gameObject);
            }
            _inventoryGlows.Clear();
            _taskTexts.Clear();
            _taskChecks.Clear();
        }

        private void BuildHeader()
        {
            Image track = CreateImage(_canvasRect, "Overall Progress Track", new Vector2(-25f, 151f),
                new Vector2(426f, 8f), ElevatedColor);
            _progressFill = CreateImage(track.rectTransform, "Progress Fill", Vector2.zero,
                new Vector2(426f, 8f), AccentColor);
            _progressFill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.anchoredPosition = Vector2.zero;
            _progressPercent = CreateText(_canvasRect, "Progress Percent", "", new Vector2(217f, 151f),
                new Vector2(42f, 24f), 13, FontStyle.Bold, TextColor, TextAnchor.MiddleRight);

            Image header = CreateImage(_canvasRect, "Education Information", new Vector2(32f, 109f),
                new Vector2(264f, 46f), CardColor);
            CreateText(header.rectTransform, "Education Title", "픽셀 구조와 CNN 필터", new Vector2(0f, 8f),
                new Vector2(240f, 25f), 16, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            CreateText(header.rectTransform, "Team Name", "케디헌  /  XR 디스플레이 교육", new Vector2(0f, -12f),
                new Vector2(240f, 20f), 10, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft);
        }

        private void BuildLeftStatus()
        {
            Image stage = CreateImage(_canvasRect, "Current Stage", new Vector2(-174f, 109f),
                new Vector2(128f, 46f), CardColor);
            CreateText(stage.rectTransform, "Stage Caption", "현재 단계", new Vector2(0f, 12f),
                new Vector2(108f, 18f), 10, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft);
            _stageLabel = CreateText(stage.rectTransform, "Stage Name", "", new Vector2(0f, -9f),
                new Vector2(108f, 24f), 13, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);

            Image tasks = CreateImage(_canvasRect, "Tasks", new Vector2(-174f, 22f),
                new Vector2(128f, 112f), CardColor);
            CreateText(tasks.rectTransform, "Task Caption", "해야 하는 임무", new Vector2(0f, 39f),
                new Vector2(108f, 22f), 11, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            for (int index = 0; index < _tasks.Length; index++)
            {
                float y = 13f - index * 25f;
                Image check = CreateImage(tasks.rectTransform, "Task Check " + index, new Vector2(-50f, y),
                    new Vector2(10f, 10f), ElevatedColor);
                AddOutline(check, MutedTextColor, new Vector2(0.5f, 0.5f));
                _taskChecks.Add(check);
                _taskTexts.Add(CreateText(tasks.rectTransform, "Task Text " + index, _tasks[index].Label,
                    new Vector2(9f, y), new Vector2(92f, 24f), 10, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft));
            }
            BuildMinimap();
        }

        private void BuildMinimap()
        {
            Image map = CreateImage(_canvasRect, "Minimap", new Vector2(-174f, -103f),
                new Vector2(128f, 120f), new Color(0.07f, 0.09f, 0.1f));
            CreateText(map.rectTransform, "Map Caption", "현재 위치", new Vector2(0f, 44f),
                new Vector2(108f, 22f), 10, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Color road = new Color(0.37f, 0.42f, 0.44f);
            CreateImage(map.rectTransform, "Road North", new Vector2(0f, 24f), new Vector2(104f, 2f), road);
            CreateImage(map.rectTransform, "Road South", new Vector2(0f, -32f), new Vector2(104f, 2f), road);
            CreateImage(map.rectTransform, "Road West", new Vector2(-34f, -4f), new Vector2(2f, 58f), road);
            CreateImage(map.rectTransform, "Road East", new Vector2(35f, -4f), new Vector2(2f, 58f), road);
            CreateImage(map.rectTransform, "Road Center", new Vector2(0f, -6f), new Vector2(104f, 2f), road);
            CreateImage(map.rectTransform, "Route", new Vector2(7f, -6f), new Vector2(52f, 2f), WarningColor);
            CreateImage(map.rectTransform, "User Position", new Vector2(-19f, -6f),
                new Vector2(9f, 9f), DangerColor).sprite = _circleSprite;
            CreateImage(map.rectTransform, "Destination", new Vector2(33f, -6f), new Vector2(7f, 7f), WarningColor);
            CreateText(map.rectTransform, "Map Destination", "목표: 픽셀 관찰 구역", new Vector2(0f, -47f),
                new Vector2(108f, 20f), 9, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
        }

        private void BuildInventory()
        {
            for (int index = 0; index < _itemNames.Length; index++)
            {
                int itemIndex = index;
                Vector2 position = new Vector2(207f, 111f - index * 44f);
                Image glow = CreateImage(_canvasRect, "Inventory Glow " + index, position,
                    new Vector2(44f, 44f), new Color(1f, 0.8f, 0.12f, 0.22f));
                AddOutline(glow, new Color(1f, 0.78f, 0.12f, 0.55f), new Vector2(2f, 2f));
                _inventoryGlows.Add(glow);
                CreateTextButton(_canvasRect, "Inventory " + index, _itemNames[index], position,
                    new Vector2(40f, 40f), () => SelectItem(itemIndex), CardColor);
            }
        }

        private void BuildMainFeedback()
        {
            Image panel = CreateImage(_canvasRect, "Concept and Feedback", new Vector2(32f, -42f),
                new Vector2(264f, 242f), CardColor);
            CreateText(panel.rectTransform, "Concept Caption", "개념 설명 · 피드백", new Vector2(0f, 99f),
                new Vector2(232f, 24f), 11, FontStyle.Bold, AccentColor, TextAnchor.MiddleLeft);
            _feedbackTitle = CreateText(panel.rectTransform, "Feedback Title", "", new Vector2(0f, 64f),
                new Vector2(232f, 40f), 18, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _feedbackBody = CreateText(panel.rectTransform, "Feedback Body", "", new Vector2(0f, -10f),
                new Vector2(232f, 94f), 13, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);
            CreateTextButton(panel.rectTransform, "Confirm Task", "임무 확인", new Vector2(0f, -92f),
                new Vector2(232f, 32f), ConfirmCurrentTask, ElevatedColor);
        }

        private void BuildSettingsOverlay()
        {
            _buildingTargets = _settingsTargets;
            Image overlay = CreateImage(_canvasRect, "Settings Overlay", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), new Color(0.18f, 0.2f, 0.22f, 0.78f));
            _settingsOverlay = overlay.gameObject;
            Image modal = CreateImage(overlay.rectTransform, "Settings Dialog", Vector2.zero,
                new Vector2(384f, 286f), CardColor);
            CreateText(modal.rectTransform, "Settings Title", "설정", new Vector2(-127f, 118f),
                new Vector2(90f, 28f), 18, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _settingsStageLabel = CreateText(modal.rectTransform, "Settings Stage", "", new Vector2(56f, 118f),
                new Vector2(232f, 24f), 11, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            CreateTextButton(modal.rectTransform, "Choose Stage", "단계 이동", new Vector2(-52f, 77f),
                new Vector2(240f, 32f), () => ShowStageMenu(true), ElevatedColor);
            Button save = CreateTextButton(modal.rectTransform, "Save", "저장하기", new Vector2(124f, 77f),
                new Vector2(96f, 32f), SaveSettings, AccentSoftColor);
            _saveLabel = save.GetComponentInChildren<Text>();
            BuildSettingSlider(modal.rectTransform, "Brightness", "밝기", new Vector2(0f, 26f),
                _brightness, SetBrightness, out _brightnessValue);
            BuildSettingSlider(modal.rectTransform, "Volume", "볼륨", new Vector2(0f, -24f),
                _volume, SetVolume, out _volumeValue);
            Button vibration = CreateTextButton(modal.rectTransform, "Vibration", "", new Vector2(0f, -72f),
                new Vector2(344f, 34f), ToggleVibration, SurfaceColor);
            CreateText(vibration.transform, "Vibration Label", "진동", new Vector2(-128f, 0f),
                new Vector2(60f, 24f), 12, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft);
            _vibrationValue = CreateText(vibration.transform, "Vibration Value", "", new Vector2(128f, 0f),
                new Vector2(60f, 24f), 12, FontStyle.Bold, AccentColor, TextAnchor.MiddleRight);
            CreateTextButton(modal.rectTransform, "Close Settings", "닫기", new Vector2(28f, -117f),
                new Vector2(88f, 32f), ToggleSettings, ElevatedColor);
            CreateTextButton(modal.rectTransform, "Exit Settings", "나가기", new Vector2(128f, -117f),
                new Vector2(88f, 32f), RequestExit, new Color(0.98f, 0.88f, 0.87f));

            _buildingTargets = _stageTargets;
            Image stagePanel = CreateImage(modal.rectTransform, "Stage Menu", Vector2.zero,
                new Vector2(384f, 286f), CardColor);
            _stageMenu = stagePanel.gameObject;
            CreateText(stagePanel.rectTransform, "Stage Menu Title", "단계 이동", new Vector2(0f, 118f),
                new Vector2(344f, 28f), 18, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            for (int index = 0; index < _stageNames.Length; index++)
            {
                int stageIndex = index;
                string label = index == 2
                    ? _stageNames[index] + " (이미지 확대 및 합성곱)"
                    : _stageNames[index];
                CreateTextButton(stagePanel.rectTransform, "Stage " + index, label,
                    new Vector2(0f, 78f - index * 32f), new Vector2(344f, 28f),
                    () => MoveToStage(stageIndex), ElevatedColor);
            }
            CreateTextButton(stagePanel.rectTransform, "Back to Settings", "뒤로", new Vector2(128f, -117f),
                new Vector2(88f, 32f), () => ShowStageMenu(false), ElevatedColor);
            _stageMenu.SetActive(false);
            _settingsOverlay.SetActive(false);
        }

        private void BuildSettingSlider(RectTransform parent, string name, string label, Vector2 position,
            float initialValue, UnityAction<float> onValueChanged, out Text valueText)
        {
            Image row = CreateImage(parent, name + " Row", position, new Vector2(344f, 40f), SurfaceColor);
            CreateText(row.rectTransform, name + " Label", label, new Vector2(-138f, 0f),
                new Vector2(44f, 24f), 12, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft);
            Image rail = CreateImage(row.rectTransform, name + " Track", new Vector2(2f, 0f),
                new Vector2(196f, 4f), ElevatedColor);
            Image fill = CreateImage(rail.rectTransform, name + " Fill", Vector2.zero, Vector2.zero, AccentColor);
            // Slider drives the anchors; offsets must remain zero at all percentages.
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            Image handle = CreateImage(rail.rectTransform, name + " Handle", Vector2.zero,
                new Vector2(10f, 10f), AccentColor);
            Slider slider = row.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(initialValue);
            slider.onValueChanged.AddListener(onValueChanged);
            RegisterControl(slider);
            valueText = CreateText(row.rectTransform, name + " Value", "", new Vector2(138f, 0f),
                new Vector2(44f, 24f), 12, FontStyle.Bold, TextColor, TextAnchor.MiddleRight);
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
            image.raycastTarget = false;
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
                new Vector2(size.x - 4f, size.y - 4f), 11, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            RegisterControl(button);
            return button;
        }

        private Outline AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
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
                "선택한 아이템으로 오염된 픽셀을 관찰하세요.\n\nRGB 채널의 값을 비교하고, 필터가 주변 픽셀에 미치는 영향을 확인하세요.");
        }

        private void RefreshInventory()
        {
            for (int index = 0; index < _inventoryGlows.Count; index++)
            {
                _inventoryGlows[index].gameObject.SetActive(index == _selectedItemIndex);
            }
        }

        private void RefreshTasks()
        {
            for (int index = 0; index < _tasks.Length; index++)
            {
                bool isDone = _tasks[index].IsCompleted;
                _taskChecks[index].color = isDone ? AccentColor : ElevatedColor;
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
                _progressFill.rectTransform.sizeDelta = new Vector2(426f * DefaultProgress, 8f);
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
                _vibrationValue.text = _isVibrationEnabled ? "켜짐" : "꺼짐";
            }
        }

        private void SelectItem(int index)
        {
            _selectedItemIndex = index;
            RefreshInventory();
            UpdateFeedback(_itemNames[index] + " 선택됨",
                "관찰할 픽셀과 RGB 채널을 확인하세요.\n\n주변 픽셀의 값이 바뀌면 필터의 출력도 달라집니다.");
        }

        private void ToggleSettings()
        {
            if (!_isInteracting)
            {
                SetInteraction(true);
            }
            bool open = !_settingsOverlay.activeSelf;
            _stageMenu.SetActive(false);
            _settingsOverlay.SetActive(open);
            ResetFocus(open ? 0 : 6);
        }

        private void ShowStageMenu(bool open)
        {
            _stageMenu.SetActive(open);
            ResetFocus(open ? _selectedStageIndex : 0);
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
            _stageMenu.SetActive(false);
            _settingsOverlay.SetActive(false);
            ResetFocus(6);

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
                    _stageNames[_selectedStageIndex] + " 시작 지점이 아직 준비되지 않았습니다.");
            }
        }

        private void SetBrightness(float value)
        {
            _brightness = Mathf.Clamp01(value);
            Screen.brightness = _brightness;
            RefreshSettingsValues();
            _saveLabel.text = "저장하기";
        }

        private void SetVolume(float value)
        {
            _volume = Mathf.Clamp01(value);
            AudioListener.volume = _volume;
            RefreshSettingsValues();
            _saveLabel.text = "저장하기";
        }

        private void ToggleVibration()
        {
            _isVibrationEnabled = !_isVibrationEnabled;
            RefreshSettingsValues();
            _saveLabel.text = "저장하기";
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
            _saveLabel.text = "저장 완료";
            UpdateFeedback("저장 완료",
                "현재 단계와 태블릿 설정을 저장했습니다.\n다음 접속 때 같은 상태로 복원됩니다.");
        }

        private void RequestExit()
        {
            SetInteraction(false);
            _exitRequested.Invoke();
            UpdateFeedback("태블릿 조작 종료", "현재 교육 화면으로 돌아왔습니다.");
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

        private void RegisterControl(Selectable control)
        {
            control.navigation = new Navigation { mode = Navigation.Mode.None };
            Outline outline = AddOutline(control.GetComponent<Image>(), AccentColor, new Vector2(1.5f, 1.5f));
            outline.enabled = false;
            _buildingTargets.Add(new FocusTarget(control, outline));
        }

        private void SetInteraction(bool active)
        {
            _isInteracting = active;
            // Swap only this rig's left movement reader; other interactions keep their inputs.
            if (active && !_movementCaptured && _moveProvider != null)
            {
                _previousMoveInput = _moveProvider.leftHandMoveInput;
                _moveProvider.leftHandMoveInput = _blockedMoveInput;
                _movementCaptured = true;
            }
            if (!active)
            {
                _settingsOverlay.SetActive(false);
                _stageMenu.SetActive(false);
            }
            ResetFocus(_selectedItemIndex);
        }

        private void ReleaseMovement()
        {
            if (_movementCaptured && _moveProvider != null && _moveProvider.leftHandMoveInput == _blockedMoveInput)
            {
                _moveProvider.leftHandMoveInput = _previousMoveInput;
            }
            _movementCaptured = false;
            _previousMoveInput = null;
        }

        private void GoBackOrOpenSettings()
        {
            if (_isAdjustingSlider)
            {
                _isAdjustingSlider = false;
                _stickMustCenter = true;
                _lastDirection = Vector2.zero;
                RefreshFocus();
            }
            else if (_isInteracting && _stageMenu.activeSelf)
            {
                ShowStageMenu(false);
            }
            else
            {
                ToggleSettings();
            }
        }

        private void ResetFocus(int index)
        {
            _focusIndex = Mathf.Clamp(index, 0, ActiveTargets.Count - 1);
            _isAdjustingSlider = false;
            _stickMustCenter = true;
            _lastDirection = Vector2.zero;
            RefreshFocus();
        }

        private void RefreshFocus()
        {
            foreach (FocusTarget target in _mainTargets)
            {
                target.Outline.enabled = false;
            }
            foreach (FocusTarget target in _settingsTargets)
            {
                target.Outline.enabled = false;
            }
            foreach (FocusTarget target in _stageTargets)
            {
                target.Outline.enabled = false;
            }
            if (_isInteracting)
            {
                Outline outline = ActiveTargets[_focusIndex].Outline;
                outline.effectColor = _isAdjustingSlider ? WarningColor : AccentColor;
                outline.enabled = true;
            }
        }

        private void Navigate(Vector2 stick)
        {
            if (stick.magnitude < 0.25f)
            {
                _stickMustCenter = false;
                _lastDirection = Vector2.zero;
                return;
            }
            if (_stickMustCenter || stick.magnitude < 0.6f)
            {
                return;
            }
            Vector2 direction = Mathf.Abs(stick.x) > Mathf.Abs(stick.y)
                ? new Vector2(Mathf.Sign(stick.x), 0f) : new Vector2(0f, Mathf.Sign(stick.y));
            bool newDirection = direction != _lastDirection;
            if (!newDirection && Time.unscaledTime < _nextRepeatTime)
            {
                return;
            }
            _lastDirection = direction;
            _nextRepeatTime = Time.unscaledTime + (newDirection ? 0.4f : 0.16f);
            List<FocusTarget> targets = ActiveTargets;
            if (_isAdjustingSlider)
            {
                if (direction.x != 0f && targets[_focusIndex].Control is Slider slider)
                {
                    slider.value = Mathf.Clamp01(slider.value + direction.x * 0.05f);
                }
                return;
            }

            Vector2 current = _canvasRect.InverseTransformPoint(targets[_focusIndex].Control.transform.position);
            float bestScore = float.PositiveInfinity;
            int nextIndex = _focusIndex;
            for (int index = 0; index < targets.Count; index++)
            {
                if (index == _focusIndex)
                {
                    continue;
                }
                Vector2 position = _canvasRect.InverseTransformPoint(targets[index].Control.transform.position);
                Vector2 delta = position - current;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 1f)
                {
                    continue;
                }
                RectTransform targetRect = (RectTransform)targets[index].Control.transform;
                Vector2 halfSize = targetRect.rect.size * 0.5f;
                float perpendicularExtent = direction.x == 0f ? halfSize.x : halfSize.y;
                float sideways = Mathf.Max(0f,
                    Mathf.Abs(delta.x * direction.y - delta.y * direction.x) - perpendicularExtent);
                float score = forward + sideways * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    nextIndex = index;
                }
            }
            _focusIndex = nextIndex;
            RefreshFocus();
        }

        private void OnDisable()
        {
            _xButton?.Disable();
            _yButton?.Disable();
            _trigger?.Disable();
            _stick?.Disable();
            if (_settingsOverlay != null && _stageMenu != null)
            {
                SetInteraction(false);
            }
            ReleaseMovement();
        }

        private void OnDestroy()
        {
            _xButton?.Dispose();
            _yButton?.Dispose();
            _trigger?.Dispose();
            _stick?.Dispose();
            DestroyRuntimeSprite(_roundedSprite);
            DestroyRuntimeSprite(_circleSprite);
        }

        private void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                Destroy(sprite.texture);
                Destroy(sprite);
            }
        }

        private sealed class FocusTarget
        {
            public FocusTarget(Selectable control, Outline outline)
            {
                Control = control;
                Outline = outline;
            }

            public Selectable Control { get; }
            public Outline Outline { get; }
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
