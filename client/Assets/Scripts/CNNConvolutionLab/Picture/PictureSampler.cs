using UnityEngine;

namespace CNNConvolution
{
    // Turns the source picture into the same 32×18 pixel grid that Room 1-1 shows:
    // 128×72 reduction, 5×5 blur, then one sample per grid cell.
    internal static class PictureSampler
    {
        public const int Columns = 32;
        public const int Rows = 18;
        private const int SmoothWidth = 128;
        private const int SmoothHeight = 72;
        private const int BlurRadius = 2;

        // Pixels are returned in texture order: left to right, bottom row first.
        public static Color32[] Sample(Texture2D source)
        {
            Texture2D smooth = CreateSmoothPicture(source);
            Color32[] pixels = new Color32[Columns * Rows];
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    pixels[y * Columns + x] = smooth.GetPixelBilinear((x + 0.5f) / Columns, (y + 0.5f) / Rows);
                }
            }
            Object.Destroy(smooth);
            return pixels;
        }

        private static Texture2D CreateSmoothPicture(Texture2D source)
        {
            Color[] reduced = new Color[SmoothWidth * SmoothHeight];
            for (int y = 0; y < SmoothHeight; y++)
            {
                for (int x = 0; x < SmoothWidth; x++)
                {
                    reduced[y * SmoothWidth + x] = source.GetPixelBilinear((x + 0.5f) / SmoothWidth, (y + 0.5f) / SmoothHeight);
                }
            }

            Color32[] blurred = new Color32[reduced.Length];
            int samples = (BlurRadius * 2 + 1) * (BlurRadius * 2 + 1);
            for (int y = 0; y < SmoothHeight; y++)
            {
                for (int x = 0; x < SmoothWidth; x++)
                {
                    Color sum = Color.clear;
                    for (int dy = -BlurRadius; dy <= BlurRadius; dy++)
                    {
                        for (int dx = -BlurRadius; dx <= BlurRadius; dx++)
                        {
                            int row = Mathf.Clamp(y + dy, 0, SmoothHeight - 1);
                            int column = Mathf.Clamp(x + dx, 0, SmoothWidth - 1);
                            sum += reduced[row * SmoothWidth + column];
                        }
                    }
                    blurred[y * SmoothWidth + x] = sum / samples;
                }
            }

            Texture2D texture = new Texture2D(SmoothWidth, SmoothHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(blurred);
            texture.Apply();
            return texture;
        }
    }
}
