using UnityEngine;

namespace CNNFeatureMaps.Lesson
{
    public sealed class LessonButtonGroupController : MonoBehaviour
    {
        [SerializeField] private LessonButton[] _buttons;

        public event System.Action<LessonAction> OnActionRequested;

        private void OnEnable()
        {
            foreach (LessonButton button in _buttons)
            {
                button.OnActionRequested += HandleActionRequested;
            }
        }

        private void OnDisable()
        {
            foreach (LessonButton button in _buttons)
            {
                button.OnActionRequested -= HandleActionRequested;
            }
        }

        private void HandleActionRequested(LessonAction action)
        {
            OnActionRequested?.Invoke(action);
        }

        public void UpdateButtons(LessonContent content, int spreadLayer, bool scanning)
        {
            foreach (LessonButton button in _buttons)
            {
                LessonContent.ButtonState state = content.Buttons[button.Action];
                button.gameObject.SetActive(state.Available);
                if (!state.Available) { continue; }
                button.SetAvailable(true);

                string label = state.Label;
                if (button.Action == LessonAction.Scan && scanning)
                {
                    label = state.ScanningLabel;
                }
                else if (button.Action == LessonAction.Spread)
                {
                    label = string.Format(state.Label, spreadLayer + 1);
                }
                if (label != null)
                {
                    button.SetLabel(label);
                }
            }
        }
    }
}
