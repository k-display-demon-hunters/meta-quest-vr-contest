// 임시 더미 데이터. 나중에 다른 prefab으로 교체 예정.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeatureMapCorridor
{
    public sealed class DummyCnnModel : MonoBehaviour, ICnnModel
    {
        private static readonly string[][] Digits =
        {
            new[]
            {
                "................", ".......##.......", "......###.......", ".....####.......", "....##.##.......", ".......##.......", ".......##.......", ".......##.......", ".......##.......", ".......##.......", ".......##.......", ".......##.......", ".......##.......", "....########....", "....########....", "................"
            }, new[]
            {
                "................", ".....######.....", "....########....", "...##......##...", "...........##...", "...........##...", "..........##....", ".........##.....", "........##......", ".......##.......", "......##........", ".....##.........", "....##..........", "...##########...", "...##########...", "................"
            }, new[]
            {
                "................", "....#######.....", "...#########....", "...##.....###...", "...........##...", "...........##...", "......#####.....", "......#####.....", "...........##...", "...........##...", "...........##...", "...##.....###...", "...#########....", "....#######.....", "................", "................"
            }
        };
        private static readonly int[][] Palettes =
        {
            new[]
            {
                0xF59A23, 0xF7A83A, 0xEE8A1C, 0xF4B04E, 0xE8801C
            }, new[]
            {
                0x4F8F86, 0x5C9E93, 0x3F7C74, 0x5FA598, 0x477F78
            }
        };
        private static readonly float[][] Biases =
        {
            new[]
            {
                -.15f, -.2f
            }, new[]
            {
                0f, 0f
            }, new[]
            {
                0f, 0f, -.05f, 0f
            }
        };
        private readonly int[] _mapCounts =
        {
            2, 2, 4
        };
        private float[][][] _inputs;
        private float[][] _scales;
        private float[][][] _templates;
        public int Size => 16;
        public int LayerSize(int layer) => layer < 2 ? Size : Size / 2;
        public int[] MapsPerLayer => _mapCounts;
        private static readonly float[, ] Smooth =
        {
            {
                .06f, .12f, .06f
            },
            {
                .12f, .28f, .12f
            },
            {
                .06f, .12f, .06f
            }
        };
        private static readonly float[, ] Vertical =
        {
            {
                -.5f, 1f, -.5f
            },
            {
                -.5f, 1f, -.5f
            },
            {
                -.5f, 1f, -.5f
            }
        };
        private static readonly float[, ] Horizontal =
        {
            {
                -.5f, -.5f, -.5f
            },
            {
                1f, 1f, 1f
            },
            {
                -.5f, -.5f, -.5f
            }
        };
        private static readonly float[, ] Diagonal =
        {
            { -.5f, -.5f, 1f },
            { -.5f, 1f, -.5f },
            { 1f, -.5f, -.5f }
        };
        private static readonly float[, ][, ] First =
        {
            {
                Times(Smooth, 1.2f), Times(Smooth, -.2f), Times(Smooth, -1f)
            },
            {
                Times(Smooth, -2f), Times(Smooth, 1.2f), Times(Smooth, 1.6f)
            }
        };
        private static readonly float[, ][, ] Second =
        {
            {
                Times(Vertical, .6f), Times(Vertical, -.3f)
            },
            {
                Times(Horizontal, .6f), Times(Horizontal, -.3f)
            },
            {
                Times(Smooth, 1.5f), Times(Smooth, -1f)
            },
            {
                Times(Diagonal, .6f), Times(Diagonal, -.3f)
            }
        };
        private static float[, ] Times(float[, ] source, float multiplier)
        {
            var matrix = new float[3, 3];
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
            matrix[row, column] = source[row, column] * multiplier;
            return matrix;
        }

        public float[, ] Kernel(int layer, int map, int src) => (layer == 1 ? First : Second)[map, src];
        public float Bias(int layer, int map) => Biases[layer - 1][map];
        public float[][] Input(int digit)
        {
            InitializeInputs();
            return _inputs[digit - 1];
        }

        private void InitializeInputs()
        {
            if (_inputs != null)
            {
                return;
            }
            _inputs = new float[3][][];
            for (int d = 0; d < 3; d++)
            {
                uint seed = (uint)((d + 1) * 911 + 7);
                var channels = new[]
                {
                    new float[256], new float[256], new float[256]
                };
                for (int i = 0; i < 256; i++)
                {
                    float random = Next(ref seed);
                    int rgb = Palettes[Digits[d][i / 16][i % 16] == '#' ? 0 : 1][(int)(random * 5)];
                    channels[0][i] = (rgb >> 16) / 255f;
                    channels[1][i] = ((rgb >> 8) & 255) / 255f;
                    channels[2][i] = (rgb & 255) / 255f;
                }
                _inputs[d] = channels;
            }
        }

        private static float Next(ref uint state)
        {
            unchecked
            {
                state += 0x6D2B79F5;
                uint t = state;
                t = (t ^ (t >> 15)) * (1 | t);
                t = (t + ((t ^ (t >> 7)) * (61 | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296f;
            }
        }

        public LayerOutput[][] Forward(int digit, ISet<string> offMaps)
        {
            float[][] source = Input(digit);
            var result = new LayerOutput[3][];
            for (int layer = 1; layer <= result.Length; layer++)
            {
                int size = LayerSize(layer);
                int count = _mapCounts[layer - 1];
                var output = new LayerOutput[count];
                var next = new float[count][];
                for (int map = 0; map < count; map++)
                {
                    bool dead = offMaps != null && offMaps.Contains((layer + "-" + map));
                    var pre = new float[size * size];
                    var act = new float[pre.Length];
                    if (layer == 2)
                    {
                        for (int i = 0; i < pre.Length; i++)
                        {
                            pre[i] = PoolingMath.ReadWindow(source[map], LayerSize(layer - 1), i).Maximum;
                        }
                    }
                    else
                    {
                        var kernels = new float[source.Length][, ];
                        for (int src = 0; src < kernels.Length; src++)
                        {
                            kernels[src] = Kernel(layer, map, src);
                        }
                        for (int i = 0; i < pre.Length; i++)
                        {
                            float sum = Bias(layer, map);
                            for (int src = 0; src < kernels.Length; src++)
                            {
                                sum += ConvolutionMath.Contribution(source[src], kernels[src], size, i);
                            }
                            pre[i] = sum;
                        }
                    }
                    for (int i = 0; i < act.Length; i++)
                    {
                        act[i] = dead ? 0 : Mathf.Max(0, pre[i]);
                    }
                    output[map] = new LayerOutput
                    {
                        pre = pre, act = act, dead = dead
                    };
                    next[map] = act;
                }
                result[layer - 1] = output;
                source = next;
            }
            return result;
        }

        private void InitializeTemplates()
        {
            if (_templates != null)
            {
                return;
            }
            var baseline = new LayerOutput[3][][];
            for (int d = 0; d < 3; d++)
            {
                baseline[d] = Forward(d + 1, null);
            }
            _scales = new float[3][];
            for (int layer = 0; layer < 3; layer++)
            {
                _scales[layer] = new float[_mapCounts[layer]];
                for (int map = 0; map < _mapCounts[layer]; map++)
                {
                    float maximum = .001f;
                    foreach (LayerOutput[][] example in baseline)
                    foreach (float value in example[layer][map].act)
                    maximum = Mathf.Max(maximum, value);
                    _scales[layer][map] = layer == 1 ? _scales[0][map] : maximum;
                }
            }
            _templates = new float[3][][];
            for (int d = 0; d < 3; d++)
            {
                _templates[d] = new float[_mapCounts[2]][];
                for (int map = 0; map < _mapCounts[2]; map++)
                {
                    float[] output = baseline[d][2][map].act;
                    for (int i = 0; i < output.Length; i++)
                    {
                        output[i] /= _scales[2][map];
                    }
                    _templates[d][map] = output;
                }
            }
        }

        public float Scale(int layer, int map)
        {
            InitializeTemplates();
            return _scales[layer - 1][map];
        }

        public float[] OutputTemplate(int digit, int map)
        {
            InitializeTemplates();
            return _templates[digit - 1][map];
        }

        public float[] Scores(LayerOutput[][] forward)
        {
            InitializeTemplates();
            var scores = new float[3];
            for (int d = 0; d < 3; d++)
            {
                float dot = 0;
                float squared = 0;
                for (int map = 0; map < _mapCounts[2]; map++)
                {
                    float[] act = forward[2][map].act;
                    float[] template = _templates[d][map];
                    for (int i = 0; i < act.Length; i++)
                    {
                        dot += act[i] / _scales[2][map] * template[i];
                        squared += template[i] * template[i];
                    }
                }
                scores[d] = dot / Mathf.Max(.001f, squared);
            }
            return scores;
        }

        public float[] Probabilities(LayerOutput[][] forward)
        {
            float[] probabilities = Scores(forward);
            float max = Mathf.Max(probabilities[0], Mathf.Max(probabilities[1], probabilities[2]));
            float total = 0;
            for (int d = 0; d < probabilities.Length; d++)
            {
                probabilities[d] = Mathf.Exp(8 * (probabilities[d] - max));
                total += probabilities[d];
            }
            for (int d = 0; d < probabilities.Length; d++)
            {
                probabilities[d] /= total;
            }
            return probabilities;
        }
    }
}
