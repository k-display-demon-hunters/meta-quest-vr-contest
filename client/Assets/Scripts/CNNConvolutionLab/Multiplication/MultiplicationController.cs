using System.Collections;

namespace CNNConvolution
{
    internal sealed class MultiplicationController
    {
        private readonly InputGridController _input;
        private readonly MultiplicationDisplayController _display;
        private readonly MultiplicationAnimationController _animation;
        private readonly LessonUIController _ui;
        private readonly LessonPlayback _playback;
        public MultiplicationController(WorkbenchPrimitiveFactory factory, InputGridController input, WorkbenchKernelController kernel,
            ResultGridController result, EquationDisplayController equations, FlyingNumberController number,
            LessonUIController ui, LessonPlayback playback)
        {
            _input = input;
            _ui = ui;
            _playback = playback;
            _display = new MultiplicationDisplayController(factory, input, kernel, result, equations, number);
            _animation = new MultiplicationAnimationController(_display, playback);
        }

        public void Cancel() => _display.Clear();
        public IEnumerator Run(ConvolutionCalculation calculation, int channel)
        {
            _ui.BeginChannel(channel);
            _input.ShowChannel(channel);
            for (int index = 0; index < 9; index++)
            {
                int weight = calculation.Weight(index);
                int product = calculation.Product(channel, index);
                _ui.Title = MultiplicationContent.CellTitle(channel, index);
                _ui.Explanation = MultiplicationContent.Explanation();
                yield return _animation.MultiplyAndRecord(channel, index, calculation.InputIndex(index), calculation.Input(channel, index), weight, product, 0.75f);
                yield return _playback.Wait(0.15f);
            }
        }
    }
}
