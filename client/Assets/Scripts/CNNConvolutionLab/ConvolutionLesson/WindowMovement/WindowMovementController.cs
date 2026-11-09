using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class WindowMovementController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        public WindowMovementController(RgbVolumeController spatial, LessonUIController ui)
        { _spatial = spatial; _ui = ui; }
        public IEnumerator Run(int row, int column, bool showGuidance)
        {
            _ui.ResetChannelProgress();
            _spatial.ClearEquations();
            if (showGuidance)
            {
                _ui.Title = WindowMovementContent.Title();
                _ui.Explanation = WindowMovementContent.Explanation();
            }
            _spatial.EndPositionQuiz();
            Pose start = _spatial.KernelPose;
            Pose target = _spatial.WindowPose(row, column);
            const float duration = 0.45f / 1.5f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _spatial.SetKernelPose(new Pose(Vector3.Lerp(start.position, target.position, Mathf.SmoothStep(0, 1, time / duration)), start.rotation));
                yield return null;
            }
            _spatial.SetKernelPose(target);
        }
    }
}
