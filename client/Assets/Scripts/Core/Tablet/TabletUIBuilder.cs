using Parchment;
using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static UnityEngine.Object;
using static Tablet.TabletVisualResources;

namespace Tablet
{
    internal sealed class TabletUIBuilder
    {
        internal const float CanvasWidth = 1554f;
        internal const float CanvasHeight = 1012f;
        internal const float TabletWorldWidth = 0.3f;
        internal const float ProgressTrackWidth = 208f;
        internal static readonly Color SurfaceColor = new Color(0.94f, 0.97f, 0.98f, 1f);
        internal static readonly Color CardColor = Color.white;
        internal static readonly Color ElevatedColor = new Color(0.87f, 0.9f, 0.94f, 1f);
        internal static readonly Color SeparatorColor = new Color(0.88f, 0.9f, 0.93f, 1f);
        internal static readonly Color AccentColor = new Color(0.19f, 0.36f, 0.63f, 1f);
        internal static readonly Color AccentSoftColor = new Color(0.85f, 0.91f, 0.99f, 1f);
        internal static readonly Color SuccessColor = new Color(0.12f, 0.56f, 0.37f, 1f);
        internal static readonly Color WarningColor = new Color(0.97f, 0.68f, 0.13f, 1f);
        internal static readonly Color DangerColor = new Color(0.8f, 0.2f, 0.25f, 1f);
        internal static readonly Color TextColor = new Color(0.035f, 0.10f, 0.22f, 1f);
        internal static readonly Color MutedTextColor = new Color(0.32f, 0.42f, 0.56f, 1f);
        internal static readonly Color[] ItemColors =
        {
            new Color(0.34f, 0.63f, 0.77f), new Color(0.26f, 0.55f, 0.51f), new Color(0.47f, 0.27f, 0.82f),
            new Color(0.86f, 0.48f, 0.16f), new Color(0.96f, 0.25f, 0.34f)
        };

        private readonly Transform _tabletTransform;
        private readonly TabletVisualResources _resources;
        private readonly Font _resolvedFont;
        private readonly Sprite _parchmentSprite;
        private readonly TabletInputController _input;
        private readonly List<TabletMinimapController> _maps = new List<TabletMinimapController>();
        private float _initialBrightness;
        private float _initialVolume;
        private Func<int, bool> _hasParchment;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _surfaceRect;
        private TabletInputController.FocusGroup _buildingGroup;

        public event Action<int> ParchmentSelected;
        public event Action CurrentTaskRequested;
        public event Action<int> StageRequested;
        public event Action<float> BrightnessChanged;
        public event Action<float> VolumeChanged;
        public event Action VibrationRequested;
        public event Action SettingsRequested;
        public event Action LearningInfoRequested;
        public event Action FactoryMapRequested;
        public event Action HintRequested;
        public event Action ImageDetailRequested;
        public event Action SaveRequested;
        public event Action ExitRequested;
        public event Action DetailCloseRequested;
        public event Action QuitConfirmationRequested;
        public event Action QuitRequested;
        public event Action<bool> StageMenuRequested;

        public ParchmentView Parchment { get; } = new ParchmentView();
        public LearningView Learning { get; } = new LearningView();
        public SettingsView Settings { get; } = new SettingsView();
        public ShellView Shell { get; } = new ShellView();
        public RectTransform CanvasRect => _canvasRect;

        public TabletUIBuilder(Transform tabletTransform, TabletVisualResources resources, Font font,
            Sprite parchmentSprite, TabletInputController input)
        {
            _tabletTransform = tabletTransform;
            _resources = resources;
            _resolvedFont = font;
            _parchmentSprite = parchmentSprite;
            _input = input;
        }

        public void Build(float brightness, float volume, Func<int, bool> hasParchment)
        {
            _initialBrightness = brightness;
            _initialVolume = volume;
            _hasParchment = hasParchment;
            BuildInterface();
        }

        private void RegisterControl(Selectable control)
        {
            _input.RegisterControl(control, _buildingGroup);
        }

        private void BuildInterface()
        {
            _canvas = _tabletTransform.GetComponentInChildren<Canvas>(true);
            if (_canvas == null)
            {
                GameObject canvasObject = new GameObject("Tablet Canvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(_tabletTransform, false);
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
            for (int index = _canvasRect.childCount - 1; index >= 0; index--)
            {
                GameObject child = _canvasRect.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            BuildTabletFrame();
            _resources.BuildRoundedBody(_tabletTransform);

            CreateGlassPanel(_surfaceRect, "Learning Column", 66f, 186f, 354f, 764f);
            CreateGlassPanel(_surfaceRect, "Concept Column", 440f, 186f, 682f, 764f);
            CreateGlassPanel(_surfaceRect, "Inventory Column", 1144f, 186f, 346f, 764f);
            _buildingGroup = TabletInputController.FocusGroup.Main;
            // Keep the five inventory targets first so saved selection still restores focus.
            BuildInventory();
            BuildMainFeedback();
            BuildDashboardActions();
            BuildHeader();
            BuildLeftStatus();
            BuildSettingsOverlay();
            BuildDetailOverlay();
        }

        private void BuildTabletFrame()
        {
            Sprite rim = _resources.CreateGradientSprite(92, new Color(0.80f, 0.82f, 0.83f), new Color(0.37f, 0.39f, 0.40f));
            PlaceImage(_canvasRect, "Metal Rim", 0f, 0f, 1554f, 1012f, Color.white).sprite = rim;
            Sprite bezel = _resources.CreateGradientSprite(88, new Color(0.055f, 0.06f, 0.065f), new Color(0.015f, 0.02f, 0.025f));
            PlaceImage(_canvasRect, "Black Bezel", 5f, 5f, 1544f, 1002f, Color.white).sprite = bezel;
            Image screen = PlaceImage(_canvasRect, "Tablet Surface", 40f, 38f, 1474f, 940f, Color.white);
            screen.sprite = _resources.CreateGradientSprite(38, new Color(0.98f, 0.99f, 1f), new Color(0.90f, 0.95f, 0.97f));
            screen.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _surfaceRect = screen.rectTransform;
            Image camera = PlaceImage(_canvasRect, "Front Camera", 772f, 17f, 11f, 11f, new Color(0.025f, 0.05f, 0.09f));
            camera.sprite = _resources.CircleSprite;
            PlaceImage(_canvasRect, "Camera Lens", 776f, 21f, 3f, 3f, new Color(0.16f, 0.31f, 0.52f)).sprite = _resources.CircleSprite;
        }

        private void BuildHeader()
        {
            Image bookTile = CreateGlassPanel(_surfaceRect, "Education Symbol Tile", 106f, 72f, 110f, 92f);
            bookTile.sprite = _resources.CreateGradientSprite(26, new Color(0.89f, 0.95f, 0.97f), new Color(0.75f, 0.85f, 0.91f));
            PlaceSymbol(_surfaceRect, "Education Book", TabletSymbol.Book, 132f, 94f, 58f, AccentColor);
            PlaceText(_surfaceRect, "Education Title", "픽셀 구조와 CNN 필터", 264f, 77f, 660f, 60f,
                41, FontStyle.Bold, TextColor);
            PlaceText(_surfaceRect, "Team Name", "미디어 · XR 디스플레이 교육", 265f, 133f, 620f, 32f,
                24, FontStyle.Normal, MutedTextColor);

            CreateGlassPanel(_surfaceRect, "Progress Capsule", 945f, 98f, 226f, 32f).sprite = _resources.RoundedSprite;
            PlaceImage(_surfaceRect, "Progress Track", 954f, 107f, ProgressTrackWidth, 14f, ElevatedColor);
            Learning.ProgressFill = PlaceImage(_surfaceRect, "Progress Fill", 954f, 107f, ProgressTrackWidth, 14f, Color.white);
            Learning.ProgressFill.sprite = _resources.PrimarySprite;
            Learning.ProgressFill.pixelsPerUnitMultiplier = 6f;
            Learning.ProgressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            Learning.ProgressFill.rectTransform.anchoredPosition = ReferencePosition(_surfaceRect, 954f, 114f);
            Learning.ProgressPercent = PlaceText(_surfaceRect, "Progress Percent", "", 1186f, 99f, 150f, 32f,
                20, FontStyle.Normal, MutedTextColor);
            Shell.SettingsButton = PlaceIconButton(_surfaceRect, "설정", TabletSymbol.Gear, 1353f, 85f, 58f, () => SettingsRequested?.Invoke());
            PlaceIconButton(_surfaceRect, "학습 정보", TabletSymbol.User, 1428f, 85f, 58f, () => LearningInfoRequested?.Invoke());
        }

        private void BuildLeftStatus()
        {
            PlaceText(_surfaceRect, "Learning Stage Caption", "학습 단계", 107f, 212f, 260f, 36f,
                23, FontStyle.Bold, TextColor);
            PlaceImage(_surfaceRect, "Stage Connector", 127f, 300f, 2f, 228f, new Color(0.80f, 0.84f, 0.88f)).sprite = null;
            for (int index = 0; index < TabletLearningContent.PhaseNames.Count; index++)
            {
                int phaseIndex = index;
                float centerY = 291f + index * 59f;
                Button phase = PlaceButton(_surfaceRect, "Learning Phase " + index, "", 91f, centerY - 31f, 307f, 62f,
                    () => StageRequested?.Invoke(phaseIndex), SurfaceColor);
                Learning.PhaseSurfaces.Add(phase.GetComponent<Image>());
                Image circle = PlaceImage(phase.transform, "Phase Number Circle", 101f, centerY - 27f, 54f, 54f, Color.white);
                circle.sprite = _resources.MutedCircleSprite;
                Learning.PhaseCircles.Add(circle);
                PlaceText(phase.transform, "Phase Number", (index + 1).ToString(), 101f, centerY - 22f, 54f, 44f,
                    21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                Learning.PhaseLabels.Add(PlaceText(phase.transform, "Phase Label", TabletLearningContent.PhaseNames[index], 173f, centerY - 20f, 185f, 40f,
                    24, FontStyle.Normal, MutedTextColor));
                Learning.PhaseChevrons.Add(PlaceSymbol(phase.transform, "Phase Chevron", TabletSymbol.ChevronRight,
                    363f, centerY - 9f, 18f, AccentColor));
            }

            Image tasks = CreateGlassPanel(_surfaceRect, "Tasks", 85f, 573f, 317f, 211f);
            PlaceText(tasks.transform, "Task Caption", "현재 임무", 113f, 592f, 195f, 34f,
                21, FontStyle.Bold, TextColor);
            Learning.TaskCount = PlaceText(tasks.transform, "Task Count", "", 326f, 592f, 48f, 34f,
                21, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            for (int index = 0; index < TabletLearningContent.TaskLabels.Count; index++)
            {
                float top = 633f + index * 47f;
                Learning.TaskRows.Add(PlaceImage(tasks.transform, "Task Row " + index, 105f, top, 279f, 46f, CardColor));
                Image check = PlaceImage(tasks.transform, "Task Check " + index, 116f, top + 9f, 30f, 30f, ElevatedColor);
                check.sprite = _resources.MutedCircleSprite;
                Learning.TaskChecks.Add(check);
                Learning.TaskCheckmarks.Add(PlaceSymbol(tasks.transform, "Completed " + index, TabletSymbol.Check,
                    122f, top + 15f, 18f, Color.white));
                Learning.TaskTexts.Add(PlaceText(tasks.transform, "Task Text " + index, TabletLearningContent.TaskLabels[index],
                    168f, top + 6f, 214f, 34f, 19, FontStyle.Normal, MutedTextColor));
            }
            BuildMinimap();
        }

        private void BuildMinimap()
        {
            Button map = PlaceButton(_surfaceRect, "Factory Minimap", "", 85f, 792f, 317f, 137f,
                () => FactoryMapRequested?.Invoke(), CardColor);
            map.GetComponent<Image>().sprite = _resources.PanelSprite;
            PlaceText(map.transform, "Map Caption", "현재 위치", 102f, 796f, 230f, 29f, 18, FontStyle.Bold, TextColor);
            PlaceSymbol(map.transform, "Expand Map", TabletSymbol.Expand, 369f, 802f, 17f, AccentColor);
            Text status = PlaceText(map.transform, "Current Factory Room", "", 102f, 900f, 230f, 25f,
                16, FontStyle.Normal, MutedTextColor);
            PlaceText(map.transform, "North", "N ↑", 342f, 900f, 42f, 25f, 15, FontStyle.Bold, AccentColor,
                TextAnchor.MiddleRight);
            Image viewport = PlaceImage(map.transform, "Nearby Factory Map", 99f, 829f, 289f, 70f, CardColor);
            AddMinimapGraphic(viewport, status, false);
        }

        private void AddMinimapGraphic(Image viewport, Text status, bool overview)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
            GameObject geometry = new GameObject("Factory Map Geometry", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TabletMinimapController));
            geometry.layer = _canvas.gameObject.layer;
            geometry.transform.SetParent(viewport.transform, false);
            RectTransform rect = geometry.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            XROrigin origin = _tabletTransform.GetComponentInParent<XROrigin>();
            Transform player = origin != null && origin.Camera != null ? origin.Camera.transform :
                _canvas.worldCamera != null ? _canvas.worldCamera.transform : null;
            TabletMinimapController map = geometry.GetComponent<TabletMinimapController>();
            map.Initialize(player, _resolvedFont, status, overview, _hasParchment);
            _maps.Add(map);
        }

        public void SetMapData(TabletMapData data)
        {
            foreach (TabletMinimapController map in _maps)
            {
                map.SetMapData(data);
            }
        }

        private void BuildInventory()
        {
            PlaceSymbol(_surfaceRect, "Inventory Symbol", TabletSymbol.Box, 1179f, 215f, 40f, AccentColor);
            PlaceText(_surfaceRect, "Inventory Caption", "아이템", 1241f, 215f, 206f, 40f,
                26, FontStyle.Bold, TextColor);
            for (int index = 0; index < ParchmentCatalog.All.Count; index++)
            {
                int itemIndex = index;
                float top = 272f + index * 103f;
                Image glow = PlaceImage(_surfaceRect, "Inventory Glow " + index, 1158f, top - 7f, 318f, 104f,
                    new Color(0.59f, 0.72f, 0.94f, 0.24f));
                glow.sprite = _resources.ShadowSprite;
                Parchment.InventoryGlows.Add(glow);
                Button item = PlaceButton(_surfaceRect, "Inventory " + index, "", 1166f, top, 302f, 90f,
                    () => ParchmentSelected?.Invoke(itemIndex), CardColor);
                Parchment.InventoryButtons.Add(item);
                Image surface = item.GetComponent<Image>();
                surface.sprite = _resources.PanelSprite;
                Parchment.InventorySurfaces.Add(surface);
                Parchment.InventoryLabels.Add(PlaceText(item.transform, "Parchment Name", "양피지 " + (index + 1).ToString("00"), 1259f, top + 14f, 164f, 34f,
                    21, FontStyle.Bold, TextColor));
                Parchment.InventoryDescriptions.Add(PlaceText(item.transform, "Parchment Description", "", 1259f, top + 48f, 182f, 29f,
                    18, FontStyle.Normal, MutedTextColor));
                Image icon = PlaceImage(item.transform, "Parchment Icon", 1176f, top + 24f, 70f, 40f, Color.white);
                icon.sprite = _parchmentSprite;
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                PlaceSymbol(item.transform, "Disclosure", TabletSymbol.ChevronRight, 1434f, top + 36f, 18f, AccentColor);
            }
        }

        private void BuildMainFeedback()
        {
            Shell.FeedbackRoot = new GameObject("Concept Feedback", typeof(RectTransform));
            Shell.FeedbackRoot.transform.SetParent(_surfaceRect, false);
            Image guideTile = CreateGlassPanel(Shell.FeedbackRoot.transform, "Guide Symbol Tile", 472f, 214f, 58f, 56f);
            guideTile.sprite = _resources.SelectedSprite;
            PlaceSymbol(Shell.FeedbackRoot.transform, "Guide Symbol", TabletSymbol.Book, 485f, 227f, 32f, AccentColor);
            PlaceText(Shell.FeedbackRoot.transform, "Concept Caption", "개념 설명 · 힌트톡", 552f, 224f, 350f, 39f,
                24, FontStyle.Bold, new Color(0.13f, 0.29f, 0.64f));
            Shell.FeedbackTitle = PlaceText(Shell.FeedbackRoot.transform, "Feedback Title", "", 485f, 301f, 605f, 67f,
                46, FontStyle.Bold, TextColor);
            Image underline = PlaceImage(Shell.FeedbackRoot.transform, "Title Underline", 485f, 350f, 296f, 36f, new Color(0.66f, 0.79f, 0.95f));
            underline.sprite = _resources.UnderlineSprite;
            underline.type = Image.Type.Simple;
            Shell.FeedbackBody = PlaceText(Shell.FeedbackRoot.transform, "Feedback Body", "", 485f, 405f, 600f, 180f,
                26, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            Shell.FeedbackBody.lineSpacing = 0.91f;
            Shell.FeedbackBody.resizeTextForBestFit = true;
            Shell.FeedbackBody.resizeTextMinSize = 23;
            Shell.FeedbackBody.resizeTextMaxSize = 26;
            BuildPixelPreviews();

            Image actionShadow = PlaceImage(Shell.FeedbackRoot.transform, "Confirm Shadow", 452f, 823f, 658f, 115f,
                new Color(0.18f, 0.36f, 0.59f, 0.28f));
            actionShadow.sprite = _resources.ShadowSprite;
            Button confirm = PlaceButton(Shell.FeedbackRoot.transform, "Confirm Task", "현재 미션 확인", 465f, 827f, 632f, 95f,
                () => CurrentTaskRequested?.Invoke(), CardColor);
            confirm.GetComponent<Image>().sprite = _resources.PrimarySprite;
            AddOutline(confirm.GetComponent<Image>(), new Color(0.53f, 0.69f, 0.91f, 0.85f), new Vector2(2f, 2f));
            Text confirmLabel = confirm.GetComponentInChildren<Text>();
            confirmLabel.fontSize = 27;
            confirmLabel.fontStyle = FontStyle.Normal;
            confirmLabel.color = Color.white;
            confirmLabel.rectTransform.anchoredPosition = new Vector2(44f, 0f);
            confirmLabel.rectTransform.sizeDelta = new Vector2(430f, 80f);
            PlaceSymbol(confirm.transform, "Confirm Check", TabletSymbol.Check, 683f, 859f, 29f, Color.white);
            PlaceSymbol(confirm.transform, "Confirm Disclosure", TabletSymbol.ChevronRight, 1035f, 862f, 24f, Color.white);
            Button hint = PlaceButton(Shell.FeedbackRoot.transform, "Hint", "", 943f, 213f, 154f, 58f, () => HintRequested?.Invoke(), CardColor);
            PlaceSymbol(hint.transform, "Hint Bulb", TabletSymbol.Bulb, 963f, 226f, 32f, new Color(1f, 0.70f, 0.20f));
            PlaceText(hint.transform, "Hint Label", "힌트 보기", 1005f, 225f, 88f, 34f, 20, FontStyle.Normal, MutedTextColor);
            Button expand = PlaceButton(Shell.FeedbackRoot.transform, "Expand Image", "", 657f, 659f, 51f, 49f,
                () => ImageDetailRequested?.Invoke(), new Color(0.31f, 0.35f, 0.41f, 0.8f));
            PlaceSymbol(expand.transform, "Expand Symbol", TabletSymbol.Expand, 670f, 672f, 25f, Color.white);
        }

        private void BuildPixelPreviews()
        {
            Image original = CreateGlassPanel(Shell.FeedbackRoot.transform, "Original Image Panel", 465f, 597f, 267f, 205f);
            PlaceText(original.transform, "Original Image Caption", "원본 이미지", 487f, 616f, 220f, 32f,
                21, FontStyle.Bold, TextColor);
            Image pixels = PlaceImage(original.transform, "Original Pixels", 488f, 658f, 220f, 122f, CardColor);
            pixels.sprite = _resources.PixelSprites[0];
            pixels.type = Image.Type.Simple;

            Image channels = CreateGlassPanel(Shell.FeedbackRoot.transform, "RGB Channels Panel", 745f, 597f, 352f, 205f);
            PlaceText(channels.transform, "RGB Caption", "RGB 채널 분해", 767f, 616f, 308f, 32f,
                21, FontStyle.Bold, TextColor);
            Color[] colors = { new Color(1f, 0.13f, 0.18f), new Color(0.05f, 0.76f, 0.27f), new Color(0.05f, 0.33f, 1f) };
            string[] labels = { "R", "G", "B" };
            for (int index = 0; index < 3; index++)
            {
                float x = 767f + index * 106f;
                Image channel = PlaceImage(channels.transform, labels[index] + " Channel", x, 659f, 92f, 87f, CardColor);
                channel.sprite = _resources.PixelSprites[index + 1];
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
            Button save = PlaceButton(_surfaceRect, "Save Dashboard", "", 1162f, 806f, 143f, 118f, () => SaveRequested?.Invoke(),
                new Color(0.88f, 0.95f, 0.96f));
            PlaceSymbol(save.transform, "Save Symbol", TabletSymbol.Download, 1213f, 830f, 38f, new Color(0.16f, 0.39f, 0.47f));
            Settings.DashboardSaveLabel = PlaceText(save.transform, "Save Label", "저장하기", 1174f, 878f, 119f, 34f,
                22, FontStyle.Normal, new Color(0.16f, 0.30f, 0.43f), TextAnchor.MiddleCenter);
            Button exit = PlaceButton(_surfaceRect, "Exit Dashboard", "", 1318f, 806f, 153f, 118f, () => ExitRequested?.Invoke(),
                new Color(1f, 0.92f, 0.94f));
            PlaceSymbol(exit.transform, "Exit Symbol", TabletSymbol.Exit, 1373f, 831f, 38f, ItemColors[4]);
            PlaceText(exit.transform, "Exit Label", "나가기", 1335f, 878f, 119f, 34f,
                22, FontStyle.Normal, ItemColors[4], TextAnchor.MiddleCenter);
        }

        private void BuildDetailOverlay()
        {
            _buildingGroup = TabletInputController.FocusGroup.Detail;
            Image overlay = CreateImage(_surfaceRect, "Detail Overlay", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), new Color(0.07f, 0.12f, 0.22f, 0.38f));
            Shell.DetailOverlay = overlay.gameObject;
            Image modal = CreateImage(overlay.transform, "Detail Sheet", Vector2.zero, new Vector2(800f, 710f), CardColor);
            modal.sprite = _resources.SheetSprite;
            Shell.DetailTitle = CreateText(modal.transform, "Detail Title", "", new Vector2(0f, 280f),
                new Vector2(700f, 64f), 38, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Shell.DetailBody = CreateText(modal.transform, "Detail Body", "", new Vector2(0f, 80f),
                new Vector2(700f, 260f), 27, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            Shell.DetailImage = CreateImage(modal.transform, "Detail Image", new Vector2(0f, -8f), new Vector2(700f, 388f), CardColor);
            Shell.DetailImage.sprite = _resources.PixelSprites[0];
            Shell.DetailImage.type = Image.Type.Simple;
            Image mapView = CreateImage(modal.transform, "Factory Overview", new Vector2(0f, -5f),
                new Vector2(700f, 480f), CardColor);
            Shell.FactoryMapView = mapView.gameObject;
            Text mapStatus = CreateText(mapView.transform, "Factory Map Status", "", new Vector2(0f, 214f),
                new Vector2(680f, 34f), 20, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleCenter);
            Image mapViewport = CreateImage(mapView.transform, "Full Factory Map", new Vector2(0f, -18f),
                new Vector2(700f, 425f), CardColor);
            AddMinimapGraphic(mapViewport, mapStatus, true);
            Shell.FactoryMapView.SetActive(false);
            Button close = CreateTextButton(modal.transform, "Close Detail", "완료", new Vector2(0f, -280f),
                new Vector2(700f, 64f), () => DetailCloseRequested?.Invoke(), AccentColor);
            close.GetComponentInChildren<Text>().fontSize = 26;
            close.GetComponentInChildren<Text>().color = CardColor;
            Shell.DetailCloseButton = close;
            Shell.ConfirmQuitButton = CreateTextButton(modal.transform, "Confirm Application Quit", "확인 · 앱 종료",
                new Vector2(0f, -190f), new Vector2(700f, 64f), () => QuitRequested?.Invoke(), AccentSoftColor);
            Shell.ConfirmQuitButton.gameObject.SetActive(false);
            Shell.DetailOverlay.SetActive(false);
        }

        private void BuildSettingsOverlay()
        {
            _buildingGroup = TabletInputController.FocusGroup.Settings;
            Image overlay = CreateImage(_surfaceRect, "Settings Overlay", Vector2.zero,
                new Vector2(CanvasWidth, CanvasHeight), new Color(0.07f, 0.12f, 0.22f, 0.38f));
            Settings.SettingsOverlay = overlay.gameObject;
            Settings.SettingsCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            Image shadow = CreateImage(overlay.transform, "Settings Shadow", new Vector2(0f, -12f),
                new Vector2(848f, 788f), new Color(0.08f, 0.17f, 0.3f, 0.25f));
            shadow.sprite = _resources.ShadowSprite;
            Image modal = CreateImage(overlay.transform, "Settings Sheet", Vector2.zero, new Vector2(800f, 740f), SurfaceColor);
            modal.sprite = _resources.SheetSprite;
            Settings.SettingsSheet = modal.rectTransform;
            CreateText(modal.transform, "Settings Title", "설정", new Vector2(-240f, 305f),
                new Vector2(240f, 60f), 40, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Learning.SettingsStageLabel = CreateText(modal.transform, "Settings Stage", "", new Vector2(100f, 305f),
                new Vector2(520f, 50f), 23, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            CreateSeparator(modal.transform, new Vector2(0f, 255f), new Vector2(720f, 1f));
            Button stages = CreateSymbolButton(modal.transform, "Choose Stage", "단계 이동", TabletSymbol.Grid,
                new Vector2(-120f, 200f), new Vector2(480f, 72f), () => StageMenuRequested?.Invoke(true), CardColor, TextColor);
            stages.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            CreateSymbol(stages.transform, "Disclosure", TabletSymbol.ChevronRight, new Vector2(205f, 0f), 28f, AccentColor);
            Button save = CreateSymbolButton(modal.transform, "Save Settings", "저장하기", TabletSymbol.Download,
                new Vector2(252f, 200f), new Vector2(216f, 72f), () => SaveRequested?.Invoke(), AccentSoftColor, AccentColor);
            Settings.SaveLabel = save.GetComponentInChildren<Text>();
            BuildSettingSlider(modal.rectTransform, "Brightness", "밝기", TabletSymbol.Sun, new Vector2(0f, 80f),
                _initialBrightness, value => BrightnessChanged?.Invoke(value), out Settings.BrightnessValue);
            BuildSettingSlider(modal.rectTransform, "Volume", "볼륨", TabletSymbol.Volume, new Vector2(0f, -30f),
                _initialVolume, value => VolumeChanged?.Invoke(value), out Settings.VolumeValue);

            Button vibration = CreateTextButton(modal.transform, "Vibration", "", new Vector2(0f, -145f),
                new Vector2(720f, 82f), () => VibrationRequested?.Invoke(), CardColor);
            CreateSymbol(vibration.transform, "Vibration Symbol", TabletSymbol.Vibrate, new Vector2(-320f, 0f), 34f, MutedTextColor);
            CreateText(vibration.transform, "Vibration Label", "진동", new Vector2(-240f, 0f),
                new Vector2(100f, 50f), 24, FontStyle.Normal, TextColor, TextAnchor.MiddleLeft);
            Settings.VibrationValue = CreateText(vibration.transform, "Vibration Value", "", new Vector2(190f, 0f),
                new Vector2(100f, 50f), 23, FontStyle.Normal, MutedTextColor, TextAnchor.MiddleRight);
            Settings.VibrationTrack = CreateImage(vibration.transform, "Vibration Switch", new Vector2(290f, 0f),
                new Vector2(74f, 44f), SuccessColor);
            Settings.VibrationThumb = CreateImage(Settings.VibrationTrack.transform, "Switch Thumb", new Vector2(15f, 0f),
                new Vector2(36f, 36f), CardColor).rectTransform;
            Settings.VibrationThumb.GetComponent<Image>().sprite = _resources.CircleSprite;
            CreateSymbolButton(modal.transform, "Close Settings", "완료", TabletSymbol.Check,
                new Vector2(42f, -285f), new Vector2(200f, 70f), () => SettingsRequested?.Invoke(), AccentColor, CardColor);
            CreateSymbolButton(modal.transform, "Exit Settings", "앱 종료", TabletSymbol.Exit,
                new Vector2(260f, -285f), new Vector2(200f, 70f), () => QuitConfirmationRequested?.Invoke(),
                new Color(1f, 0.92f, 0.94f), ItemColors[4]);

            _buildingGroup = TabletInputController.FocusGroup.Stage;
            Image stagePanel = CreateImage(modal.transform, "Stage Menu", Vector2.zero, new Vector2(800f, 740f), SurfaceColor);
            stagePanel.sprite = _resources.SheetSprite;
            Shell.StageMenu = stagePanel.gameObject;
            CreateText(stagePanel.transform, "Stage Menu Title", "단계 이동", new Vector2(0f, 305f),
                new Vector2(720f, 60f), 40, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            for (int index = 0; index < TabletLearningContent.StageNames.Count; index++)
            {
                int stageIndex = index;
                Button stage = CreateTextButton(stagePanel.transform, "Stage " + index, TabletLearningContent.StageNames[index],
                    new Vector2(0f, 220f - index * 80f), new Vector2(720f, 70f),
                    () => StageRequested?.Invoke(stageIndex), CardColor);
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
                Learning.StageSurfaces.Add(stage.GetComponent<Image>());
                Learning.StageCheckmarks.Add(CreateSymbol(stage.transform, "Current Stage", TabletSymbol.Check,
                    new Vector2(318f, 0f), 30f, AccentColor));
            }
            CreateSymbolButton(stagePanel.transform, "Back to Settings", "설정으로", TabletSymbol.ChevronLeft,
                new Vector2(240f, -295f), new Vector2(240f, 62f), () => StageMenuRequested?.Invoke(false), CardColor, AccentColor);
            Shell.StageMenu.SetActive(false);
            Settings.SettingsOverlay.SetActive(false);
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
            handle.sprite = _resources.CircleSprite;
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

        public Vector2 ReferencePosition(Transform parent, float x, float y)
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
            shadow.sprite = _resources.ShadowSprite;
            Image panel = PlaceImage(parent, name, x, y, width, height, CardColor);
            panel.sprite = _resources.PanelSprite;
            AddOutline(panel, new Color(1f, 1f, 1f, 0.68f), Vector2.one);
            return panel;
        }

        private Button PlaceIconButton(Transform parent, string name, TabletSymbol symbol, float x, float y, float size,
            UnityAction action)
        {
            Image shadow = PlaceImage(parent, name + " Shadow", x - 8f, y - 2f, size + 16f, size + 18f,
                new Color(0.20f, 0.33f, 0.47f, 0.12f));
            shadow.sprite = _resources.ShadowSprite;
            Button button = PlaceButton(parent, name, "", x, y, size, size, action, CardColor);
            button.GetComponent<Image>().sprite = _resources.CircleSprite;
            PlaceSymbol(button.transform, name + " Symbol", symbol, x + 15f, y + 15f, size - 30f, AccentColor);
            return button;
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
            image.sprite = _resources.RoundedSprite;
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
            image.sprite = _resources.SymbolSprites[symbol];
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

        // These bundles transfer UI references only; each controller owns its feature state.
        internal sealed class ParchmentView
        {
            internal readonly List<Image> InventoryGlows = new List<Image>();
            internal readonly List<Image> InventorySurfaces = new List<Image>();
            internal readonly List<Text> InventoryLabels = new List<Text>();
            internal readonly List<Text> InventoryDescriptions = new List<Text>();
            internal readonly List<Button> InventoryButtons = new List<Button>();

        }

        internal sealed class LearningView
        {
            internal readonly List<Image> PhaseSurfaces = new List<Image>();
            internal readonly List<Image> PhaseCircles = new List<Image>();
            internal readonly List<Text> PhaseLabels = new List<Text>();
            internal readonly List<Image> PhaseChevrons = new List<Image>();
            internal readonly List<Image> TaskRows = new List<Image>();
            internal Text ProgressPercent;
            internal Image ProgressFill;
            internal readonly List<Text> TaskTexts = new List<Text>();
            internal readonly List<Image> TaskChecks = new List<Image>();
            internal readonly List<Image> TaskCheckmarks = new List<Image>();
            internal readonly List<Image> StageSurfaces = new List<Image>();
            internal readonly List<Image> StageCheckmarks = new List<Image>();
            internal Text TaskCount;
            internal Text SettingsStageLabel;
        }

        internal sealed class SettingsView
        {
            internal RectTransform SettingsSheet;
            internal CanvasGroup SettingsCanvasGroup;
            internal Text SaveLabel;
            internal Text DashboardSaveLabel;
            internal GameObject SettingsOverlay;
            internal Text BrightnessValue;
            internal Text VolumeValue;
            internal Text VibrationValue;
            internal Image VibrationTrack;
            internal RectTransform VibrationThumb;
        }

        internal sealed class ShellView
        {
            internal Button SettingsButton;
            internal GameObject FeedbackRoot;
            internal Text FeedbackTitle;
            internal Text FeedbackBody;
            internal GameObject DetailOverlay;
            internal Button DetailCloseButton;
            internal Button ConfirmQuitButton;
            internal Text DetailTitle;
            internal Text DetailBody;
            internal Image DetailImage;
            internal GameObject FactoryMapView;
            internal GameObject StageMenu;
        }
    }
}
