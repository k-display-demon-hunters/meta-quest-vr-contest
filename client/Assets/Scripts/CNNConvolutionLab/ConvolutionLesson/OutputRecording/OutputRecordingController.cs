using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class OutputRecordingController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        public OutputRecordingController(RgbVolumeController spatial, LessonUIController ui)
        { _spatial = spatial; _ui = ui; }
        public IEnumerator Run(ConvolutionCalculation calculation, bool firstOutput)
        {
            if (firstOutput)
            {
                _ui.Title = OutputRecordingContent.FirstTitle();
                _ui.Explanation = OutputRecordingContent.FirstExplanation();
                _spatial.BeginOutputMap();
            }
            _ui.Title = OutputRecordingContent.RecordTitle(calculation.Row, calculation.Column);
            _ui.Explanation = OutputRecordingContent.RecordExplanation();
            _spatial.ShowRgbSum(calculation.Output(calculation.Row, calculation.Column), OutputRecordingContent.Equation(calculation));
            yield return new WaitForSecondsRealtime(0.7f);
            yield return new OutputRecordingAnimationController(_spatial).Run(calculation.Row * 3 + calculation.Column, calculation.Output(calculation.Row, calculation.Column));
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }
}
