using Core;
using System.Text;
using UnityEngine;

namespace CNNConvolution
{
    public sealed class ConvolutionCalculation
    {
        private readonly int[,] _inputs;
        public int Row { get; private set; }
        public int Column { get; private set; }

        public ConvolutionCalculation(ConvolutionInputData input)
        {
            _inputs = new int[3, 25];
            for (int index = 0; index < 25; index++)
            {
                Color32 pixel = input[index];
                _inputs[0, index] = Mathf.RoundToInt(pixel.r * 3f / 255f);
                _inputs[1, index] = Mathf.RoundToInt(pixel.g * 3f / 255f);
                _inputs[2, index] = Mathf.RoundToInt(pixel.b * 3f / 255f);
            }
        }

        private ConvolutionCalculation(int[,] inputs) { _inputs = inputs; }

        public static ConvolutionCalculation CreateExample()
        {
            return new ConvolutionCalculation(new int[,] {
                { 0,1,3,0,2, 1,2,3,1,0, 3,2,1,2,3, 0,2,0,2,1, 1,0,1,3,0 },
                { 1,2,1,3,0, 2,2,0,3,1, 1,0,3,2,2, 3,1,2,1,0, 2,3,0,2,1 },
                { 2,1,0,1,3, 0,3,2,2,1, 1,2,1,0,3, 0,3,1,3,2, 2,0,3,1,0 }
            });
        }

        public void SetWindow(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public int GridInput(int channel, int index) => _inputs[channel, index];
        public int InputIndex(int index) => (Row + index / 3) * 5 + Column + index % 3;
        public int Input(int channel, int index) => GridInput(channel, InputIndex(index));
        public int Weight(int channel, int index) => index == 0 || index == 4 || index == 8 ? 1 : 0;
        public int Product(int channel, int index) => Input(channel, index) * Weight(channel, index);
        public int Sum(int channel)
        {
            int sum = 0;
            for (int i = 0; i < 9; i++) { sum += Product(channel, i); }
            return sum;
        }

        public int Output(int row, int column)
        {
            int sum = 0;
            for (int channel = 0; channel < 3; channel++)
            {
                sum += Sum(channel, row, column);
            }
            return sum;
        }

        public int Sum(int channel, int row, int column)
        {
            int sum = 0;
            for (int index = 0; index < 9; index++)
            {
                sum += GridInput(channel, (row + index / 3) * 5 + column + index % 3) * Weight(channel, index);
            }
            return sum;
        }

        public int[] Outputs()
        {
            int[] values = new int[9];
            for (int index = 0; index < 9; index++) { values[index] = Output(index / 3, index % 3); }
            return values;
        }

        public string ChannelEquation(int channel, bool productsOnly = false)
        {
            StringBuilder expression = new StringBuilder();
            expression.Append("RGB"[channel]).Append(' ');
            for (int index = 0; index < 9; index += 4)
            {
                if (index > 0) { expression.Append(" + "); }
                expression.Append(Input(channel, index));
                if (!productsOnly) { expression.Append("×1"); }
            }
            return expression.Append(" = ").Append(Sum(channel)).ToString();
        }
        public string Summary(int completed) =>
            "R 합: " + (completed > 0 ? Sum(0).ToString() : "?") +
            "     G 합: " + (completed > 1 ? Sum(1).ToString() : "?") +
            "     B 합: " + (completed > 2 ? Sum(2).ToString() : "?");
        public int[] ChannelSums() => new[] { Sum(0), Sum(1), Sum(2) };
    }
}
