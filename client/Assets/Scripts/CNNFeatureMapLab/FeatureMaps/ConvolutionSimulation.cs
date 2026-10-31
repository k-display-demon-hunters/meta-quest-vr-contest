using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class ConvolutionSimulation
    {
        public const int Size = 8;
        public static readonly int[] Channels = { 2, 3, 5 };
        private readonly Color[,] _input = new Color[Size, Size];
        private readonly float[][,,] _values = new float[3][,,];
        private readonly float[][] _maxima = new float[3][];
        private readonly bool[][,,] _affected = new bool[4][,,];

        public ConvolutionSimulation(Color[,] input)
        {
            _affected[0] = new bool[1, Size, Size];
            for (int layer = 0; layer < 3; layer++)
            {
                _affected[layer + 1] = new bool[Channels[layer], Size, Size];
            }
            RestoreInput(input);
        }

        public Color ReadInputColor(int row, int column) => _input[row, column];
        public float ReadValue(int layer, int channel, int row, int column) =>
            layer < 0 ? _input[row, column].grayscale : _values[layer][channel, row, column];
        public float ReadScale(int layer, int channel) => Mathf.Max(0.001f, _maxima[layer][channel]);
        public int InputChannels(int layer) => layer == 0 ? 3 : Channels[layer - 1];
        public bool IsAffected(int layer, int channel, int row, int column) =>
            _affected[layer + 1][layer < 0 ? 0 : channel, row, column];

        public float ReadWeight(int layer, int channel, int inputChannel, int dy, int dx)
        {
            if (layer == 0)
            {
                if (channel == 0) { return (inputChannel == 0 ? 1f : -0.5f) / 9f; }
                float luminance = inputChannel == 0 ? 0.2126f : inputChannel == 1 ? 0.7152f : 0.0722f;
                return (dx < 0 ? 0f : dx == 0 ? -1f : 1f) * luminance / 3f;
            }
            float spatial;
            switch (channel % 5)
            {
                case 0: spatial = 1f / 9f; break;
                case 1: spatial = dx / 3f; break;
                case 2: spatial = dy / 3f; break;
                case 3: spatial = dx == 0 && dy == 0 ? 1f : 0f; break;
                default: spatial = dx == 0 && dy == 0 ? 1f : Mathf.Abs(dx) + Mathf.Abs(dy) == 1 ? -0.25f : 0f; break;
            }
            // Fixed illustrative kernels. Every map combines the preceding channels.
            float mix = (channel + inputChannel) % 3 == 0 ? 1f : 0.5f;
            return spatial * mix / InputChannels(layer);
        }

        public float ReadContribution(int layer, int channel, int inputChannel, int row, int column)
        {
            float sum = 0f;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int y = row + dy;
                    int x = column + dx;
                    if (!Inside(y, x)) { continue; }
                    float value = layer == 0 ? _input[y, x][inputChannel] : _values[layer - 1][inputChannel, y, x];
                    sum += value * ReadWeight(layer, channel, inputChannel, dy, dx);
                }
            }
            return sum;
        }

        public float ReadFirstLayerSum(int channel, int row, int column) => _values[0][channel, row, column];
        public float ReadKernelWeight(int channel, int dy, int dx) => ReadWeight(0, channel, 0, dy, dx);

        public void SelectPixel(int row, int column)
        {
            ClearSelection();
            if (!Inside(row, column)) { return; }
            _affected[0][0, row, column] = true;
            for (int layer = 0; layer < 3; layer++)
            {
                for (int channel = 0; channel < Channels[layer]; channel++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        for (int x = 0; x < Size; x++)
                        {
                            for (int source = 0; source < InputChannels(layer); source++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        if (Inside(y + dy, x + dx) && ReadWeight(layer, channel, source, dy, dx) != 0f
                                            && _affected[layer][layer == 0 ? 0 : source, y + dy, x + dx])
                                        {
                                            _affected[layer + 1][channel, y, x] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SelectFeatureCell(int layer, int channel, int row, int column)
        {
            ClearSelection();
            _affected[layer + 1][channel, row, column] = true;
            for (int current = layer; current >= 0; current--)
            {
                for (int output = 0; output < Channels[current]; output++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        for (int x = 0; x < Size; x++)
                        {
                            if (!_affected[current + 1][output, y, x]) { continue; }
                            for (int source = 0; source < InputChannels(current); source++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    for (int dx = -1; dx <= 1; dx++)
                                    {
                                        if (Inside(y + dy, x + dx) && ReadWeight(current, output, source, dy, dx) != 0f)
                                        {
                                            _affected[current][current == 0 ? 0 : source, y + dy, x + dx] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public int CountAffectedPositions(int layer)
        {
            int count = 0;
            foreach (bool affected in _affected[layer + 1]) { if (affected) { count++; } }
            return count;
        }

        private void ClearSelection()
        {
            foreach (bool[,,] layer in _affected) { System.Array.Clear(layer, 0, layer.Length); }
        }

        public void SelectWindow(int row, int column)
        {
            ClearSelection();
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (Inside(row + dy, column + dx)) { _affected[0][0, row + dy, column + dx] = true; }
                }
            }
        }

        public void ToggleInput(int row, int column)
        {
            Color color = _input[row, column];
            _input[row, column] = new Color(color.g, color.b, color.r);
            ComputeConvolutions();
        }

        public void RestoreInput(Color[,] input)
        {
            if (input == null || input.GetLength(0) != Size || input.GetLength(1) != Size)
            {
                throw new System.ArgumentException($"Input must be a {Size} x {Size} RGB array.", nameof(input));
            }
            System.Array.Copy(input, _input, input.Length);
            ClearSelection();
            ComputeConvolutions();
        }

        private void ComputeConvolutions()
        {
            for (int layer = 0; layer < 3; layer++)
            {
                _values[layer] = new float[Channels[layer], Size, Size];
                _maxima[layer] = new float[Channels[layer]];
                for (int channel = 0; channel < Channels[layer]; channel++)
                {
                    for (int row = 0; row < Size; row++)
                    {
                        for (int column = 0; column < Size; column++)
                        {
                            float sum = 0f;
                            for (int source = 0; source < InputChannels(layer); source++)
                            {
                                sum += ReadContribution(layer, channel, source, row, column);
                            }
                            // Parchment 4 teaches channel summation. Activation belongs to parchment 5.
                            _values[layer][channel, row, column] = sum;
                            _maxima[layer][channel] = Mathf.Max(_maxima[layer][channel], Mathf.Abs(sum));
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
                        float value = Mathf.Abs(_values[2][channel, row, column]) / (Size * Size);
                        if (channel % 2 == 0) { first += value; } else { second += value; }
                    }
                }
            }
            float total = first + second;
            return total > 0f ? (output == 0 ? first : second) / total : 0.5f;
        }

        private static bool Inside(int row, int column) => row >= 0 && row < Size && column >= 0 && column < Size;
    }
}
