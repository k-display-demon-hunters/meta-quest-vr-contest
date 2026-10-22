using UnityEngine;

namespace KdyLearning
{
    // Fixed teaching sign and hand-designed filters, not the output of model training.
    public static class KdySignExampleData
    {
        public const int Width = 16;
        public const int Height = 12;
        private static readonly string[] LetterA =
        {
            "01110", "10001", "10001", "11111", "10001", "10001", "10001"
        };
        private static readonly string[] LetterI =
        {
            "11111", "00100", "00100", "00100", "00100", "00100", "11111"
        };
        private static readonly int[,,] Kernels =
        {
            { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } },
            { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } },
            { { 0, -1, 0 }, { -1, 4, -1 }, { 0, -1, 0 } }
        };

        public static Color[] CreateImage()
        {
            Color[] pixels = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    pixels[y * Width + x] = Pixel(x, y);
                }
            }
            return pixels;
        }

        public static Color Pixel(int x, int y)
        {
            int row = y - 2;
            if (row >= 0 && row < 7)
            {
                if (x >= 2 && x < 7 && LetterA[row][x - 2] == '1')
                {
                    return new Color(.20f, .88f, 1f, 1f);
                }
                if (x >= 9 && x < 14 && LetterI[row][x - 9] == '1')
                {
                    return new Color(1f, .70f, .20f, 1f);
                }
            }
            return new Color(.025f, .045f, .08f, 1f);
        }

        public static float Sample(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return 0f;
            }
            Color pixel = Pixel(x, y);
            return .2126f * pixel.r + .7152f * pixel.g + .0722f * pixel.b;
        }

        public static int Weight(int kind, int x, int y)
        {
            return Kernels[Mathf.Clamp(kind, 0, 2), y, x];
        }

        public static float Filter(int kind, int x, int y)
        {
            float response = 0f;
            for (int ky = 0; ky < 3; ky++)
            {
                for (int kx = 0; kx < 3; kx++)
                {
                    response += Weight(kind, kx, ky) * Sample(x + kx - 1, y + ky - 1);
                }
            }
            return response;
        }
    }
}
