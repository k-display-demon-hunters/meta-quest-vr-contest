namespace FeatureMapCorridor
{
    internal sealed partial class DigitExecutionData
    {
        private static readonly int[] Sizes = { 16, 8, 8 };
        // Fitted feature-regression kernels; see mnist-color-data/learned-filters.
        private static readonly float[][][] FirstKernels =
        {
            new[] { new[] { 0.0864394382f, 0.139560536f, 0.104613185f, 0.144950062f, 0.206640154f, 0.145957187f, 0.104448155f, 0.138458461f, 0.0854470804f }, new[] { 0.0001759921f, 0.0044628391f, 0.00120933959f, 0.00358252041f, 0.0101800803f, 0.00473131659f, 0.00479190843f, 0.00740712369f, 0.00227297889f }, new[] { -0.0526146702f, -0.080349572f, -0.0618175268f, -0.0846919566f, -0.116645426f, -0.0843501166f, -0.0631097257f, -0.0827076137f, -0.0528975613f } },
            new[] { new[] { -0.0864394382f, -0.139560536f, -0.104613185f, -0.144950062f, -0.206640154f, -0.145957187f, -0.104448155f, -0.138458461f, -0.0854470804f }, new[] { -0.0001759921f, -0.0044628391f, -0.00120933959f, -0.00358252041f, -0.0101800803f, -0.00473131659f, -0.00479190843f, -0.00740712369f, -0.00227297889f }, new[] { 0.0526146702f, 0.080349572f, 0.0618175268f, 0.0846919566f, 0.116645426f, 0.0843501166f, 0.0631097257f, 0.0827076137f, 0.0528975613f } }
        };
        private static readonly float[][][] SecondKernels =
        {
            new[] { new[] { -0.62166959f, 1.19215083f, -0.574949682f, -0.794670284f, 1.19990194f, -0.791309297f, -0.592020929f, 1.23930418f, -0.656247914f }, new[] { 0.103254698f, -0.173238724f, 0.0842788219f, -0.0372248292f, -0.458138704f, -0.0478851274f, 0.0800939128f, -0.154733032f, 0.0825472847f } },
            new[] { new[] { -0.595686018f, -0.786015511f, -0.593667209f, 1.09734595f, 1.20929492f, 1.13854051f, -0.586307704f, -0.786750972f, -0.628275335f }, new[] { 0.149858624f, -0.0661692023f, 0.16816248f, -0.253769815f, -0.508653402f, -0.286262006f, 0.157133713f, -0.0561502464f, 0.172731966f } },
            new[] { new[] { 0.0624256358f, 0.167740837f, 0.0744883046f, 0.133960634f, 0.294389397f, 0.142890394f, 0.0805236176f, 0.172270328f, 0.0621884689f }, new[] { 0.0155214984f, -0.0206024367f, 0.0158138294f, -0.0202621426f, -0.0650486574f, -0.0235164501f, 0.0145628555f, -0.0204274319f, 0.0165228713f } },
            new[] { new[] { -0.839290023f, -0.405210167f, 1.10478544f, -0.415604025f, 0.871299803f, -0.36709252f, 1.10763705f, -0.357397228f, -0.877787352f }, new[] { 0.0403980501f, 0.154080018f, -0.209520802f, 0.141857833f, -0.400795639f, 0.138872966f, -0.18272163f, 0.0999514014f, 0.0679171756f } }
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
