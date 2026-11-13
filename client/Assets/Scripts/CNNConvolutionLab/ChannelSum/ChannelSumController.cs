using System.Collections;

namespace CNNConvolution
{
    internal sealed class ChannelSumController
    {
        private readonly InputGridController _input;
        private readonly WorkbenchKernelController _kernel;
        private readonly ResultGridController _result;
        private readonly EquationDisplayController _equations;
        private readonly ChannelSumDisplayController _display;
        private readonly ChannelSumAnimationController _animation;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        public ChannelSumController(WorkbenchPrimitiveFactory factory, InputGridController input, WorkbenchKernelController kernel, ResultGridController result,
            EquationDisplayController equations, FlyingNumberController number, LessonUIController ui,
            LessonNarrationController narration, LessonPlayback playback)
        {
            _input = input;
            _kernel = kernel;
            _result = result;
            _equations = equations;
            _ui = ui;
            _narration = narration;
            _playback = playback;
            _display = new ChannelSumDisplayController(factory, number, result, equations);
            _animation = new ChannelSumAnimationController(_display, playback);
        }

        public void Cancel() => _display.Clear();
        public IEnumerator RunDetailed(ConvolutionCalculation calculation, int channel)
        {
            _ui.Caption = ChannelSumContent.Detailed(channel);
            _narration.Play(NarrationCue.ChannelSum);
            _equations.Clear();
            _result.HideHighlight();
            int[] products = new int[9];
            for (int i = 0; i < 9; i++) { products[i] = calculation.Product(channel, i); }
            yield return _animation.Run(channel, products, calculation.Sum(channel));
            _equations.ShowChannelSum(calculation.Sum(channel), ChannelSumContent.Equation(calculation, channel, true));
            yield return _playback.Wait(0.9f);
            yield return _narration.WaitUntilDone();
        }

        // The caller owns the caption and narration; this only shows one channel's products and sum.
        public IEnumerator RunCompact(ConvolutionCalculation calculation, int channel)
        {
            _equations.Clear();
            _input.ShowChannel(channel);
            _kernel.ShowChannel(channel);
            _result.Clear();
            yield return _playback.Wait(0.25f);
            for (int i = 0; i < 9; i++) { _result.Record(i, calculation.Product(channel, i)); }
            _equations.ShowChannelSum(calculation.Sum(channel), ChannelSumContent.Equation(calculation, channel, false));
            yield return _playback.Wait(0.8f);
        }
    }
}
