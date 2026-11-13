using System.Collections;

namespace CNNConvolution
{
    internal sealed class OutputRecordingController
    {
        private readonly EquationDisplayController _equations;
        private readonly OutputRecordingAnimationController _animation;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        public OutputRecordingController(WorkbenchPrimitiveFactory factory, WorkbenchKernelController kernel, ResultGridController result,
            EquationDisplayController equations, FlyingNumberController number, LessonUIController ui,
            LessonNarrationController narration, LessonPlayback playback)
        {
            _equations = equations;
            _ui = ui;
            _narration = narration;
            _playback = playback;
            _animation = new OutputRecordingAnimationController(factory, kernel, result, equations, number, playback);
        }
        public void Cancel() => _animation.Cancel();

        // The first output is narrated: the three sums stay gathered until the spoken addition ends.
        public IEnumerator Run(ConvolutionCalculation calculation, bool narrated)
        {
            int output = calculation.Output(calculation.Row, calculation.Column);
            _ui.Caption = narrated
                ? OutputRecordingContent.RgbSum()
                : OutputRecordingContent.Record(calculation.Row, calculation.Column);
            _equations.ShowRgbSum(output, OutputRecordingContent.Equation(calculation));
            if (narrated) { _narration.Play(NarrationCue.RgbSum); }
            yield return _playback.Wait(0.7f);
            yield return _animation.Gather(new[] { calculation.Sum(0), calculation.Sum(1), calculation.Sum(2) });
            if (narrated)
            {
                yield return _narration.WaitUntilDone();
                _ui.Caption = OutputRecordingContent.Record(calculation.Row, calculation.Column);
                _narration.Play(NarrationCue.FirstOutput);
            }
            yield return _animation.Record(calculation.Row * 3 + calculation.Column, output);
            yield return _playback.Wait(0.35f);
            if (narrated) { yield return _narration.WaitUntilDone(); }
        }
    }
}
