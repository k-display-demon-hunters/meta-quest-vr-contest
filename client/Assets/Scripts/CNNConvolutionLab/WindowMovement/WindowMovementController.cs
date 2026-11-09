using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class WindowMovementController
    {
        private readonly InputGridController _input;
        private readonly WorkbenchKernelController _kernel;
        private readonly EquationDisplayController _equations;
        private readonly LessonUIController _ui;
        public WindowMovementController(InputGridController input, WorkbenchKernelController kernel,
            EquationDisplayController equations, LessonUIController ui)
        {
            _input = input;
            _kernel = kernel;
            _equations = equations;
            _ui = ui;
        }
        public IEnumerator Run(int row, int column, bool showGuidance)
        {
            _ui.ResetChannelProgress();
            _equations.Clear();
            if (showGuidance)
            {
                _ui.Title = WindowMovementContent.Title();
                _ui.Explanation = WindowMovementContent.Explanation();
            }
            Pose start = _kernel.Pose;
            Pose target = _input.WindowPose(row, column);
            const float duration = 0.45f / 1.5f;
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                _kernel.SetPose(new Pose(Vector3.Lerp(start.position, target.position, Mathf.SmoothStep(0, 1, time / duration)), start.rotation));
                yield return null;
            }
            _kernel.SetPose(target);
        }
    }
}
