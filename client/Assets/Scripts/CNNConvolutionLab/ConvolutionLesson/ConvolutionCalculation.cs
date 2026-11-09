namespace CNNConvolution
{
    public sealed class ConvolutionCalculation
    {
        private readonly int[,] _inputs;
        public int Row { get; }
        public int Column { get; }

        private ConvolutionCalculation(int[,] inputs, int row = 0, int column = 0)
        {
            _inputs = inputs;
            Row = row;
            Column = column;
        }

        public static ConvolutionCalculation CreateExample()
        {
            return new ConvolutionCalculation(new int[,] {
                { 0,1,3,0,2, 1,2,3,1,0, 3,2,1,2,3, 0,2,0,2,1, 1,0,1,3,0 },
                { 1,2,1,3,0, 2,2,0,3,1, 1,0,3,2,2, 3,1,2,1,0, 2,3,0,2,1 },
                { 2,1,0,1,3, 0,3,2,2,1, 1,2,1,0,3, 0,3,1,3,2, 2,0,3,1,0 }
            });
        }

        // Windows share the fixed input, but cannot change one another's position.
        public ConvolutionCalculation ForWindow(int row, int column)
        {
            return new ConvolutionCalculation(_inputs, row, column);
        }

        public int GridInput(int channel, int index) => _inputs[channel, index];
        public int InputIndex(int index) => (Row + index / 3) * 5 + Column + index % 3;
        public int Input(int channel, int index) => GridInput(channel, InputIndex(index));
        public int Weight(int index) => index == 0 || index == 4 || index == 8 ? 1 : 0;
        public int Product(int channel, int index) => Input(channel, index) * Weight(index);
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
                sum += GridInput(channel, (row + index / 3) * 5 + column + index % 3) * Weight(index);
            }
            return sum;
        }

        public int[] Outputs()
        {
            int[] values = new int[9];
            for (int index = 0; index < 9; index++) { values[index] = Output(index / 3, index % 3); }
            return values;
        }
    }
}
