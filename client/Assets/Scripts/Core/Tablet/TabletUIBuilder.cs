using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tablet
{
    // Fixed hierarchy and captions live in TabletUI.prefab.
    public sealed class TabletUIBuilder : MonoBehaviour
    {
        internal const float CanvasWidth = 1200f;
        internal const float CanvasHeight = 900f;
        internal const float TabletWorldWidth = .3f;
        internal static readonly Color AccentColor = new Color(.26f, .43f, .55f);
        internal static readonly Color WarningColor = new Color(.82f, .55f, .22f);
        internal static readonly Color CardColor = Color.white;
        internal static readonly Color AccentSoftColor = new Color(.86f, .92f, .96f);
        [Header("Tablet surfaces")]
        [SerializeField] internal Image Frame;
        [SerializeField] internal Image Bezel;
        [SerializeField] internal Image Screen;
        [SerializeField] internal GameObject[] AppPanels;
        [SerializeField] internal Button[] AppButtons;
        [SerializeField] internal Image[] RoundedSurfaces;
        [Header("Learning and collection")]
        [SerializeField] internal Button[] StageButtons;
        [SerializeField] internal Text[] StageStates;
        [SerializeField] internal Button[] PaperButtons;
        [SerializeField] internal Text[] PaperTitles;
        [SerializeField] internal Text[] PaperSubtitles;
        [SerializeField] internal Text PaperCount;
        [SerializeField] internal GameObject EmptyCollection;
        [SerializeField] internal Image ProgressFill;
        [SerializeField] internal Text ProgressLabel;
        [Header("Nearby map")]
        [SerializeField] internal TabletMinimapController Minimap;
        [SerializeField] internal Text MapStatus;
        [SerializeField] internal Text Status;
        [Header("Settings and exit")]
        [SerializeField] internal Button SettingsButton;
        [SerializeField] internal GameObject SettingsPanel;
        [SerializeField] internal Slider Brightness;
        [SerializeField] internal Slider Volume;
        [SerializeField] internal Text BrightnessValue;
        [SerializeField] internal Text VolumeValue;
        [SerializeField] internal Button Vibration;
        [SerializeField] internal Text VibrationValue;
        [SerializeField] internal Button Save;
        [SerializeField] internal Text SaveLabel;
        [SerializeField] internal Button SettingsClose;
        [SerializeField] internal Button Quit;
        [SerializeField] internal GameObject QuitPanel;
        [SerializeField] internal Button QuitConfirm;
        [SerializeField] internal Button QuitCancel;
        internal int SelectedApp { get; private set; }
        internal event Action<int> AppRequested;
        internal event Action<int> StageRequested;
        internal event Action<int> PaperRequested;
        internal void Initialize(TabletVisualResources resources, TabletInputController input, Transform player, Func<int, bool> collected)
        {
            foreach (Image surface in RoundedSurfaces)
            {
                surface.sprite = resources.RoundedSprite;
                surface.type = Image.Type.Sliced;
            }
            Frame.sprite = resources.CreateGradientSprite(70, new Color(.8f, .88f, .92f), new Color(.40f, .53f, .62f));
            Bezel.sprite = resources.CreateGradientSprite(68, new Color(.12f, .20f, .25f), new Color(.05f, .09f, .12f));
            Screen.sprite = resources.CreateGradientSprite(50, Color.white, new Color(.92f, .96f, .98f));
            Frame.color = Bezel.color = Screen.color = Color.white;
            for (int i = 0; i < AppButtons.Length; i++)
            {
                int index = i;
                AppButtons[i].onClick.AddListener(() => AppRequested?.Invoke(index));
                input.RegisterControl(AppButtons[i], TabletInputController.FocusGroup.Main);
            }
            for (int i = 0; i < StageButtons.Length; i++)
            {
                int index = i;
                StageButtons[i].onClick.AddListener(() => StageRequested?.Invoke(index));
                input.RegisterControl(StageButtons[i], TabletInputController.FocusGroup.Main);
            }
            for (int i = 0; i < PaperButtons.Length; i++)
            {
                int index = i;
                PaperButtons[i].onClick.AddListener(() => PaperRequested?.Invoke(index));
                input.RegisterControl(PaperButtons[i], TabletInputController.FocusGroup.Main);
            }
            input.RegisterControl(SettingsButton, TabletInputController.FocusGroup.Main);
            Selectable[] settings = { Brightness, Volume, Vibration, Save, SettingsClose, Quit };
            foreach (Selectable control in settings)
            {
                input.RegisterControl(control, TabletInputController.FocusGroup.Settings);
            }
            input.RegisterControl(QuitCancel, TabletInputController.FocusGroup.Detail);
            input.RegisterControl(QuitConfirm, TabletInputController.FocusGroup.Detail);
            Minimap.Initialize(player, MapStatus.font, MapStatus, false, collected);
            ProgressFill.sprite = resources.RoundedSprite;
            ProgressFill.type = Image.Type.Filled;
            ShowApp(0);
        }
        internal void ShowApp(int index)
        {
            SelectedApp = index;
            for (int i = 0; i < AppPanels.Length; i++)
            {
                AppPanels[i].SetActive(i == index);
                AppButtons[i].GetComponent<Image>().color = i == index ? AccentSoftColor : CardColor;
            }
        }
    }
}
