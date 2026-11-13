using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNNScreen
{
    // Owns the generated textures; the Inspector source asset is not owned by this result.
    public sealed class PixelImageData : IDisposable
    {
        public Texture2D PhotoTexture { get; }
        public Texture2D PixelTexture { get; }
        public IReadOnlyList<Color32> Pixels { get; }
        public int Columns => PixelTexture.width;
        public int Rows => PixelTexture.height;

        public PixelImageData(Texture2D photoTexture, Texture2D pixelTexture, Color32[] pixels)
        {
            PhotoTexture = photoTexture;
            PixelTexture = pixelTexture;
            Pixels = Array.AsReadOnly(pixels);
        }

        public PixelPatchData CapturePatch(Vector2Int origin)
        {
            Color32[] patch = new Color32[9];
            for (int i = 0; i < 9; i++)
            {
                patch[i] = Pixels[(origin.y + 2 - i / 3) * Columns + origin.x + i % 3];
            }
            return new PixelPatchData(patch);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(PixelTexture);
        }
    }
}
