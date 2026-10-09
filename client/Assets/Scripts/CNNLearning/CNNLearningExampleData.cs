using UnityEngine;

namespace CNNLearning
{
    public sealed class CNNLearningExampleData
    {
        public const int Size = 8;
        public static readonly int[] Channels = { 2, 3, 5 };
        private readonly float[,] _input = new float[Size, Size];
        private readonly float[][,,] _values = new float[3][,,];
        private readonly bool[][,] _affected = new bool[4][,];

        public CNNLearningExampleData(Texture2D image)
        {
            Color[] pixels = image.GetPixels();
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    // Tensor rows run top to bottom; Unity texture rows run bottom to top.
                    _input[row, column] = pixels[(Size - 1 - row) * Size + column].grayscale;
                }
            }
            for (int layer = 0; layer < 4; layer++)
            {
                _affected[layer] = new bool[Size, Size];
            }
            ComputeConvolutions();
        }

        public float ReadValue(int layer, int channel, int row, int column)
        {
            return layer < 0 ? _input[row, column] : _values[layer][channel, row, column];
        }

        public bool IsAffected(int layer, int row, int column) => _affected[layer + 1][row, column];

        public void SelectPixel(int row, int column)
        {
            for (int layer = 0; layer < 4; layer++)
            {
                System.Array.Clear(_affected[layer], 0, _affected[layer].Length);
            }
            if (row < 0 || column < 0)
            {
                return;
            }
            _affected[0][row, column] = true;
            for (int layer = 1; layer < 4; layer++)
            {
                for (int outputRow = 0; outputRow < Size; outputRow++)
                {
                    for (int outputColumn = 0; outputColumn < Size; outputColumn++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int inputRow = outputRow + dy;
                                int inputColumn = outputColumn + dx;
                                if (Inside(inputRow, inputColumn) && _affected[layer - 1][inputRow, inputColumn])
                                {
                                    _affected[layer][outputRow, outputColumn] = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public float ReadOutput(int output)
        {
            float first = 0f;
            float second = 0f;
            for (int channel = 0; channel < Channels[2]; channel++)
            {
                for (int row = 0; row < Size; row++)
                {
                    for (int column = 0; column < Size; column++)
                    {
                        float value = _values[2][channel, row, column] / (Size * Size);
                        first += value * (channel % 2 == 0 ? 1f : -.5f);
                        second += value * (channel % 2 == 0 ? -.5f : 1f);
                    }
                }
            }
            float probability = 1f / (1f + Mathf.Exp(second - first));
            return output == 0 ? probability : 1f - probability;
        }

        public float ReadKernelWeight(int channel, int dy, int dx)
        {
            return (channel % 3 == 0 ? dx : channel % 3 == 1 ? dy : 1f / 3f) / 3f;
        }

        public float ReadFirstLayerSum(int channel, int row, int column)
        {
            float sum = 0f;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (Inside(row + dy, column + dx))
                    {
                        sum += _input[row + dy, column + dx] * ReadKernelWeight(channel, dy, dx);
                    }
                }
            }
            return sum;
        }

        public void SelectWindow(int row, int column)
        {
            SelectPixel(-1, -1);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (Inside(row + dy, column + dx))
                    {
                        _affected[0][row + dy, column + dx] = true;
                    }
                }
            }
        }

        public void ToggleInput(int row, int column)
        {
            _input[row, column] = _input[row, column] >= .5f ? 0f : 1f;
            ComputeConvolutions();
        }

        public void RestoreInput(Texture2D image)
        {
            Color[] pixels = image.GetPixels();
            for (int row = 0; row < Size; row++)
            {
                for (int column = 0; column < Size; column++)
                {
                    _input[row, column] = pixels[(Size - 1 - row) * Size + column].grayscale;
                }
            }
            SelectPixel(-1, -1);
            ComputeConvolutions();
        }

        private void ComputeConvolutions()
        {
            for (int layer = 0; layer < 3; layer++)
            {
                int inputChannels = layer == 0 ? 1 : Channels[layer - 1];
                _values[layer] = new float[Channels[layer], Size, Size];
                for (int channel = 0; channel < Channels[layer]; channel++)
                {
                    for (int row = 0; row < Size; row++)
                    {
                        for (int column = 0; column < Size; column++)
                        {
                            float sum = 0f;
                            for (int inputChannel = 0; inputChannel < inputChannels; inputChannel++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        int inputRow = row + dy;
                                        int inputColumn = column + dx;
                                        if (!Inside(inputRow, inputColumn))
                                        {
                                            continue;
                                        }
                                        float value = layer == 0 ? _input[inputRow, inputColumn] :
                                            _values[layer - 1][inputChannel, inputRow, inputColumn];
                                        // Fixed educational filters; every output sees every preceding channel.
                                        float weight = ReadKernelWeight(channel, dy, dx);
                                        sum += value * weight / inputChannels;
                                    }
                                }
                            }
                            _values[layer][channel, row, column] = Mathf.Max(0f, sum);
                        }
                    }
                }
            }
        }

        private static bool Inside(int row, int column) => row >= 0 && row < Size && column >= 0 && column < Size;
    }
}
