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

        public void UpdateButtons(LessonStep step, int spreadLayer, bool scanning)
        {
            bool isFirstStep = step == LessonStep.BuildFeatureMap;
            bool isFilterStep = isFirstStep || step == LessonStep.CompareFilters;
            foreach (LessonButton button in _buttons)
            {
                switch (button.Action)
                {
                    case LessonAction.Previous:
                        button.SetAvailable(!isFirstStep);
                        break;
                    case LessonAction.Next:
                        button.SetAvailable(true);
                        button.SetLabel(step == LessonStep.EditInput ? LessonContent.FinishButtonText : LessonContent.NextButtonText);
                        break;
                    case LessonAction.Scan:
                        button.SetAvailable(isFilterStep);
                        button.SetLabel(scanning ? LessonContent.StopButtonText : LessonContent.ScanButtonText);
                        break;
                    case LessonAction.Filter:
                        button.SetAvailable(isFilterStep);
                        break;
                    case LessonAction.Spread:
                        button.SetAvailable(!isFirstStep);
                        button.SetLabel(string.Format(LessonContent.SpreadButtonFormat, spreadLayer + 1));
                        break;
                    case LessonAction.Restore:
                    case LessonAction.Enlarge:
                    case LessonAction.Shrink:
                        button.SetAvailable(true);
                        break;
                }
            }
        }
    }
}
