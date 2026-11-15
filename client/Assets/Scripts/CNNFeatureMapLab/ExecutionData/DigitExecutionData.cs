namespace FeatureMapCorridor
{
    internal sealed partial class DigitExecutionData
    {
        private static readonly int[] Sizes = { 16, 8, 8 };
        // Digit-label classification kernels; see mnist-color-data/learned-filters.
        private static readonly float[][][] FirstKernels =
        {
            new[] { new[] { 0.401525438f, 0.150332659f, -0.24341324f, -0.236059934f, 0.641795814f, 0.38237986f, -0.390634865f, 0.268514454f, -0.0400497355f }, new[] { -0.0973554254f, -0.161777318f, 0.221408322f, 0.179670513f, -0.281969279f, -0.327359527f, 0.459611475f, 0.745335042f, 0.096679002f }, new[] { 0.164345533f, 0.311191201f, 0.727968395f, -0.305738956f, -0.320769012f, -0.0765196085f, 0.0353320949f, -0.69615072f, -0.880029261f } },
            new[] { new[] { -0.356446236f, -0.194887117f, -0.00280008418f, -0.639254689f, -0.882218659f, -0.936416328f, -1.00796521f, -0.419213355f, 0.452534705f }, new[] { -0.599521041f, -0.24296692f, 0.192960441f, -0.260632664f, -0.0201474521f, 0.188747212f, -0.138471633f, -0.373523146f, -0.281057179f }, new[] { 0.741295934f, 1.5089184f, 0.760710776f, 1.25247264f, 1.685112f, 1.34290493f, 1.25026906f, 1.53676307f, 0.919084311f } }
        };
        private static readonly float[][][] SecondKernels =
        {
            new[] { new[] { 0.594290435f, -0.221939474f, 0.454086214f, 0.355507433f, 0.141794115f, 0.128703728f, -0.147580057f, 0.851703882f, 0.28565836f }, new[] { 0.335398912f, -0.869114697f, -0.283114344f, 0.578566432f, 0.0144180385f, -0.0906376615f, -0.0396295115f, -0.168015942f, -0.617887378f } },
            new[] { new[] { 0.784144938f, 0.909339786f, 0.878483772f, 0.42585063f, -0.608840823f, 0.121383399f, 0.0619184859f, 0.683734953f, 0.143033102f }, new[] { -0.356021494f, 0.0913137496f, -0.102675118f, -0.446979851f, 0.145394534f, -0.0709114596f, 0.326158732f, -0.049007941f, -0.261588722f } },
            new[] { new[] { -0.442448854f, -0.095547989f, -0.396493763f, 0.0931011289f, 0.53690356f, 1.04146409f, -0.164004475f, 0.114712976f, 0.0141176935f }, new[] { 0.300562412f, 0.645045698f, 0.629148602f, -0.629037917f, -0.108106486f, -0.226083338f, -0.405536175f, -0.386187285f, 0.16865544f } },
            new[] { new[] { -0.808518708f, -0.252266526f, -0.0868967026f, 0.221164897f, 0.639140069f, -0.0782429501f, 0.317654908f, 0.103852026f, 0.0222886465f }, new[] { 0.672812283f, 0.0106325429f, 0.419304222f, 0.609829307f, -0.113429047f, -0.420994848f, -0.102934882f, -1.0688715f, -0.0190988816f } }
        };

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
