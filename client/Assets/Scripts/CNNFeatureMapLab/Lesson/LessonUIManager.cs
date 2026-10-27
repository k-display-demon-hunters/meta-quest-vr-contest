using UnityEngine;

namespace CNNFeatureMaps.Lesson
{
    public sealed class LessonUIManager : MonoBehaviour
    {
        [SerializeField] private LessonGuide _guide;
        [SerializeField] private LessonButtonGroupController _buttonGroup;
        [SerializeField] private FilterCardController _filterCard;

        public event System.Action OnPreviousRequested;
        public event System.Action OnNextRequested;
        public event System.Action OnScanRequested;
        public event System.Action OnFilterRequested;
        public event System.Action OnSpreadRequested;
        public event System.Action OnRestoreRequested;
        public event System.Action OnEnlargeRequested;
        public event System.Action OnShrinkRequested;

        private void OnEnable()
        {
            _buttonGroup.OnActionRequested += HandleActionRequested;
        }

        private void OnDisable()
        {
            _buttonGroup.OnActionRequested -= HandleActionRequested;
        }

        private void HandleActionRequested(LessonAction action)
        {
            switch (action)
            {
                case LessonAction.Previous:
                    OnPreviousRequested?.Invoke();
                    break;
                case LessonAction.Next:
                    OnNextRequested?.Invoke();
                    break;
                case LessonAction.Scan:
                    OnScanRequested?.Invoke();
                    break;
                case LessonAction.Filter:
                    OnFilterRequested?.Invoke();
                    break;
                case LessonAction.Spread:
                    OnSpreadRequested?.Invoke();
                    break;
                case LessonAction.Restore:
                    OnRestoreRequested?.Invoke();
                    break;
                case LessonAction.Enlarge:
                    OnEnlargeRequested?.Invoke();
                    break;
                case LessonAction.Shrink:
                    OnShrinkRequested?.Invoke();
                    break;
            }
        }

        public void ShowLesson(LessonStep step, int channel, int spreadLayer, bool scanning)
        {
            LessonContent content = GetContent(step);
            _guide.ShowContent(content);
            _filterCard.Show(channel, content.ShowFilterCard);
            _buttonGroup.UpdateButtons(content, spreadLayer, scanning);
        }

        public void UpdateButtons(LessonStep step, int spreadLayer, bool scanning)
        {
            _buttonGroup.UpdateButtons(GetContent(step), spreadLayer, scanning);
        }

        private static LessonContent GetContent(LessonStep step)
        {
            switch (step)
            {
                case LessonStep.BuildFeatureMap:
                    return LessonContent.BuildFeatureMap;
                case LessonStep.CompareFilters:
                    return LessonContent.CompareFilters;
                case LessonStep.TraceReceptiveField:
                    return LessonContent.TraceReceptiveField;
                case LessonStep.EditInput:
                    return LessonContent.EditInput;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(step), step, null);
            }
        }

        public void ShowPatch(int row, int column, float sum, float value, int count)
        {
            _guide.ShowPatch(row, column, sum, value, count);
        }

        public void ShowInputEdited(int editCount)
        {
            _guide.ShowInputEdited(editCount);
        }

        public void ShowAffectedPixel(int row, int column, int affectedCount)
        {
            _guide.ShowAffectedPixel(row, column, affectedCount);
        }

        public void ShowReleaseBeforeAction()
        {
            _guide.ShowReleaseBeforeAction();
        }

        public void ShowStepFinished(bool skipped)
        {
            _guide.ShowStepFinished(skipped);
        }

        public void ShowSelectScaleTarget()
        {
            _guide.ShowSelectScaleTarget();
        }
    }
}
