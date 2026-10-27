using UnityEngine;

namespace CNNFeatureMaps.Lesson
{
    public sealed class LessonGuide : MonoBehaviour
    {
        [SerializeField] private TextMesh _title;
        [SerializeField] private TextMesh _instruction;
        [SerializeField] private TextMesh _readout;

        public void ShowContent(LessonContent content)
        {
            // Filter steps supply their readout through ShowPatch after the base UI is shown.
            if (content.Readout != null)
            {
                _readout.text = content.Readout;
            }
            _title.text = content.Title;
            _instruction.text = content.Instruction;
        }

        public void ShowPatch(int row, int column, float sum, float value, int count)
        {
            _readout.text = string.Format(LessonContent.PatchFormat, row + 1, column + 1, sum, value, count);
        }

        public void ShowInputEdited(int editCount)
        {
            _readout.text = string.Format(LessonContent.InputEditedFormat, editCount);
        }

        public void ShowAffectedPixel(int row, int column, int affectedCount)
        {
            _readout.text = string.Format(LessonContent.AffectedPixelFormat, row + 1, column + 1, affectedCount);
        }

        public void ShowReleaseBeforeAction()
        {
            _readout.text = LessonContent.ReleaseBeforeActionText;
        }

        public void ShowStepFinished(bool skipped)
        {
            string message = skipped ? LessonContent.StepSkippedText : LessonContent.StepFinishedText;
            _readout.text = message + LessonContent.MoveToModelDesignText;
        }

        public void ShowSelectScaleTarget()
        {
            _readout.text = LessonContent.SelectScaleTargetText;
        }
    }
}
