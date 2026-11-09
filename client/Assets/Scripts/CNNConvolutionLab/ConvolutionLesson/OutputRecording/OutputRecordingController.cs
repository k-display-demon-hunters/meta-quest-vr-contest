using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class OutputRecordingController
    {
        private readonly ResultGridController _result;
        private readonly EquationDisplayController _equations;
        private readonly OutputRecordingAnimationController _animation;
        private readonly LessonUIController _ui;
        public OutputRecordingController(WorkbenchKernelController kernel, ResultGridController result,
            EquationDisplayController equations, FlyingNumberController number, LessonUIController ui)
        {
            _result = result;
            _equations = equations;
            _ui = ui;
            _animation = new OutputRecordingAnimationController(kernel, result, number);
        }
        public IEnumerator Run(ConvolutionCalculation calculation, bool firstOutput)
        {
            if (firstOutput)
            {
                _ui.Title = OutputRecordingContent.FirstTitle();
                _ui.Explanation = OutputRecordingContent.FirstExplanation();
                _result.BeginOutputs();
            }
            _ui.Title = OutputRecordingContent.RecordTitle(calculation.Row, calculation.Column);
            _ui.Explanation = OutputRecordingContent.RecordExplanation();
            _equations.ShowRgbSum(calculation.Output(calculation.Row, calculation.Column), OutputRecordingContent.Equation(calculation));
            yield return new WaitForSecondsRealtime(0.7f);
            yield return _animation.Run(calculation.Row * 3 + calculation.Column, calculation.Output(calculation.Row, calculation.Column));
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }
}
