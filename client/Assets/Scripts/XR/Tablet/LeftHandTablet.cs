using System;
using System.Collections;
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
    [DefaultExecutionOrder(-400)]
    public sealed class LeftHandTablet : MonoBehaviour
    {
        private const float CanvasWidth = 1554f;
        private const float CanvasHeight = 1012f;
        private const float TabletWorldWidth = 0.3f;
        private const float DefaultProgress = 0.75f;
        private const float ProgressTrackWidth = 208f;
        private const string SaveBrightnessKey = "Tablet.Brightness";
        private const string SaveVolumeKey = "Tablet.Volume";
        private const string SaveVibrationKey = "Tablet.Vibration";
        private const string SaveStageKey = "Tablet.Stage";
        private const string SaveInventoryKey = "Tablet.Inventory";

        private static readonly Color SurfaceColor = new Color(0.94f, 0.97f, 0.98f, 1f);
        private static readonly Color CardColor = Color.white;
        private static readonly Color ElevatedColor = new Color(0.87f, 0.9f, 0.94f, 1f);
        private static readonly Color SeparatorColor = new Color(0.88f, 0.9f, 0.93f, 1f);
        private static readonly Color AccentColor = new Color(0.19f, 0.36f, 0.63f, 1f);
        private static readonly Color AccentSoftColor = new Color(0.85f, 0.91f, 0.99f, 1f);
        private static readonly Color SuccessColor = new Color(0.12f, 0.56f, 0.37f, 1f);
        private static readonly Color WarningColor = new Color(0.97f, 0.68f, 0.13f, 1f);
        private static readonly Color DangerColor = new Color(0.8f, 0.2f, 0.25f, 1f);
        private static readonly Color TextColor = new Color(0.035f, 0.10f, 0.22f, 1f);
        private static readonly Color MutedTextColor = new Color(0.32f, 0.42f, 0.56f, 1f);
        private static readonly Color[] ItemColors =
        {
            new Color(0.34f, 0.63f, 0.77f), new Color(0.26f, 0.55f, 0.51f), new Color(0.47f, 0.27f, 0.82f),
            new Color(0.86f, 0.48f, 0.16f), new Color(0.96f, 0.25f, 0.34f)
        };

        [SerializeField, HideInInspector] private Text _message;
        [SerializeField] private Font _font;
        [SerializeField] private Transform[] _stageStartPoints;
        [SerializeField] private StageMoveEvent _stageMoveRequested = new StageMoveEvent();
        [SerializeField] private UnityEvent _saveRequested = new UnityEvent();
        [SerializeField] private UnityEvent _exitRequested = new UnityEvent();

        private readonly string[] _itemNames = { "양피지 #1", "양피지 #2", "양피지 #3", "양피지 #4", "양피지 #5" };
        private readonly string[] _itemDescriptions =
        {
            "기본 필터 렌즈", "엣지 강화 렌즈", "블러 제거 렌즈", "노이즈 억제 렌즈", "고주파 필터 렌즈"
        };
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

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _surfaceRect;
        private Font _resolvedFont;
        private Sprite _roundedSprite;
        private Sprite _circleSprite;
        private Sprite _sheetSprite;
        private Sprite _shadowSprite;
        private Sprite _panelSprite;
        private Sprite _selectedSprite;
        private Sprite _primarySprite;
        private Sprite _blueCircleSprite;
        private Sprite _mutedCircleSprite;
        private Sprite _underlineSprite;
        private readonly List<Sprite> _generatedSprites = new List<Sprite>();
        private readonly List<Sprite> _pixelSprites = new List<Sprite>();
        private readonly List<Image> _phaseSurfaces = new List<Image>();
        private readonly List<Image> _phaseCircles = new List<Image>();
        private readonly List<Text> _phaseLabels = new List<Text>();
        private readonly List<Image> _phaseChevrons = new List<Image>();
        private readonly List<Image> _taskRows = new List<Image>();
        private MeshFilter _bodyFilter;
        private Mesh _originalBodyMesh;
        private Mesh _bodyMesh;
        private Vector3 _originalBodyScale;
        private Texture2D _symbolAtlas;
        private readonly Dictionary<TabletSymbol, Sprite> _symbolSprites = new Dictionary<TabletSymbol, Sprite>();
        private InputAction _xButton;
        private InputAction _yButton;
        private InputAction _trigger;
        private InputAction _menuButton;
        private InputAction _stick;
        private ContinuousMoveProvider _moveProvider;
        private XRInputValueReader<Vector2> _previousMoveInput;
        private readonly XRInputValueReader<Vector2> _blockedMoveInput =
            new XRInputValueReader<Vector2>("Tablet Focus", XRInputValueReader.InputSourceMode.ManualValue);
        private readonly List<FocusTarget> _mainTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _settingsTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _stageTargets = new List<FocusTarget>();
        private readonly List<FocusTarget> _detailTargets = new List<FocusTarget>();
        private List<FocusTarget> _buildingTargets;
        private GameObject _stageMenu;
        private RectTransform _settingsSheet;
        private CanvasGroup _settingsCanvasGroup;
        private Coroutine _settingsAnimation;
        private Text _saveLabel;
        private Text _dashboardSaveLabel;
        private Button _settingsButton;
        private GameObject _detailOverlay;
        private Text _detailTitle;
        private Text _detailBody;
        private Image _detailImage;
        private int _detailReturnFocusIndex;
        private bool _movementCaptured;
        private bool _isInteracting;
        private bool _isAdjustingSlider;
        private bool _stickMustCenter;
        private Vector2 _lastDirection;
        private float _nextRepeatTime;
        private int _focusIndex;

        private Text _progressPercent;
        private Image _progressFill;
        private Text _feedbackTitle;
        private Text _feedbackBody;
        private readonly List<Image> _inventoryGlows = new List<Image>();
        private readonly List<Image> _inventorySurfaces = new List<Image>();
        private readonly List<Text> _inventoryLabels = new List<Text>();
        private readonly List<Text> _taskTexts = new List<Text>();
        private readonly List<Image> _taskChecks = new List<Image>();
        private readonly List<Image> _taskCheckmarks = new List<Image>();
        private readonly List<Image> _stageSurfaces = new List<Image>();
        private readonly List<Image> _stageCheckmarks = new List<Image>();
        private Text _taskCount;
        private GameObject _settingsOverlay;
        private Text _brightnessValue;
        private Text _volumeValue;
        private Text _vibrationValue;
        private Image _vibrationTrack;
        private RectTransform _vibrationThumb;
        private Text _settingsStageLabel;
        private float _brightness = 0.8f;
        private float _volume = 0.49f;
        private int _selectedItemIndex;
        private int _selectedStageIndex;
        private bool _isVibrationEnabled = true;
        private int _completedTaskIndex = 1;

        private List<FocusTarget> ActiveTargets => _detailOverlay != null && _detailOverlay.activeSelf ? _detailTargets :
            _stageMenu.activeSelf ? _stageTargets :
            _settingsOverlay.activeSelf ? _settingsTargets : _mainTargets;

        private void Awake()
        {
            _xButton = new InputAction("Tablet X", InputActionType.Button,
                "<XRController>{LeftHand}/primaryButton");
            _yButton = new InputAction("Tablet Y", InputActionType.Button,
                "<XRController>{LeftHand}/secondaryButton");
            _trigger = new InputAction("Tablet Trigger", InputActionType.Button,
                "<XRController>{LeftHand}/triggerPressed");
            _menuButton = new InputAction("Tablet Settings", InputActionType.Button,
                "<XRController>{LeftHand}/menuButton");
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
            _menuButton.Enable();
            _stick.Enable();
        }

        private void Update()
        {
            bool navigating = _trigger.IsPressed();
            Vector2 stick = _stick.ReadValue<Vector2>();
            if (navigating)
            {
                if (!_isInteracting)
                {
                    SetInteraction(true);
                }
                CaptureMovement();
            }
            else if (stick.magnitude < 0.25f)
            {
                // Do not turn a tablet gesture into walking until the stick returns to neutral.
                ReleaseMovement();
            }

            if (_menuButton.WasPressedThisFrame())
            {
                ToggleSettings();
            }
            else if (_yButton.WasPressedThisFrame())
            {
                GoBack();
            }
            else if (_xButton.WasPressedThisFrame())
            {
                if (!_isInteracting)
                {
                    SetInteraction(true);
                }
                if (ActiveTargets[_focusIndex].Control is Button button && button.IsInteractable())
                {
                    button.onClick.Invoke();
                }
                else if (ActiveTargets[_focusIndex].Control is Slider slider && slider.IsInteractable())
                {
                    _isAdjustingSlider = !_isAdjustingSlider;
                    _stickMustCenter = true;
                    RefreshFocus();
                }
            }

            if (navigating && _isInteracting)
            {
                Navigate(stick);
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
            scaler.dynamicPixelsPerUnit = 2f;
            _canvasRect = _canvas.GetComponent<RectTransform>();
            _canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            _canvasRect.localScale = Vector3.one * (TabletWorldWidth / CanvasWidth);
            CreateRuntimeSprites();
            ClearCanvas();
            BuildTabletFrame();
            BuildRoundedBody();

            CreateGlassPanel(_surfaceRect, "Learning Column", 66f, 186f, 354f, 764f);
            CreateGlassPanel(_surfaceRect, "Concept Column", 440f, 186f, 682f, 764f);
            CreateGlassPanel(_surfaceRect, "Inventory Column", 1144f, 186f, 346f, 764f);
            _buildingTargets = _mainTargets;
            // Keep the five inventory targets first so saved selection still restores focus.
            BuildInventory();
            BuildMainFeedback();
            BuildDashboardActions();
            BuildHeader();
            BuildLeftStatus();
            BuildSettingsOverlay();
            BuildDetailOverlay();
            _buildingTargets = null;
        }

        private void CreateRuntimeSprites()
        {
            _roundedSprite = CreateRoundedSprite(256, 24);
            _circleSprite = CreateRoundedSprite(128, 64);
            _sheetSprite = CreateRoundedSprite(256, 36);
            _shadowSprite = CreateRoundedSprite(256, 44, 12f);
            _panelSprite = CreateGradientSprite(36, new Color(1f, 1f, 1f, 0.88f), new Color(0.94f, 0.97f, 0.98f, 0.7f));
            _selectedSprite = CreateGradientSprite(24, new Color(0.90f, 0.94f, 1f), new Color(0.82f, 0.88f, 0.98f));
            _primarySprite = CreateGradientSprite(36, new Color(0.37f, 0.52f, 0.72f), new Color(0.09f, 0.20f, 0.38f));
            _blueCircleSprite = CreateGradientSprite(128, new Color(0.49f, 0.66f, 0.88f), new Color(0.16f, 0.30f, 0.52f));
            _mutedCircleSprite = CreateGradientSprite(128, new Color(0.81f, 0.84f, 0.88f), new Color(0.68f, 0.72f, 0.77f));
            _underlineSprite = CreateUnderlineSprite();
            CreateSymbolSprites();
            for (int channel = -1; channel < 3; channel++)
            {
                _pixelSprites.Add(CreatePixelSprite(channel));
            }
        }

        private void ClearCanvas()
        {
            for (int index = _canvasRect.childCount - 1; index >= 0; index--)
            {
                _canvasRect.GetChild(index).gameObject.SetActive(false);
                Destroy(_canvasRect.GetChild(index).gameObject);
            }
            _inventoryGlows.Clear();
            _inventorySurfaces.Clear();
            _inventoryLabels.Clear();
            _taskTexts.Clear();
            _taskChecks.Clear();
            _taskCheckmarks.Clear();
            _taskRows.Clear();
            _stageSurfaces.Clear();
            _stageCheckmarks.Clear();
            _phaseSurfaces.Clear();
            _phaseCircles.Clear();
            _phaseLabels.Clear();
            _phaseChevrons.Clear();
            _mainTargets.Clear();
            _settingsTargets.Clear();
            _stageTargets.Clear();
            _detailTargets.Clear();
        }

        private void BuildTabletFrame()
        {
            Sprite rim = CreateGradientSprite(92, new Color(0.80f, 0.82f, 0.83f), new Color(0.37f, 0.39f, 0.40f));
            PlaceImage(_canvasRect, "Metal Rim", 0f, 0f, 1554f, 1012f, Color.white).sprite = rim;
            Sprite bezel = CreateGradientSprite(88, new Color(0.055f, 0.06f, 0.065f), new Color(0.015f, 0.02f, 0.025f));
            PlaceImage(_canvasRect, "Black Bezel", 5f, 5f, 1544f, 1002f, Color.white).sprite = bezel;
            Image screen = PlaceImage(_canvasRect, "Tablet Surface", 40f, 38f, 1474f, 940f, Color.white);
            screen.sprite = CreateGradientSprite(38, new Color(0.98f, 0.99f, 1f), new Color(0.90f, 0.95f, 0.97f));
            screen.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _surfaceRect = screen.rectTransform;
            Image camera = PlaceImage(_canvasRect, "Front Camera", 772f, 17f, 11f, 11f, new Color(0.025f, 0.05f, 0.09f));
            camera.sprite = _circleSprite;
            PlaceImage(_canvasRect, "Camera Lens", 776f, 21f, 3f, 3f, new Color(0.16f, 0.31f, 0.52f)).sprite = _circleSprite;
        }

        private void BuildHeader()
        {
            Image bookTile = CreateGlassPanel(_surfaceRect, "Education Symbol Tile", 106f, 72f, 110f, 92f);
            bookTile.sprite = CreateGradientSprite(26, new Color(0.89f, 0.95f, 0.97f), new Color(0.75f, 0.85f, 0.91f));
            PlaceSymbol(_surfaceRect, "Education Book", TabletSymbol.Book, 132f, 94f, 58f, AccentColor);
            PlaceText(_surfaceRect, "Education Title", "픽셀 구조와 CNN 필터", 264f, 77f, 660f, 60f,
                41, FontStyle.Bold, TextColor);
            PlaceText(_surfaceRect, "Team Name", "미디어 · XR 디스플레이 교육", 265f, 133f, 620f, 32f,
                24, FontStyle.Normal, MutedTextColor);

            CreateGlassPanel(_surfaceRect, "Progress Capsule", 945f, 98f, 226f, 32f).sprite = _roundedSprite;
            PlaceImage(_surfaceRect, "Progress Track", 954f, 107f, ProgressTrackWidth, 14f, ElevatedColor);
            _progressFill = PlaceImage(_surfaceRect, "Progress Fill", 954f, 107f, ProgressTrackWidth, 14f, Color.white);
            _progressFill.sprite = _primarySprite;
            _progressFill.pixelsPerUnitMultiplier = 6f;
            _progressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _progressFill.rectTransform.anchoredPosition = ReferencePosition(_surfaceRect, 954f, 114f);
            _progressPercent = PlaceText(_surfaceRect, "Progress Percent", "", 1186f, 99f, 150f, 32f,
                20, FontStyle.Normal, MutedTextColor);
            _settingsButton = PlaceIconButton(_surfaceRect, "설정", TabletSymbol.Gear, 1353f, 85f, 58f, ToggleSettings);
            PlaceIconButton(_surfaceRect, "학습 정보", TabletSymbol.User, 1428f, 85f, 58f, ShowLearningInfo);
        }

        private void BuildLeftStatus()
        {
            PlaceText(_surfaceRect, "Learning Stage Caption", "학습 단계", 107f, 212f, 260f, 36f,
                23, FontStyle.Bold, TextColor);
            PlaceImage(_surfaceRect, "Stage Connector", 127f, 300f, 2f, 228f, new Color(0.80f, 0.84f, 0.88f)).sprite = null;
            for (int index = 0; index < _phaseNames.Length; index++)
            {
                int phaseIndex = index;
                float centerY = 291f + index * 59f;
                Button phase = PlaceButton(_surfaceRect, "Learning Phase " + index, "", 91f, centerY - 31f, 307f, 62f,
                    () => MoveToStage(_phaseStageIndices[phaseIndex]), SurfaceColor);
                _phaseSurfaces.Add(phase.GetComponent<Image>());
                Image circle = PlaceImage(phase.transform, "Phase Number Circle", 101f, centerY - 27f, 54f, 54f, Color.white);
                circle.sprite = _mutedCircleSprite;
                _phaseCircles.Add(circle);
                PlaceText(phase.transform, "Phase Number", (index + 1).ToString(), 101f, centerY - 22f, 54f, 44f,
                    21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                _phaseLabels.Add(PlaceText(phase.transform, "Phase Label", _phaseNames[index], 173f, centerY - 20f, 185f, 40f,
                    24, FontStyle.Normal, MutedTextColor));
                _phaseChevrons.Add(PlaceSymbol(phase.transform, "Phase Chevron", TabletSymbol.ChevronRight,
                    363f, centerY - 9f, 18f, AccentColor));
            }

            Image tasks = CreateGlassPanel(_surfaceRect, "Tasks", 85f, 573f, 317f, 211f);
            PlaceText(tasks.transform, "Task Caption", "현재 임무", 113f, 592f, 195f, 34f,
                21, FontStyle.Bold, TextColor);
            _taskCount = PlaceText(tasks.transform, "Task Count", "", 326f, 592f, 48f, 34f,
                21, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            for (int index = 0; index < _tasks.Length; index++)
            {
                float top = 633f + index * 47f;
                _taskRows.Add(PlaceImage(tasks.transform, "Task Row " + index, 105f, top, 279f, 46f, CardColor));
                Image check = PlaceImage(tasks.transform, "Task Check " + index, 116f, top + 9f, 30f, 30f, ElevatedColor);
                check.sprite = _mutedCircleSprite;
                _taskChecks.Add(check);
                _taskCheckmarks.Add(PlaceSymbol(tasks.transform, "Completed " + index, TabletSymbol.Check,
                    122f, top + 15f, 18f, Color.white));
                _taskTexts.Add(PlaceText(tasks.transform, "Task Text " + index, _tasks[index].Label,
                    168f, top + 6f, 214f, 34f, 19, FontStyle.Normal, MutedTextColor));
            }
            BuildMinimap();
        }

        private void BuildMinimap()
        {
            Image map = CreateGlassPanel(_surfaceRect, "Minimap", 85f, 792f, 317f, 137f);
            PlaceText(map.transform, "Map Caption", "현재 위치", 112f, 810f, 200f, 34f, 21, FontStyle.Bold, TextColor);
            PlaceText(map.transform, "North", "N", 354f, 810f, 24f, 34f, 21, FontStyle.Normal, MutedTextColor);
            PlaceImage(map.transform, "Location Track", 111f, 861f, 267f, 12f, ElevatedColor);
            PlaceImage(map.transform, "Route", 197f, 861f, 118f, 12f, new Color(0.69f, 0.76f, 0.85f));
            PlaceImage(map.transform, "User Position", 185f, 855f, 24f, 24f, Color.white).sprite = _blueCircleSprite;
            PlaceImage(map.transform, "Destination", 304f, 856f, 22f, 22f, new Color(1f, 0.58f, 0.62f)).sprite = _circleSprite;
            PlaceText(map.transform, "Map Destination", "픽셀 관찰 구역", 164f, 889f, 210f, 29f,
                18, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleCenter);
        }

        private void BuildInventory()
        {
            PlaceSymbol(_surfaceRect, "Inventory Symbol", TabletSymbol.Box, 1179f, 215f, 40f, AccentColor);
            PlaceText(_surfaceRect, "Inventory Caption", "아이템", 1241f, 215f, 206f, 40f,
                26, FontStyle.Bold, TextColor);
            for (int index = 0; index < _itemNames.Length; index++)
            {
                int itemIndex = index;
                float top = 272f + index * 103f;
                Image glow = PlaceImage(_surfaceRect, "Inventory Glow " + index, 1158f, top - 7f, 318f, 104f,
                    new Color(0.59f, 0.72f, 0.94f, 0.24f));
                glow.sprite = _shadowSprite;
                _inventoryGlows.Add(glow);
                Button item = PlaceButton(_surfaceRect, "Inventory " + index, "", 1166f, top, 302f, 90f,
                    () => SelectItem(itemIndex), CardColor);
                Image surface = item.GetComponent<Image>();
                surface.sprite = _panelSprite;
                _inventorySurfaces.Add(surface);
                _inventoryLabels.Add(PlaceText(item.transform, "Parchment Name", _itemNames[index], 1259f, top + 14f, 164f, 34f,
                    21, FontStyle.Bold, TextColor));
                PlaceText(item.transform, "Parchment Description", _itemDescriptions[index], 1259f, top + 48f, 182f, 29f,
                    18, FontStyle.Normal, MutedTextColor);
                PlaceSymbol(item.transform, "Item Symbol", TabletSymbol.Box, 1188f, top + 23f, 46f, ItemColors[index]);
                PlaceSymbol(item.transform, "Disclosure", TabletSymbol.ChevronRight, 1434f, top + 36f, 18f, AccentColor);
            }
        }

        private void BuildMainFeedback()
        {
            Image guideTile = CreateGlassPanel(_surfaceRect, "Guide Symbol Tile", 472f, 214f, 58f, 56f);
            guideTile.sprite = _selectedSprite;
            PlaceSymbol(_surfaceRect, "Guide Symbol", TabletSymbol.Book, 485f, 227f, 32f, AccentColor);
            PlaceText(_surfaceRect, "Concept Caption", "개념 설명 · 힌트톡", 552f, 224f, 350f, 39f,
                24, FontStyle.Bold, new Color(0.13f, 0.29f, 0.64f));
            _feedbackTitle = PlaceText(_surfaceRect, "Feedback Title", "", 485f, 301f, 605f, 67f,
                46, FontStyle.Bold, TextColor);
            Image underline = PlaceImage(_surfaceRect, "Title Underline", 485f, 350f, 296f, 36f, new Color(0.66f, 0.79f, 0.95f));
            underline.sprite = _underlineSprite;
            underline.type = Image.Type.Simple;
            _feedbackBody = PlaceText(_surfaceRect, "Feedback Body", "", 485f, 405f, 600f, 180f,
                26, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            _feedbackBody.lineSpacing = 0.91f;
            _feedbackBody.resizeTextForBestFit = true;
            _feedbackBody.resizeTextMinSize = 23;
            _feedbackBody.resizeTextMaxSize = 26;
            BuildPixelPreviews();

            Image actionShadow = PlaceImage(_surfaceRect, "Confirm Shadow", 452f, 823f, 658f, 115f,
                new Color(0.18f, 0.36f, 0.59f, 0.28f));
            actionShadow.sprite = _shadowSprite;
            Button confirm = PlaceButton(_surfaceRect, "Confirm Task", "작업 확인하기", 465f, 827f, 632f, 95f,
                ConfirmCurrentTask, CardColor);
            confirm.GetComponent<Image>().sprite = _primarySprite;
            AddOutline(confirm.GetComponent<Image>(), new Color(0.53f, 0.69f, 0.91f, 0.85f), new Vector2(2f, 2f));
            Text confirmLabel = confirm.GetComponentInChildren<Text>();
            confirmLabel.fontSize = 27;
            confirmLabel.fontStyle = FontStyle.Normal;
            confirmLabel.color = Color.white;
            confirmLabel.rectTransform.anchoredPosition = new Vector2(44f, 0f);
            confirmLabel.rectTransform.sizeDelta = new Vector2(430f, 80f);
            PlaceSymbol(confirm.transform, "Confirm Check", TabletSymbol.Check, 683f, 859f, 29f, Color.white);
            PlaceSymbol(confirm.transform, "Confirm Disclosure", TabletSymbol.ChevronRight, 1035f, 862f, 24f, Color.white);
            Button hint = PlaceButton(_surfaceRect, "Hint", "", 943f, 213f, 154f, 58f, ShowHint, CardColor);
            PlaceSymbol(hint.transform, "Hint Bulb", TabletSymbol.Bulb, 963f, 226f, 32f, new Color(1f, 0.70f, 0.20f));
            PlaceText(hint.transform, "Hint Label", "힌트 보기", 1005f, 225f, 88f, 34f, 20, FontStyle.Normal, MutedTextColor);
            Button expand = PlaceButton(_surfaceRect, "Expand Image", "", 657f, 659f, 51f, 49f,
                () => ShowDetail("원본 이미지", "오염 픽셀과 주변 픽셀의 밝기·색을 비교하세요.", true), new Color(0.31f, 0.35f, 0.41f, 0.8f));
            PlaceSymbol(expand.transform, "Expand Symbol", TabletSymbol.Expand, 670f, 672f, 25f, Color.white);
        }

        private void BuildPixelPreviews()
        {
            Image original = CreateGlassPanel(_surfaceRect, "Original Image Panel", 465f, 597f, 267f, 205f);
            PlaceText(original.transform, "Original Image Caption", "원본 이미지", 487f, 616f, 220f, 32f,
                21, FontStyle.Bold, TextColor);
            Image pixels = PlaceImage(original.transform, "Original Pixels", 488f, 658f, 220f, 122f, CardColor);
            pixels.sprite = _pixelSprites[0];
            pixels.type = Image.Type.Simple;

            Image channels = CreateGlassPanel(_surfaceRect, "RGB Channels Panel", 745f, 597f, 352f, 205f);
            PlaceText(channels.transform, "RGB Caption", "RGB 채널 분해", 767f, 616f, 308f, 32f,
                21, FontStyle.Bold, TextColor);
            Color[] colors = { new Color(1f, 0.13f, 0.18f), new Color(0.05f, 0.76f, 0.27f), new Color(0.05f, 0.33f, 1f) };
            string[] labels = { "R", "G", "B" };
            for (int index = 0; index < 3; index++)
            {
                float x = 767f + index * 106f;
                Image channel = PlaceImage(channels.transform, labels[index] + " Channel", x, 659f, 92f, 87f, CardColor);
                channel.sprite = _pixelSprites[index + 1];
                channel.type = Image.Type.Simple;
                AddOutline(channel, colors[index], new Vector2(2f, 2f));
                PlaceImage(channels.transform, labels[index] + " Caption Surface", x, 749f, 92f, 31f,
                    Color.Lerp(CardColor, colors[index], 0.18f));
                PlaceText(channels.transform, labels[index] + " Caption", labels[index], x, 748f, 92f, 32f,
                    20, FontStyle.Normal, colors[index], TextAnchor.MiddleCenter);
            }
        }

        private void BuildDashboardActions()
        {
            Button save = PlaceButton(_surfaceRect, "Save Dashboard", "", 1162f, 806f, 143f, 118f, SaveSettings,
                new Color(0.88f, 0.95f, 0.96f));
            PlaceSymbol(save.transform, "Save Symbol", TabletSymbol.Download, 1213f, 830f, 38f, new Color(0.16f, 0.39f, 0.47f));
            _dashboardSaveLabel = PlaceText(save.transform, "Save Label", "저장하기", 1174f, 878f, 119f, 34f,
                22, FontStyle.Normal, new Color(0.16f, 0.30f, 0.43f), TextAnchor.MiddleCenter);
            Button exit = PlaceButton(_surfaceRect, "Exit Dashboard", "", 1318f, 806f, 153f, 118f, RequestExit,
                new Color(1f, 0.92f, 0.94f));
            PlaceSymbol(exit.transform, "Exit Symbol", TabletSymbol.Exit, 1373f, 831f, 38f, ItemColors[4]);
            PlaceText(exit.transform, "Exit Label", "나가기", 1335f, 878f, 119f, 34f,
                22, FontStyle.Normal, ItemColors[4], TextAnchor.MiddleCenter);
        }

        private void BuildDetailOverlay()
        {
            _buildingTargets = _detailTargets;
            Image overlay = CreateImage(_surfaceRect, "Detail Overlay", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), new Color(0.07f, 0.12f, 0.22f, 0.38f));
            _detailOverlay = overlay.gameObject;
            Image modal = CreateImage(overlay.transform, "Detail Sheet", Vector2.zero, new Vector2(800f, 710f), CardColor);
            modal.sprite = _sheetSprite;
            _detailTitle = CreateText(modal.transform, "Detail Title", "", new Vector2(0f, 280f),
                new Vector2(700f, 64f), 38, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _detailBody = CreateText(modal.transform, "Detail Body", "", new Vector2(0f, 80f),
                new Vector2(700f, 260f), 27, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            _detailImage = CreateImage(modal.transform, "Detail Image", new Vector2(0f, -8f), new Vector2(700f, 388f), CardColor);
            _detailImage.sprite = _pixelSprites[0];
            _detailImage.type = Image.Type.Simple;
            Button close = CreateTextButton(modal.transform, "Close Detail", "완료", new Vector2(0f, -280f),
                new Vector2(700f, 64f), CloseDetail, AccentColor);
            close.GetComponentInChildren<Text>().fontSize = 26;
            close.GetComponentInChildren<Text>().color = CardColor;
            _detailOverlay.SetActive(false);
        }

        private void BuildSettingsOverlay()
        {
            _buildingTargets = _settingsTargets;
            Image overlay = CreateImage(_surfaceRect, "Settings Overlay", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), new Color(0.07f, 0.12f, 0.22f, 0.38f));
            _settingsOverlay = overlay.gameObject;
            _settingsCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            Image shadow = CreateImage(overlay.transform, "Settings Shadow", new Vector2(0f, -12f),
                new Vector2(848f, 788f), new Color(0.08f, 0.17f, 0.3f, 0.25f));
            shadow.sprite = _shadowSprite;
            Image modal = CreateImage(overlay.transform, "Settings Sheet", Vector2.zero, new Vector2(800f, 740f), SurfaceColor);
            modal.sprite = _sheetSprite;
            _settingsSheet = modal.rectTransform;
            CreateText(modal.transform, "Settings Title", "설정", new Vector2(-240f, 305f),
                new Vector2(240f, 60f), 40, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            _settingsStageLabel = CreateText(modal.transform, "Settings Stage", "", new Vector2(100f, 305f),
                new Vector2(520f, 50f), 23, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            CreateSeparator(modal.transform, new Vector2(0f, 255f), new Vector2(720f, 1f));
            Button stages = CreateSymbolButton(modal.transform, "Choose Stage", "단계 이동", TabletSymbol.Grid,
                new Vector2(-120f, 200f), new Vector2(480f, 72f), () => ShowStageMenu(true), CardColor, TextColor);
            stages.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            CreateSymbol(stages.transform, "Disclosure", TabletSymbol.ChevronRight, new Vector2(205f, 0f), 28f, AccentColor);
            Button save = CreateSymbolButton(modal.transform, "Save Settings", "저장하기", TabletSymbol.Download,
                new Vector2(252f, 200f), new Vector2(216f, 72f), SaveSettings, AccentSoftColor, AccentColor);
            _saveLabel = save.GetComponentInChildren<Text>();
            BuildSettingSlider(modal.rectTransform, "Brightness", "밝기", TabletSymbol.Sun, new Vector2(0f, 80f),
                _brightness, SetBrightness, out _brightnessValue);
            BuildSettingSlider(modal.rectTransform, "Volume", "볼륨", TabletSymbol.Volume, new Vector2(0f, -30f),
                _volume, SetVolume, out _volumeValue);

            Button vibration = CreateTextButton(modal.transform, "Vibration", "", new Vector2(0f, -145f),
                new Vector2(720f, 82f), ToggleVibration, CardColor);
            CreateSymbol(vibration.transform, "Vibration Symbol", TabletSymbol.Vibrate, new Vector2(-320f, 0f), 34f, MutedTextColor);
            CreateText(vibration.transform, "Vibration Label", "진동", new Vector2(-240f, 0f),
                new Vector2(100f, 50f), 24, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft);
            _vibrationValue = CreateText(vibration.transform, "Vibration Value", "", new Vector2(190f, 0f),
                new Vector2(100f, 50f), 23, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            _vibrationTrack = CreateImage(vibration.transform, "Vibration Switch", new Vector2(290f, 0f),
                new Vector2(74f, 44f), SuccessColor);
            _vibrationThumb = CreateImage(_vibrationTrack.transform, "Switch Thumb", new Vector2(15f, 0f),
                new Vector2(36f, 36f), CardColor).rectTransform;
            _vibrationThumb.GetComponent<Image>().sprite = _circleSprite;
            CreateSymbolButton(modal.transform, "Close Settings", "완료", TabletSymbol.Check,
                new Vector2(42f, -285f), new Vector2(200f, 70f), ToggleSettings, AccentColor, CardColor);
            CreateSymbolButton(modal.transform, "Exit Settings", "나가기", TabletSymbol.Exit,
                new Vector2(260f, -285f), new Vector2(200f, 70f), RequestExit,
                new Color(1f, 0.92f, 0.94f), ItemColors[4]);

            _buildingTargets = _stageTargets;
            Image stagePanel = CreateImage(modal.transform, "Stage Menu", Vector2.zero, new Vector2(800f, 740f), SurfaceColor);
            stagePanel.sprite = _sheetSprite;
            _stageMenu = stagePanel.gameObject;
            CreateText(stagePanel.transform, "Stage Menu Title", "단계 이동", new Vector2(0f, 305f),
                new Vector2(720f, 60f), 40, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            for (int index = 0; index < _stageNames.Length; index++)
            {
                int stageIndex = index;
                Button stage = CreateTextButton(stagePanel.transform, "Stage " + index, _stageNames[index],
                    new Vector2(0f, 220f - index * 80f), new Vector2(720f, 70f),
                    () => MoveToStage(stageIndex), CardColor);
                Text label = stage.GetComponentInChildren<Text>();
                label.fontStyle = FontStyle.Normal;
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.anchoredPosition = new Vector2(-10f, index == 2 ? 12f : 0f);
                label.rectTransform.sizeDelta = new Vector2(640f, 40f);
                if (index == 2)
                {
                    CreateText(stage.transform, "Stage Detail", "이미지 확대 및 합성곱", new Vector2(-10f, -20f),
                        new Vector2(640f, 28f), 18, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleLeft);
                }
                _stageSurfaces.Add(stage.GetComponent<Image>());
                _stageCheckmarks.Add(CreateSymbol(stage.transform, "Current Stage", TabletSymbol.Check,
                    new Vector2(318f, 0f), 30f, AccentColor));
            }
            CreateSymbolButton(stagePanel.transform, "Back to Settings", "설정으로", TabletSymbol.ChevronLeft,
                new Vector2(240f, -295f), new Vector2(240f, 62f), () => ShowStageMenu(false), CardColor, AccentColor);
            _stageMenu.SetActive(false);
            _settingsOverlay.SetActive(false);
        }

        private void BuildSettingSlider(RectTransform parent, string name, string label, TabletSymbol symbol, Vector2 position,
            float initialValue, UnityAction<float> onValueChanged, out Text valueText)
        {
            Image row = CreateImage(parent, name + " Row", position, new Vector2(720f, 90f), CardColor);
            CreateSymbol(row.transform, name + " Symbol", symbol, new Vector2(-320f, 0f), 34f, MutedTextColor);
            CreateText(row.transform, name + " Label", label, new Vector2(-240f, 0f),
                new Vector2(100f, 50f), 24, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft);
            Image rail = CreateImage(row.transform, name + " Track", new Vector2(20f, 0f), new Vector2(340f, 12f), ElevatedColor);
            Image fill = CreateImage(rail.transform, name + " Fill", Vector2.zero, Vector2.zero, AccentColor);
            rail.pixelsPerUnitMultiplier = 4f;
            fill.pixelsPerUnitMultiplier = 4f;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            Image handle = CreateImage(rail.transform, name + " Handle", Vector2.zero, new Vector2(34f, 34f), CardColor);
            handle.sprite = _circleSprite;
            AddOutline(handle, new Color(0.17f, 0.25f, 0.38f, 0.17f), new Vector2(1.5f, -1.5f));
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
            valueText = CreateText(row.transform, name + " Value", "", new Vector2(290f, 0f),
                new Vector2(100f, 50f), 24, FontStyle.Normal, TextColor, TextAnchor.MiddleRight);
        }

        // Reference-space coordinates keep the supplied 1554 x 1012 composition intact.
        private Vector2 ReferencePosition(Transform parent, float x, float y)
        {
            Vector3 point = _canvasRect.TransformPoint(new Vector3(x - CanvasWidth * 0.5f, CanvasHeight * 0.5f - y, 0f));
            return parent.InverseTransformPoint(point);
        }

        private Image PlaceImage(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            return CreateImage(parent, name, ReferencePosition(parent, x + width * 0.5f, y + height * 0.5f),
                new Vector2(width, height), color);
        }

        private Text PlaceText(Transform parent, string name, string value, float x, float y, float width, float height,
            int fontSize, FontStyle style, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            return CreateText(parent, name, value, ReferencePosition(parent, x + width * 0.5f, y + height * 0.5f),
                new Vector2(width, height), fontSize, style, color, alignment);
        }

        private Image PlaceSymbol(Transform parent, string name, TabletSymbol symbol, float x, float y, float size, Color color)
        {
            return CreateSymbol(parent, name, symbol, ReferencePosition(parent, x + size * 0.5f, y + size * 0.5f), size, color);
        }

        private Button PlaceButton(Transform parent, string name, string label, float x, float y, float width, float height,
            UnityAction action, Color color)
        {
            return CreateTextButton(parent, name, label, ReferencePosition(parent, x + width * 0.5f, y + height * 0.5f),
                new Vector2(width, height), action, color);
        }

        private Image CreateGlassPanel(Transform parent, string name, float x, float y, float width, float height)
        {
            Image shadow = PlaceImage(parent, name + " Shadow", x - 10f, y + 2f, width + 20f, height + 20f,
                new Color(0.28f, 0.45f, 0.58f, 0.07f));
            shadow.sprite = _shadowSprite;
            Image panel = PlaceImage(parent, name, x, y, width, height, CardColor);
            panel.sprite = _panelSprite;
            AddOutline(panel, new Color(1f, 1f, 1f, 0.68f), Vector2.one);
            return panel;
        }

        private Button PlaceIconButton(Transform parent, string name, TabletSymbol symbol, float x, float y, float size,
            UnityAction action)
        {
            Image shadow = PlaceImage(parent, name + " Shadow", x - 8f, y - 2f, size + 16f, size + 18f,
                new Color(0.20f, 0.33f, 0.47f, 0.12f));
            shadow.sprite = _shadowSprite;
            Button button = PlaceButton(parent, name, "", x, y, size, size, action, CardColor);
            button.GetComponent<Image>().sprite = _circleSprite;
            PlaceSymbol(button.transform, name + " Symbol", symbol, x + 15f, y + 15f, size - 30f, AccentColor);
            return button;
        }

        private void BuildRoundedBody()
        {
            Transform body = transform.Find("Tablet Body");
            _bodyFilter = body != null ? body.GetComponent<MeshFilter>() : null;
            if (_bodyFilter == null)
            {
                return;
            }
            _originalBodyMesh = _bodyFilter.sharedMesh;
            _originalBodyScale = body.localScale;
            const int cornerSegments = 12;
            const int ringCount = 4 * cornerSegments;
            float halfWidth = TabletWorldWidth * 0.5f;
            float halfHeight = TabletWorldWidth * CanvasHeight / CanvasWidth * 0.5f;
            const float radius = 0.0175f;
            Vector3[] vertices = new Vector3[ringCount * 2 + 2];
            List<int> triangles = new List<int>(ringCount * 12);
            for (int index = 0; index < ringCount; index++)
            {
                int corner = index / cornerSegments;
                float angle = (corner * 90f + index % cornerSegments * 90f / cornerSegments) * Mathf.Deg2Rad;
                float centerX = corner == 0 || corner == 3 ? halfWidth - radius : -halfWidth + radius;
                float centerY = corner < 2 ? halfHeight - radius : -halfHeight + radius;
                float x = centerX + Mathf.Cos(angle) * radius;
                float y = centerY + Mathf.Sin(angle) * radius;
                vertices[index] = new Vector3(x, y, -0.006f);
                vertices[index + ringCount] = new Vector3(x, y, 0.006f);
                int next = (index + 1) % ringCount;
                triangles.AddRange(new[] { ringCount * 2, next, index, ringCount * 2 + 1, index + ringCount, next + ringCount,
                    index, next, next + ringCount, index, next + ringCount, index + ringCount });
            }
            vertices[ringCount * 2] = new Vector3(0f, 0f, -0.006f);
            vertices[ringCount * 2 + 1] = new Vector3(0f, 0f, 0.006f);
            _bodyMesh = new Mesh { name = "Rounded Tablet Body" };
            _bodyMesh.vertices = vertices;
            _bodyMesh.triangles = triangles.ToArray();
            _bodyMesh.RecalculateNormals();
            _bodyMesh.RecalculateBounds();
            _bodyFilter.sharedMesh = _bodyMesh;
            body.localScale = Vector3.one;
        }

        private Sprite CreateGradientSprite(int radius, Color top, Color bottom)
        {
            Sprite sprite = CreateRoundedSprite(256, radius, 0f, top, bottom);
            _generatedSprites.Add(sprite);
            return sprite;
        }

        private Sprite CreatePixelSprite(int channel)
        {
            const int cellSize = 32;
            int columns = channel < 0 ? 9 : 5;
            int width = columns * cellSize;
            const int height = 5 * cellSize;
            Color32[] pixels = new Color32[width * height];
            Color[] channelColors = { new Color(1f, 0.35f, 0.38f), new Color(0.38f, 1f, 0.57f), new Color(0.47f, 0.59f, 1f) };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int cellX = x / cellSize + (channel < 0 ? 0 : 2);
                    int cellY = y / cellSize;
                    float value = 0.07f + (Mathf.Sin(cellX * 3.7f + cellY * 2.3f) + 1f) * 0.045f;
                    if (cellX == 4 && cellY == 2)
                    {
                        value = 1f;
                    }
                    else if (Mathf.Abs(cellX - 4) + Mathf.Abs(cellY - 2) == 1)
                    {
                        value = 0.31f;
                    }
                    Color source = new Color(value, value, value);
                    if (cellX == 5 && cellY == 2)
                    {
                        source = new Color(1f, 0.16f, 0.17f);
                    }
                    Color color = source;
                    if (channel >= 0)
                    {
                        float amount = channel == 0 ? source.r : channel == 1 ? source.g : source.b;
                        color = Color.Lerp(new Color(0.055f, 0.065f, 0.085f),
                            Color.Lerp(Color.white, channelColors[channel], 0.26f), amount);
                    }
                    if (x % cellSize == 0 || y % cellSize == 0)
                    {
                        color = Color.Lerp(color, new Color(0.45f, 0.50f, 0.58f), 0.28f);
                    }
                    float edgeX = Mathf.Max(Mathf.Max(8f - x, x - (width - 9f)), 0f);
                    float edgeY = Mathf.Max(Mathf.Max(8f - y, y - (height - 9f)), 0f);
                    color.a = Mathf.Clamp01(8.5f - Mathf.Sqrt(edgeX * edgeX + edgeY * edgeY));
                    pixels[y * width + x] = color;
                }
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = channel < 0 ? "Original Pixel Preview" : "RGB Pixel Preview " + channel;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateUnderlineSprite()
        {
            const int width = 320;
            const int height = 40;
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = x / (float)(width - 1);
                    float curve = 5f + 29f * (1f - t) * (1f - t);
                    float alpha = Mathf.Clamp01(3f - Mathf.Abs(height - 1f - y - curve)) * (1f - t * 0.75f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
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

        private void CreateSeparator(Transform parent, Vector2 position, Vector2 size)
        {
            CreateImage(parent, "Separator", position, size, SeparatorColor).sprite = null;
        }

        private Image CreateSymbol(Transform parent, string name, TabletSymbol symbol, Vector2 position, float size, Color color)
        {
            Image image = CreateImage(parent, name, position, new Vector2(size, size), color);
            image.sprite = _symbolSprites[symbol];
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
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
            rect.sizeDelta = new Vector2(size.x, Mathf.Max(size.y, fontSize * 1.5f));
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
                new Vector2(size.x - 12f, size.y - 8f), 24, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
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

        private Button CreateSymbolButton(Transform parent, string name, string label, TabletSymbol symbol,
            Vector2 position, Vector2 size, UnityAction onClick, Color color, Color foreground)
        {
            Button button = CreateTextButton(parent, name, label, position, size, onClick, color);
            Text text = button.GetComponentInChildren<Text>();
            text.color = foreground;
            text.fontSize = 24;
            text.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            text.rectTransform.sizeDelta = new Vector2(size.x - 90f, size.y - 12f);
            CreateSymbol(button.transform, name + " Symbol", symbol,
                new Vector2(-size.x * 0.5f + 32f, 0f), 28f, foreground);
            return button;
        }

        private Sprite CreateRoundedSprite(int size, int radius, float softness = 0f, Color? topColor = null, Color? bottomColor = null)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Tablet Rounded UI";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            float halfSize = size * 0.5f;
            float extent = halfSize - Mathf.Max(softness, 0.5f);
            float effectiveRadius = Mathf.Min(radius, extent);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(Mathf.Abs(x + 0.5f - halfSize), Mathf.Abs(y + 0.5f - halfSize))
                        - Vector2.one * (extent - effectiveRadius);
                    float distance = new Vector2(Mathf.Max(delta.x, 0f), Mathf.Max(delta.y, 0f)).magnitude
                        + Mathf.Min(Mathf.Max(delta.x, delta.y), 0f) - effectiveRadius;
                    float alpha = Mathf.Clamp01(0.5f - distance / Mathf.Max(1f, softness * 2f));
                    Color color = Color.Lerp(bottomColor ?? Color.white, topColor ?? Color.white, y / (float)(size - 1));
                    color.a *= alpha;
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Vector4 border = Vector4.one * Mathf.Min(halfSize, radius + softness);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        private void CreateSymbolSprites()
        {
            const int cellSize = 64;
            const int columns = 4;
            TabletSymbol[] symbols = (TabletSymbol[])Enum.GetValues(typeof(TabletSymbol));
            int width = columns * cellSize;
            int height = Mathf.CeilToInt(symbols.Length / (float)columns) * cellSize;
            Color32[] pixels = new Color32[width * height];
            foreach (TabletSymbol symbol in symbols)
            {
                List<Vector2[]> paths = GetSymbolPaths(symbol);
                int offsetX = (int)symbol % columns * cellSize;
                int offsetY = (int)symbol / columns * cellSize;
                for (int y = 0; y < cellSize; y++)
                {
                    for (int x = 0; x < cellSize; x++)
                    {
                        Vector2 point = new Vector2((x + 0.5f) * 24f / cellSize, 24f - (y + 0.5f) * 24f / cellSize);
                        float distanceSquared = float.PositiveInfinity;
                        foreach (Vector2[] path in paths)
                        {
                            for (int index = 1; index < path.Length; index++)
                            {
                                Vector2 segment = path[index] - path[index - 1];
                                float t = Mathf.Clamp01(Vector2.Dot(point - path[index - 1], segment) / segment.sqrMagnitude);
                                distanceSquared = Mathf.Min(distanceSquared, (point - path[index - 1] - segment * t).sqrMagnitude);
                            }
                        }
                        float alpha = Mathf.Clamp01((1f - Mathf.Sqrt(distanceSquared)) * cellSize / 24f + 0.5f);
                        pixels[(offsetY + y) * width + offsetX + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }
            }
            // Rasterize once and share one atlas across every tablet symbol.
            _symbolAtlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _symbolAtlas.name = "Tablet Lucide Symbols";
            _symbolAtlas.wrapMode = TextureWrapMode.Clamp;
            _symbolAtlas.filterMode = FilterMode.Bilinear;
            _symbolAtlas.SetPixels32(pixels);
            _symbolAtlas.Apply(false, true);
            foreach (TabletSymbol symbol in symbols)
            {
                Rect rect = new Rect((int)symbol % columns * cellSize, (int)symbol / columns * cellSize, cellSize, cellSize);
                _symbolSprites.Add(symbol, Sprite.Create(_symbolAtlas, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect));
            }
        }

        // Simplified Lucide line geometry: https://github.com/lucide-icons/lucide
        // ISC License: Copyright (c) 2026 Lucide Icons and Contributors
        // Permission to use, copy, modify, and/or distribute this software for any
        // purpose with or without fee is hereby granted, provided that the above
        // copyright notice and this permission notice appear in all copies.
        // THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
        // WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
        // MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
        // ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
        // WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
        // ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
        // OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
        //
        // Feather-derived check, chevrons and log-out also use the MIT License:
        // Copyright (c) 2013-present Cole Bemis
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        // The above copyright notice and this permission notice shall be included in
        // all copies or substantial portions of the Software.
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        // THE SOFTWARE.
        private List<Vector2[]> GetSymbolPaths(TabletSymbol symbol)
        {
            List<Vector2[]> paths = new List<Vector2[]>();
            switch (symbol)
            {
                case TabletSymbol.Check:
                    AddSymbolPath(paths, 20, 6, 9, 17, 4, 12);
                    break;
                case TabletSymbol.ChevronLeft:
                    AddSymbolPath(paths, 15, 18, 9, 12, 15, 6);
                    break;
                case TabletSymbol.ChevronRight:
                    AddSymbolPath(paths, 9, 18, 15, 12, 9, 6);
                    break;
                case TabletSymbol.Sun:
                    AddSymbolArc(paths, new Vector2(12f, 12f), 4f, 0f, 360f);
                    AddSymbolPath(paths, 12, 2, 12, 4);
                    AddSymbolPath(paths, 12, 20, 12, 22);
                    AddSymbolPath(paths, 4.93f, 4.93f, 6.34f, 6.34f);
                    AddSymbolPath(paths, 17.66f, 17.66f, 19.07f, 19.07f);
                    AddSymbolPath(paths, 2, 12, 4, 12);
                    AddSymbolPath(paths, 20, 12, 22, 12);
                    AddSymbolPath(paths, 6.34f, 17.66f, 4.93f, 19.07f);
                    AddSymbolPath(paths, 19.07f, 4.93f, 17.66f, 6.34f);
                    break;
                case TabletSymbol.Volume:
                    AddSymbolPath(paths, 11, 5, 6, 8, 3, 8, 2, 9, 2, 15, 3, 16, 6, 16, 11, 19, 11, 5);
                    AddSymbolArc(paths, new Vector2(12f, 12f), 5f, -36.87f, 36.87f);
                    AddSymbolArc(paths, new Vector2(13f, 12f), 9f, -45f, 45f);
                    break;
                case TabletSymbol.Settings:
                    AddSymbolPath(paths, 3, 5, 10, 5);
                    AddSymbolPath(paths, 14, 3, 14, 7);
                    AddSymbolPath(paths, 14, 5, 21, 5);
                    AddSymbolPath(paths, 3, 12, 8, 12);
                    AddSymbolPath(paths, 8, 10, 8, 14);
                    AddSymbolPath(paths, 12, 12, 21, 12);
                    AddSymbolPath(paths, 3, 19, 12, 19);
                    AddSymbolPath(paths, 16, 17, 16, 21);
                    AddSymbolPath(paths, 16, 19, 21, 19);
                    break;
                case TabletSymbol.Gear:
                    Vector2[] gear = new Vector2[49];
                    for (int index = 0; index < gear.Length; index++)
                    {
                        float angle = index * Mathf.PI * 2f / 48f;
                        float radius = index % 6 == 0 || index % 6 == 1 || index % 6 == 5 ? 10f : 7.8f;
                        gear[index] = new Vector2(12f, 12f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    }
                    paths.Add(gear);
                    AddSymbolArc(paths, new Vector2(12f, 12f), 3.2f, 0f, 360f);
                    break;
                case TabletSymbol.User:
                    AddSymbolArc(paths, new Vector2(12f, 6f), 3.5f, 0f, 360f);
                    AddSymbolArc(paths, new Vector2(12f, 21f), 7f, 180f, 360f);
                    break;
                case TabletSymbol.Bulb:
                    AddSymbolArc(paths, new Vector2(12f, 8f), 6f, 140f, 400f);
                    AddSymbolPath(paths, 7.4f, 11.9f, 9, 16, 15, 16, 16.6f, 11.9f);
                    AddSymbolPath(paths, 9, 19, 15, 19);
                    AddSymbolPath(paths, 10, 22, 14, 22);
                    break;
                case TabletSymbol.Download:
                    AddSymbolPath(paths, 12, 3, 12, 15);
                    AddSymbolPath(paths, 7, 10, 12, 15, 17, 10);
                    AddSymbolPath(paths, 3, 15, 3, 20, 4, 21, 20, 21, 21, 20, 21, 15);
                    break;
                case TabletSymbol.Expand:
                    AddSymbolPath(paths, 14, 3, 21, 3, 21, 10);
                    AddSymbolPath(paths, 21, 3, 14, 10);
                    AddSymbolPath(paths, 10, 21, 3, 21, 3, 14);
                    AddSymbolPath(paths, 3, 21, 10, 14);
                    break;
                case TabletSymbol.Box:
                    AddSymbolPath(paths, 3, 7, 12, 2, 21, 7, 21, 17, 12, 22, 3, 17, 3, 7);
                    AddSymbolPath(paths, 3.3f, 7, 12, 12, 20.7f, 7);
                    AddSymbolPath(paths, 12, 22, 12, 12);
                    break;
                case TabletSymbol.Exit:
                    AddSymbolPath(paths, 16, 17, 21, 12, 16, 7);
                    AddSymbolPath(paths, 21, 12, 9, 12);
                    AddSymbolPath(paths, 9, 21, 5, 21, 3, 19, 3, 5, 5, 3, 9, 3);
                    break;
                case TabletSymbol.Bookmark:
                    AddSymbolPath(paths, 5, 20, 5, 5, 7, 3, 17, 3, 19, 5, 19, 20, 12, 16, 5, 20);
                    break;
                case TabletSymbol.Vibrate:
                    AddSymbolPath(paths, 2, 8, 4, 10, 2, 12, 4, 14, 2, 16);
                    AddSymbolPath(paths, 22, 8, 20, 10, 22, 12, 20, 14, 22, 16);
                    AddSymbolPath(paths, 9, 5, 15, 5, 16, 6, 16, 18, 15, 19, 9, 19, 8, 18, 8, 6, 9, 5);
                    break;
                case TabletSymbol.Grid:
                    AddSymbolPath(paths, 5, 3, 19, 3, 21, 5, 21, 19, 19, 21, 5, 21, 3, 19, 3, 5, 5, 3);
                    AddSymbolPath(paths, 12, 3, 12, 21);
                    AddSymbolPath(paths, 3, 12, 21, 12);
                    break;
                case TabletSymbol.Book:
                    AddSymbolPath(paths, 12, 5, 8, 3, 4, 3, 2, 5, 2, 17, 4, 19, 8, 19, 12, 21,
                        16, 19, 20, 19, 22, 17, 22, 5, 20, 3, 16, 3, 12, 5, 12, 21);
                    break;
            }
            return paths;
        }

        private void AddSymbolPath(List<Vector2[]> paths, params float[] coordinates)
        {
            Vector2[] points = new Vector2[coordinates.Length / 2];
            for (int index = 0; index < points.Length; index++)
            {
                points[index] = new Vector2(coordinates[index * 2], coordinates[index * 2 + 1]);
            }
            paths.Add(points);
        }

        private void AddSymbolArc(List<Vector2[]> paths, Vector2 center, float radius, float startDegrees, float endDegrees)
        {
            const int segments = 24;
            Vector2[] points = new Vector2[segments + 1];
            for (int index = 0; index <= segments; index++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segments) * Mathf.Deg2Rad;
                points[index] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            paths.Add(points);
        }

        private enum TabletSymbol
        {
            Check, ChevronLeft, ChevronRight, Sun, Volume, Settings, Box, Exit, Bookmark, Vibrate, Grid, Book,
            Gear, User, Bulb, Download, Expand
        }

        private void RefreshAll()
        {
            RefreshInventory();
            RefreshTasks();
            RefreshStage();
            RefreshProgress();
            RefreshSettingsValues();
            UpdateFeedback("필터 렌즈 준비 완료",
                "선택한 양피지로 오염된 픽셀을\n관찰하세요.\n\nRGB 채널의 값을 비교하고, 필터가 주변\n픽셀에 미치는 영향을 확인해보세요.");
        }

        private void RefreshInventory()
        {
            for (int index = 0; index < _inventoryGlows.Count; index++)
            {
                _inventoryGlows[index].gameObject.SetActive(index == _selectedItemIndex);
                _inventorySurfaces[index].sprite = index == _selectedItemIndex ? _selectedSprite : _panelSprite;
                _inventorySurfaces[index].color = CardColor;
                _inventoryLabels[index].fontStyle = FontStyle.Bold;
            }
        }

        private void RefreshTasks()
        {
            int completedCount = 0;
            for (int index = 0; index < _tasks.Length; index++)
            {
                bool isDone = _tasks[index].IsCompleted;
                _taskChecks[index].sprite = isDone ? _blueCircleSprite : _mutedCircleSprite;
                _taskChecks[index].color = CardColor;
                _taskCheckmarks[index].gameObject.SetActive(isDone);
                _taskTexts[index].color = isDone ? AccentColor : MutedTextColor;
                _taskRows[index].sprite = _selectedSprite;
                _taskRows[index].color = new Color(1f, 1f, 1f, isDone ? 0.65f : 0f);
                if (isDone)
                {
                    completedCount++;
                }
            }
            _taskCount.text = completedCount + "/" + _tasks.Length;
        }

        private void RefreshStage()
        {
            string stageName = _stageNames[_selectedStageIndex];
            if (_settingsStageLabel != null)
            {
                _settingsStageLabel.text = stageName;
            }
            for (int index = 0; index < _stageSurfaces.Count; index++)
            {
                bool isCurrent = index == _selectedStageIndex;
                _stageSurfaces[index].color = isCurrent ? AccentSoftColor : CardColor;
                _stageCheckmarks[index].gameObject.SetActive(isCurrent);
            }
            int phaseIndex = Mathf.Max(0, _selectedStageIndex - 1);
            for (int index = 0; index < _phaseSurfaces.Count; index++)
            {
                bool isCurrent = index == phaseIndex;
                _phaseSurfaces[index].sprite = isCurrent ? _selectedSprite : _panelSprite;
                _phaseSurfaces[index].color = new Color(1f, 1f, 1f, isCurrent ? 0.85f : 0.015f);
                _phaseCircles[index].sprite = isCurrent ? _blueCircleSprite : _mutedCircleSprite;
                _phaseCircles[index].rectTransform.sizeDelta = Vector2.one * (isCurrent ? 54f : 42f);
                _phaseLabels[index].fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal;
                _phaseLabels[index].color = isCurrent ? TextColor : MutedTextColor;
                _phaseChevrons[index].gameObject.SetActive(isCurrent);
            }
        }

        private void RefreshProgress()
        {
            int percent = Mathf.RoundToInt(DefaultProgress * 100f);
            if (_progressPercent != null)
            {
                _progressPercent.text = "전체 진행도 <b><color=#315A96>" + percent + "%</color></b>";
            }
            if (_progressFill != null)
            {
                _progressFill.rectTransform.sizeDelta = new Vector2(ProgressTrackWidth * DefaultProgress, 14f);
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
                _vibrationTrack.color = _isVibrationEnabled ? SuccessColor : ElevatedColor;
                _vibrationThumb.anchoredPosition = new Vector2(_isVibrationEnabled ? 15f : -15f, 0f);
            }
        }

        private void SelectItem(int index)
        {
            _selectedItemIndex = index;
            RefreshInventory();
            UpdateFeedback(_itemNames[index] + " 선택됨",
                "관찰할 픽셀과 RGB 채널을 확인하세요.\n\n주변 픽셀의 값이 바뀌면 필터의 출력도 달라집니다.");
        }

        private int SettingsFocusIndex()
        {
            for (int index = 0; index < _mainTargets.Count; index++)
            {
                if (_mainTargets[index].Control == _settingsButton)
                {
                    return index;
                }
            }
            return _selectedItemIndex;
        }

        private void RefreshSaveLabels(string value)
        {
            _saveLabel.text = value;
            _dashboardSaveLabel.text = value;
        }

        private void ShowHint()
        {
            ShowDetail("필터 관찰 힌트",
                "밝은 픽셀과 붉은 픽셀의 RGB 값을 비교해보세요.\n\n" +
                "흰색은 R·G·B 값이 모두 높고, 붉은색은 R 값이 상대적으로 높습니다.\n\n" +
                "필터는 관찰하는 픽셀뿐 아니라 주변 픽셀의 값도 함께 사용합니다.", false);
        }

        private void ShowLearningInfo()
        {
            ShowDetail("학습 정보", "픽셀 구조와 CNN 필터\n미디어 · XR 디스플레이 교육\n\n" +
                "현재 단계: " + _stageNames[_selectedStageIndex] + "\n선택한 양피지: " + _itemNames[_selectedItemIndex] +
                "\n\n케디헌", false);
        }

        private void ShowDetail(string title, string body, bool showImage)
        {
            _detailReturnFocusIndex = _focusIndex;
            _detailTitle.text = title;
            _detailBody.text = body;
            _detailBody.fontSize = showImage ? 23 : 27;
            _detailBody.rectTransform.anchoredPosition = new Vector2(0f, showImage ? 211f : 60f);
            _detailBody.rectTransform.sizeDelta = new Vector2(700f, showImage ? 54f : 330f);
            _detailImage.gameObject.SetActive(showImage);
            _detailOverlay.SetActive(true);
            ResetFocus(0);
        }

        private void CloseDetail()
        {
            _detailOverlay.SetActive(false);
            ResetFocus(_detailReturnFocusIndex);
        }

        private void ToggleSettings()
        {
            if (!_isInteracting)
            {
                SetInteraction(true);
            }
            bool open = !_settingsOverlay.activeSelf;
            _detailOverlay.SetActive(false);
            _stageMenu.SetActive(false);
            SetSettingsVisible(open);
            ResetFocus(open ? 0 : SettingsFocusIndex());
        }

        private void SetSettingsVisible(bool visible)
        {
            if (_settingsAnimation != null)
            {
                StopCoroutine(_settingsAnimation);
                _settingsAnimation = null;
            }
            _settingsOverlay.SetActive(visible);
            _settingsCanvasGroup.alpha = 1f;
            _settingsSheet.anchoredPosition = Vector2.zero;
            if (visible)
            {
                _settingsAnimation = StartCoroutine(AnimateSettingsOpen());
            }
        }

        private IEnumerator AnimateSettingsOpen()
        {
            const float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                _settingsCanvasGroup.alpha = eased;
                _settingsSheet.anchoredPosition = new Vector2(0f, -6f * (1f - eased));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _settingsCanvasGroup.alpha = 1f;
            _settingsSheet.anchoredPosition = Vector2.zero;
            _settingsAnimation = null;
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
                "현재 임무를 모두 확인했습니다.\n설정에서 저장하거나 단계 이동을 선택할 수 있습니다.");
        }

        private void MoveToStage(int stageIndex)
        {
            _selectedStageIndex = Mathf.Clamp(stageIndex, 0, _stageNames.Length - 1);
            RefreshStage();
            _stageMoveRequested.Invoke(_selectedStageIndex);
            _stageMenu.SetActive(false);
            SetSettingsVisible(false);
            ResetFocus(SettingsFocusIndex());

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
            RefreshSaveLabels("저장하기");
        }

        private void SetVolume(float value)
        {
            _volume = Mathf.Clamp01(value);
            AudioListener.volume = _volume;
            RefreshSettingsValues();
            RefreshSaveLabels("저장하기");
        }

        private void ToggleVibration()
        {
            _isVibrationEnabled = !_isVibrationEnabled;
            RefreshSettingsValues();
            RefreshSaveLabels("저장하기");
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
            RefreshSaveLabels("저장 완료");
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
            Outline outline = AddOutline(control.GetComponent<Image>(), AccentColor, new Vector2(3f, 3f));
            outline.enabled = false;
            _buildingTargets.Add(new FocusTarget(control, outline));
        }

        private void SetInteraction(bool active)
        {
            _isInteracting = active;
            if (!active)
            {
                SetSettingsVisible(false);
                _stageMenu.SetActive(false);
                if (_detailOverlay != null)
                {
                    _detailOverlay.SetActive(false);
                }
            }
            ResetFocus(_selectedItemIndex);
        }

        private void CaptureMovement()
        {
            if (!_movementCaptured && _moveProvider != null)
            {
                _previousMoveInput = _moveProvider.leftHandMoveInput;
                _moveProvider.leftHandMoveInput = _blockedMoveInput;
                _movementCaptured = true;
            }
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

        private void GoBack()
        {
            if (_detailOverlay.activeSelf)
            {
                CloseDetail();
            }
            else if (_isAdjustingSlider)
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
            else if (_settingsOverlay.activeSelf)
            {
                ToggleSettings();
            }
            else
            {
                SetInteraction(false);
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
            foreach (FocusTarget target in _detailTargets)
            {
                target.Outline.enabled = false;
            }
            for (int index = 0; index < _phaseSurfaces.Count; index++)
            {
                bool isCurrent = index == Mathf.Max(0, _selectedStageIndex - 1);
                bool isFocused = _isInteracting && ActiveTargets == _mainTargets &&
                    _mainTargets[_focusIndex].Control.gameObject == _phaseSurfaces[index].gameObject;
                _phaseSurfaces[index].color = new Color(1f, 1f, 1f, isCurrent || isFocused ? 0.85f : 0.015f);
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
            _menuButton?.Disable();
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
            _menuButton?.Dispose();
            _stick?.Dispose();
            DestroyRuntimeSprite(_roundedSprite);
            DestroyRuntimeSprite(_circleSprite);
            DestroyRuntimeSprite(_sheetSprite);
            DestroyRuntimeSprite(_shadowSprite);
            DestroyRuntimeSprite(_underlineSprite);
            foreach (Sprite sprite in _generatedSprites)
            {
                DestroyRuntimeSprite(sprite);
            }
            foreach (Sprite sprite in _pixelSprites)
            {
                DestroyRuntimeSprite(sprite);
            }
            foreach (Sprite sprite in _symbolSprites.Values)
            {
                Destroy(sprite);
            }
            Destroy(_symbolAtlas);
            if (_bodyFilter != null && _bodyFilter.sharedMesh == _bodyMesh)
            {
                _bodyFilter.sharedMesh = _originalBodyMesh;
                _bodyFilter.transform.localScale = _originalBodyScale;
            }
            Destroy(_bodyMesh);
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
