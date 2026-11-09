using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ChannelSumController
    {
        private readonly InputGridController _input;
        private readonly ResultGridController _result;
        private readonly EquationDisplayController _equations;
        private readonly ChannelSumDisplayController _display;
        private readonly ChannelSumAnimationController _animation;
        private readonly LessonUIController _ui;
        public ChannelSumController(WorkbenchPrimitiveFactory factory, InputGridController input, ResultGridController result,
            EquationDisplayController equations, FlyingNumberController number, LessonUIController ui)
        {
            _input = input;
            _result = result;
            _equations = equations;
            _ui = ui;
            _display = new ChannelSumDisplayController(factory, number, result, equations);
            _animation = new ChannelSumAnimationController(_display);
        }

        public void Cancel() => _display.Clear();
        public IEnumerator RunDetailed(ConvolutionCalculation calculation, int channel)
        {
            _ui.Title = ChannelSumContent.DetailedTitle(channel);
            _ui.Explanation = ChannelSumContent.DetailedExplanation();
            _ui.Guide = ChannelSumContent.DetailedGuide(channel);
            _ui.Feedback = ChannelSumContent.EmptyFeedback();
            _equations.Clear();
            _result.HideHighlight();
            int[] products = new int[9];
            for (int i = 0; i < 9; i++) { products[i] = calculation.Product(channel, i); }
            yield return _animation.Run(channel, products, calculation.Sum(channel));
            _equations.ShowChannelSum(calculation.Sum(channel), ChannelSumContent.Equation(calculation, channel, true));
            _ui.CompleteChannel(channel, calculation.Sum(channel));
            yield return new WaitForSecondsRealtime(0.9f);
        }

        public IEnumerator RunCompact(ConvolutionCalculation calculation, int channel, bool showProducts)
        {
            _ui.BeginChannel(channel);
            _ui.Title = ChannelSumContent.CompactTitle(channel, calculation.Row, calculation.Column);
            _ui.Explanation = ChannelSumContent.CompactExplanation();
            _ui.Guide = ChannelSumContent.CompactGuide();
            _ui.Feedback = ChannelSumContent.CompactFeedback();
            _equations.Clear();
            _input.ShowChannel(channel);
            if (showProducts) { _result.BeginProducts(); }
            yield return new WaitForSecondsRealtime(0.25f);
            if (showProducts)
            {
                for (int i = 0; i < 9; i++) { _result.Record(i, calculation.Product(channel, i)); }
            }
            _equations.ShowChannelSum(calculation.Sum(channel), ChannelSumContent.Equation(calculation, channel, false));
            _ui.CompleteChannel(channel, calculation.Sum(channel));
            yield return new WaitForSecondsRealtime(0.8f);
        }
    }
}
