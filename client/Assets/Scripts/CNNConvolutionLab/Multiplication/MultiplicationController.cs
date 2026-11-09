using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class MultiplicationController
    {
        private readonly InputGridController _input;
        private readonly MultiplicationDisplayController _display;
        private readonly MultiplicationAnimationController _animation;
        private readonly LessonUIController _ui;
        public MultiplicationController(WorkbenchPrimitiveFactory factory, InputGridController input, WorkbenchKernelController kernel,
            ResultGridController result, EquationDisplayController equations, FlyingNumberController number, LessonUIController ui)
        {
            _input = input;
            _ui = ui;
            _display = new MultiplicationDisplayController(factory, input, kernel, result, equations, number);
            _animation = new MultiplicationAnimationController(_display);
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
                _ui.Guide = MultiplicationContent.Guide();
                _ui.Feedback = MultiplicationContent.WeightFeedback(weight);
                yield return _animation.MultiplyAndRecord(channel, index, calculation.InputIndex(index), calculation.Input(channel, index), weight, product, 0.75f);
                _ui.Feedback = MultiplicationContent.RecordedFeedback(product);
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }
    }
}
