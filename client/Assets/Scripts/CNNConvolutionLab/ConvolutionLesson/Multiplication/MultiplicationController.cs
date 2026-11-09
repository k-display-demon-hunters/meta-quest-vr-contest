using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class MultiplicationController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        public MultiplicationController(RgbVolumeController spatial, LessonUIController ui)
        { _spatial = spatial; _ui = ui; }
        public IEnumerator Run(ConvolutionCalculation calculation, int channel)
        {
            _ui.BeginChannel(channel);
            _spatial.ShowSlidingChannel(channel, true);
            MultiplicationAnimationController animation = new MultiplicationAnimationController(_spatial);
            for (int index = 0; index < 9; index++)
            {
                int weight = calculation.Weight(index);
                int product = calculation.Product(channel, index);
                _ui.Title = MultiplicationContent.CellTitle(channel, index);
                _ui.Explanation = MultiplicationContent.Explanation();
                _ui.Guide = MultiplicationContent.Guide();
                _ui.Feedback = MultiplicationContent.WeightFeedback(weight);
                yield return animation.MultiplyAndRecord(channel, index, calculation.InputIndex(index), calculation.Input(channel, index), weight, product, 0.75f);
                _ui.Feedback = MultiplicationContent.RecordedFeedback(product);
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }
    }
}
