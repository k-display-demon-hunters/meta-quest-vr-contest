using Core;
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

        public ConvolutionInputData CaptureConvolutionInput(Vector2Int selectedOrigin)
        {
            // Selection uses bottom-left texture coordinates. Extend to the right and
            // down from its top-left, moving the whole crop inward at image edges.
            int left = Mathf.Clamp(selectedOrigin.x, 0, Columns - ConvolutionInputData.Size);
            int bottom = Mathf.Clamp(selectedOrigin.y - 2, 0, Rows - ConvolutionInputData.Size);
            int top = bottom + ConvolutionInputData.Size - 1;
            Color32[] pixels = new Color32[ConvolutionInputData.Size * ConvolutionInputData.Size];
            for (int row = 0; row < ConvolutionInputData.Size; row++)
            {
                for (int column = 0; column < ConvolutionInputData.Size; column++)
                {
                    pixels[row * ConvolutionInputData.Size + column] = Pixels[(top - row) * Columns + left + column];
                }
            }
            return new ConvolutionInputData(pixels, top - (selectedOrigin.y + 2), selectedOrigin.x - left);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(PhotoTexture);
            UnityEngine.Object.Destroy(PixelTexture);
        }
    }
}
