using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Shared presentation values only; scene controllers still own their actions.
    public static class SceneUIStyle
    {
        public static readonly Color Surface = new Color(0.025f, 0.065f, 0.09f, 0.98f);
        public static readonly Color Button = new Color(0.07f, 0.26f, 0.31f);
        public static readonly Color Accent = new Color(0.28f, 0.95f, 0.91f);
        public static readonly Color Muted = new Color(0.72f, 0.83f, 0.87f);
        public static readonly Color Disabled = new Color(0.12f, 0.16f, 0.18f);

        public static void Apply(UnityEngine.UI.Button button)
        {
            if (button == null)
            {
                return;
            }
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.6f, 1f, 0.97f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.22f, 0.7f, 0.72f);
            colors.disabledColor = new Color(0.35f, 0.4f, 0.43f, 0.65f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (button.targetGraphic is Image image)
            {
                image.color = Button;
            }
            foreach (Text label in button.GetComponentsInChildren<Text>(true)) label.color = Color.white;
            if (button.GetComponent<SceneButtonFeedbackController>() == null)
            {
                button.gameObject.AddComponent<SceneButtonFeedbackController>();
            }
        }

        public static void ApplyButtons(Transform root)
        {
            foreach (UnityEngine.UI.Button button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Apply(button);
            }
        }
    }
}
