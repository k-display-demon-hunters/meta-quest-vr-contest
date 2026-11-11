using System.Collections;

namespace CNNConvolution
{
    internal sealed class MultiplicationController
    {
        private readonly InputGridController _input;
        private readonly ResultGridController _products;
        private readonly WorkbenchKernelController _kernel;
        private readonly MultiplicationDisplayController _display;
        private readonly MultiplicationAnimationController _animation;
        private readonly LessonUIController _ui;
        private readonly LessonPlayback _playback;
        public MultiplicationController(WorkbenchPrimitiveFactory factory, InputGridController input, WorkbenchKernelController kernel,
            ResultGridController result, EquationDisplayController equations, FlyingNumberController number,
            LessonUIController ui, LessonPlayback playback)
        {
            _input = input;
            _products = result;
            _kernel = kernel;
            _ui = ui;
            _playback = playback;
            _display = new MultiplicationDisplayController(factory, input, kernel, result, equations, number);
            _animation = new MultiplicationAnimationController(_display, playback);
        }

        public void Cancel() => _display.Clear();
        public IEnumerator Run(ConvolutionCalculation calculation, int channel)
        {
            _products.Clear();
            _ui.ResetChannelProgress();
            _ui.BeginChannel(channel);
            _input.ShowChannel(channel);
            _kernel.ShowChannel(channel);
            _ui.SetStage(1);
            // Include both weights and a nonzero product before presenting the full grid.
            int[] examples = { 0, 1, 4 };
            foreach (int index in examples)
            {
                int weight = calculation.Weight(index);
                int product = calculation.Product(channel, index);
                _ui.Title = MultiplicationContent.CellTitle(channel, index);
                _ui.Explanation = MultiplicationContent.Explanation();
                yield return _animation.MultiplyAndRecord(channel, index, calculation.InputIndex(index), calculation.Input(channel, index), weight, product, 0.75f);
                yield return _playback.Wait(0.15f);
            }
            for (int index = 0; index < 9; index++) { _products.Record(index, calculation.Product(channel, index)); }
            _ui.Title = "같은 위치끼리 곱하면 아홉 개의 곱이 생겨요";
            _ui.Explanation = "가중치 0인 칸도 곱해요. 지금은 곱한 결과이며, 아직 출력 한 칸이 아니에요.";
        }
    }
}
