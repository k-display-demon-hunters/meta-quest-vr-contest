using UnityEngine;

namespace KdyLearning
{
    // A held-out teaching sign shared by the pickup and inspection scenes. No model inference.
    public static class KdyFactorySignExampleData
    {
        public const int Width = 16;
        public const int Height = 12;
        private static readonly string[] VGlyph =
        {
            "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#.."
        };
        private static readonly string[] RGlyph =
        {
            "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"
        };
        private static readonly Color[] ClearImage = BuildClearImage();
        private static readonly Color[] BlurredImage = Blur(ClearImage);
        public static float BaselineError { get; } = KdyTrainingExampleData.MeanSquaredError(BlurredImage, ClearImage);

        public static Color[] CreateClearImage()
        {
            return (Color[])ClearImage.Clone();
        }

        public static Color[] CreateBlurredImage()
        {
            return (Color[])BlurredImage.Clone();
        }

        public static float GetImprovement(float trainingImprovement)
        {
            // A separate authored outcome for a new image; the training target is not a held-out guarantee.
            return Mathf.Clamp(trainingImprovement - 1.6f, 0f, 98f);
        }

        public static Color[] BlendForImprovement(float improvement)
        {
            float amount = 1f - Mathf.Sqrt(1f - Mathf.Clamp(improvement, 0f, 99.9f) / 100f);
            Color[] pixels = new Color[ClearImage.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.Lerp(BlurredImage[i], ClearImage[i], amount);
                pixels[i].a = 1f;
            }
            return pixels;
        }

        private static Color[] BuildClearImage()
        {
            Color[] pixels = new Color[Width * Height];
            Color background = new Color(.035f, .09f, .16f, 1f);
            Color border = new Color(.12f, .6f, .8f, 1f);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    pixels[y * Width + x] = x == 0 || x == Width - 1 || y == 0 || y == Height - 1 ? border : background;
                }
            }
            PaintGlyph(pixels, VGlyph, 2, 2, new Color(.2f, .95f, 1f, 1f));
            PaintGlyph(pixels, RGlyph, 9, 2, new Color(1f, .68f, .14f, 1f));
            return pixels;
        }

        private static void PaintGlyph(Color[] image, string[] glyph, int originX, int originY, Color color)
        {
            for (int y = 0; y < glyph.Length; y++)
            {
                for (int x = 0; x < glyph[y].Length; x++)
                {
                    if (glyph[y][x] == '#')
                    {
                        image[(originY + y) * Width + originX + x] = color;
                    }
                }
            }
        }

        private static Color[] Blur(Color[] source)
        {
            Color[] pixels = new Color[source.Length];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color sum = Color.clear;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int px = Mathf.Clamp(x + dx, 0, Width - 1);
                            int py = Mathf.Clamp(y + dy, 0, Height - 1);
                            sum += source[py * Width + px];
                        }
                    }
                    pixels[y * Width + x] = sum / 9f;
                }
            }
            return pixels;
        }
    }
}
