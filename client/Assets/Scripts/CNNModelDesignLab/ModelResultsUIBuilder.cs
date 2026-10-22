using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ModelDesignScene
{
    internal sealed class ModelResultsUIBuilder
    {
        internal static readonly Color SurfaceColor = UI.SceneUIStyle.Surface;
        internal static readonly Color MutedColor = UI.SceneUIStyle.Muted;
        internal static readonly Color TrainingColor = new Color(0.27f, 0.81f, 0.91f);
        internal static readonly Color ValidationColor = new Color(1f, 0.72f, 0.29f);
        internal static readonly Color ButtonColor = UI.SceneUIStyle.Button;

        private readonly Font _font;

        public ModelResultsUIBuilder(Font font)
        {
            _font = font;
        }

        public RectTransform BuildRoot(Transform transform)
        {
            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(1200, 1050);
            Canvas canvas = transform.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = transform.gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            TrackedDeviceGraphicRaycaster raycaster = transform.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = transform.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            raycaster.enabled = true;
            CanvasScaler scaler = transform.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = transform.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.dynamicPixelsPerUnit = 4;
            Box(root, "Surface", 0, 0, 1200, 1050, SurfaceColor);
            return root;
        }

        public RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public Image Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size)
        {
            Text text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.text = value;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public Button TextButton(Transform parent, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            Image surface = Box(parent, label, x, y, width, 36, ButtonColor);
            Button button = CreateButton(surface, action);
            Text text = Label(surface.transform, "Label", label, 0, 0, width, 36, 19);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        public Button CreateButton(Image surface, UnityEngine.Events.UnityAction action)
        {
            surface.raycastTarget = true;
            Button button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(action);
            UI.SceneUIStyle.Apply(button);
            return button;
        }
    }
}
