using System;
using System.Collections;
using UnityEngine;

namespace UI
{
    public sealed class ConvolutionCalculation
    {
        private readonly int[,] _inputs = new int[3, 9];
        public ConvolutionCalculation(PixelPatchData patch)
        {
            for (int c = 0; c < 3; c++)
            for (int i = 0; i < 9; i++)
            {
                Color32 pixel = patch[i];
                int value = c == 0 ? pixel.r : c == 1 ? pixel.g : pixel.b;
                // Observation retains 0..255; arithmetic explicitly uses a 0..3 practice scale.
                _inputs[c, i] = Mathf.RoundToInt(value * 3f / 255f);
            }
        }
        public int Input(int channel, int index) => _inputs[channel, index];
        public int Weight(int channel, int index) => index == 0 || index == 4 || index == 8 ? 1 : 0;
        public int Product(int channel, int index) => Input(channel, index) * Weight(channel, index);
        public int Sum(int channel)
        {
            int sum = 0;
            for (int i = 0; i < 9; i++) { sum += Product(channel, i); }
            return sum;
        }
        public string Summary(int completed) =>
            "R 합: " + (completed > 0 ? Sum(0).ToString() : "?") +
            "     G 합: " + (completed > 1 ? Sum(1).ToString() : "?") +
            "     B 합: " + (completed > 2 ? Sum(2).ToString() : "?");
        public int[] ChannelSums() => new[] { Sum(0), Sum(1), Sum(2) };
    }
}
