using System.Collections;

namespace CNNConvolution
{
    internal sealed class MultiplicationController
    {
        // The two spoken cells read the first and the centre product of the fixed example aloud.
        private const int FirstCell = 0;
        private const int CenterCell = 4;

        private readonly InputGridController _input;
        private readonly ResultGridController _products;
        private readonly WorkbenchKernelController _kernel;
        private readonly MultiplicationDisplayController _display;
        private readonly MultiplicationAnimationController _animation;
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly LessonPlayback _playback;
        public MultiplicationController(WorkbenchPrimitiveFactory factory, InputGridController input, WorkbenchKernelController kernel,
            ResultGridController result, EquationDisplayController equations, FlyingNumberController number,
            LessonUIController ui, LessonNarrationController narration, LessonPlayback playback)
        {
            _input = input;
            _products = result;
            _kernel = kernel;
            _ui = ui;
            _narration = narration;
            _playback = playback;
            _display = new MultiplicationDisplayController(factory, input, kernel, result, equations, number);
            _animation = new MultiplicationAnimationController(_display, playback);
        }

        public void Cancel() => _display.Clear();
        public IEnumerator Run(ConvolutionCalculation calculation, int channel)
        {
            _products.Clear();
            _input.ShowChannel(channel);
            _kernel.ShowChannel(channel);
            _ui.Caption = MultiplicationContent.Intro();
            _narration.Play(NarrationCue.MultiplyIntro);
            yield return _narration.WaitUntilDone();
            for (int index = 0; index < 9; index++)
            {
                int weight = calculation.Weight(index);
                int product = calculation.Product(channel, index);
                _ui.Caption = MultiplicationContent.Cell(channel, index);
                if (index == FirstCell) { _narration.Play(NarrationCue.MultiplyFirstCell); }
                else if (index == CenterCell) { _narration.Play(NarrationCue.MultiplyCenterCell); }
                yield return _animation.MultiplyAndRecord(channel, index, calculation.InputIndex(index), calculation.Input(channel, index), weight, product, 0.75f);
                yield return _narration.WaitUntilDone();
                yield return _playback.Wait(0.15f);
            }
            _ui.Caption = MultiplicationContent.Done();
            _narration.Play(NarrationCue.MultiplyDone);
            yield return _narration.WaitUntilDone();
        }
    }
}
