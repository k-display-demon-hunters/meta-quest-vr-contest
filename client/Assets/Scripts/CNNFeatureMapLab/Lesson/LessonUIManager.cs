using UnityEngine;

namespace CNNFeatureMaps.Lesson
{
    public sealed class LessonUIManager : MonoBehaviour
    {
        [SerializeField] private LessonGuide _guide;
        [SerializeField] private LessonButtonGroupController _buttonGroup;
        [SerializeField] private FilterCardController _filterCard;

        public event System.Action<LessonAction> OnActionRequested;

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
            OnActionRequested?.Invoke(action);
        }

        public void ShowLesson(LessonStep step, int channel, int spreadLayer, bool scanning)
        {
            _guide.ShowStep(step);
            bool isFilterStep = step == LessonStep.BuildFeatureMap || step == LessonStep.CompareFilters;
            _filterCard.Show(channel, isFilterStep);
            UpdateButtons(step, spreadLayer, scanning);
        }

        public void UpdateButtons(LessonStep step, int spreadLayer, bool scanning)
        {
            _buttonGroup.UpdateButtons(step, spreadLayer, scanning);
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
