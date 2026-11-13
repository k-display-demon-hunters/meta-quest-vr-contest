namespace FeatureMapCorridor
{
    internal sealed partial class DigitExecutionData
    {
        private static readonly int[] Sizes = { 16, 8, 8 };
        // The saved convolution results were generated with these kernels from b14a2db.
        private static readonly float[] Smooth = { .06f, .12f, .06f, .12f, .28f, .12f, .06f, .12f, .06f };
        private static readonly float[][][] FirstKernels =
        {
            ScaleKernel(Smooth, 1.2f, -.2f, -1f),
            ScaleKernel(Smooth, -2f, 1.2f, 1.6f)
        };

        private static float[][] ScaleKernel(float[] kernel, params float[] channelScales) =>
            System.Array.ConvertAll(channelScales, scale => System.Array.ConvertAll(kernel, weight => weight * scale));

        private static readonly DigitExecutionData[] Digits = { CreateDigit1(), CreateDigit2(), CreateDigit3() };

        private static readonly float[][][] OutputTemplates = System.Array.ConvertAll(Digits, digit => digit._templates);

        private readonly float[][] _input;
        private readonly SavedConvMap[] _first;
        private readonly float[][] _pool;
        private readonly PoolingCalculationResult[] _poolWindows;
        private readonly SavedConvMap[] _second;
        private readonly float[][] _templates;
        private readonly SavedOutput _output;

        private DigitExecutionData(float[][] input, SavedConvMap[] first, float[][] pool,
            PoolingCalculationResult[] poolWindows, SavedConvMap[] second, float[][] templates, SavedOutput output)
        {
            _input = input;
            _first = first;
            _pool = pool;
            _poolWindows = poolWindows;
            _second = second;
            _templates = templates;
            _output = output;
        }

        internal static DigitExecutionData ForDigit(int digit) => Digits[digit - 1];

        internal FeatureMapSnapshot GetSnapshot()
        {
            var maps = new[]
            {
                new[] { _first[0].Pixels, _first[1].Pixels },
                _pool,
                new[] { _second[0].Pixels, _second[1].Pixels, _second[2].Pixels, _second[3].Pixels }
            };
            return new FeatureMapSnapshot(Sizes, _input, maps, DisplayScales, _output.Scores, _output.Probabilities, OutputTemplates);
        }

        internal RoundCalculationResult GetRound(int digit, int layer, int map)
        {
            if (layer == 4)
            {
                float scale = OutputScales[digit - 1];
                return new RoundCalculationResult(4, map, digit, 0, 8, _output.Contributions,
                    _templates, new[] { scale, scale, scale, scale }, new float[4], 0f, 0f);
            }

            return layer == 1 ? _first[map].Round : _second[map].Round;
        }

        internal PoolingCalculationResult GetPooling(int channel) => _poolWindows[channel];

        private readonly struct SavedConvMap
        {
            internal readonly float[] Pixels;
            internal readonly RoundCalculationResult Round;

            internal SavedConvMap(float[] pixels, RoundCalculationResult round)
            {
                Pixels = pixels;
                Round = round;
            }
        }

        private readonly struct SavedOutput
        {
            internal readonly float[] Scores;
            internal readonly float[] Probabilities;
            internal readonly float[][] Contributions;

            internal SavedOutput(float[] scores, float[] probabilities, float[][] contributions)
            {
                Scores = scores;
                Probabilities = probabilities;
                Contributions = contributions;
            }
        }
    }
}
