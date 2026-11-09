using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class ChannelSumController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        public ChannelSumController(RgbVolumeController spatial, LessonUIController ui)
        { _spatial = spatial; _ui = ui; }
        public IEnumerator RunDetailed(ConvolutionCalculation calculation, int channel)
        {
            _ui.Title = ChannelSumContent.DetailedTitle(channel);
            _ui.Explanation = ChannelSumContent.DetailedExplanation();
            _ui.Guide = ChannelSumContent.DetailedGuide(channel);
            _ui.Feedback = ChannelSumContent.EmptyFeedback();
            _spatial.ClearEquations();
            int[] products = new int[9];
            for (int i = 0; i < 9; i++) { products[i] = calculation.Product(channel, i); }
            yield return new ChannelSumAnimationController(_spatial).Run(channel, products, calculation.Sum(channel));
            _spatial.ShowChannelSum(calculation.Sum(channel), ChannelSumContent.Equation(calculation, channel, true));
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
            _spatial.ShowSlidingChannel(channel, showProducts);
            yield return new WaitForSecondsRealtime(0.25f);
            if (showProducts)
            {
                int[] products = new int[9];
                for (int i = 0; i < 9; i++) { products[i] = calculation.Product(channel, i); }
                _spatial.ShowProducts(products);
            }
            _spatial.ShowChannelSum(calculation.Sum(channel), ChannelSumContent.Equation(calculation, channel, false));
            _ui.CompleteChannel(channel, calculation.Sum(channel));
            yield return new WaitForSecondsRealtime(0.8f);
        }
    }
}
