using UnityEngine;

namespace CNNScreen
{
    public sealed class PixelImageFactory : MonoBehaviour
    {
        public const int Columns = 32;
        public const int Rows = 18;
        [SerializeField] private Texture2D _sourceImage;

        public PixelImageData Create()
        {
            Texture2D samplingImage = CreateSmoothPhoto(_sourceImage);
            Color32[] pixels = SamplePixels(samplingImage, Columns, Rows);
            Destroy(samplingImage);
            Texture2D pixelTexture = CreateTexture(pixels, Columns, Rows, FilterMode.Point);
            return new PixelImageData(_sourceImage, pixelTexture, pixels);
        }

        private static Texture2D CreateTexture(Color32[] pixels, int columns, int rows, FilterMode filterMode)
        {
            Texture2D texture = new Texture2D(columns, rows, TextureFormat.RGBA32, false);
            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSmoothPhoto(Texture2D sourceImage)
        {
            const int Width = 128;
            const int Height = 72;
            const int Radius = 2;
            Color[] source = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color color = sourceImage.GetPixelBilinear((x + 0.5f) / Width, (y + 0.5f) / Height);
                    source[y * Width + x] = color;
                }
            }
            Color32[] colors = new Color32[source.Length];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color sum = Color.clear;
                    for (int dy = -Radius; dy <= Radius; dy++)
                    {
                        for (int dx = -Radius; dx <= Radius; dx++)
                        {
                            sum += source[Mathf.Clamp(y + dy, 0, Height - 1) * Width + Mathf.Clamp(x + dx, 0, Width - 1)];
                        }
                    }
                    Color blurred = sum / 25f;
                    colors[y * Width + x] = blurred;
                }
            }
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(colors);
            texture.Apply();
            return texture;
        }

        private static Color32[] SamplePixels(Texture2D image, int columns, int rows)
        {
            Color32[] pixels = new Color32[columns * rows];
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    pixels[y * columns + x] = image.GetPixelBilinear((x + 0.5f) / columns, (y + 0.5f) / rows);
                }
            }
            return pixels;
        }
    }
}
