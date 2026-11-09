using System;
using UnityEngine;
namespace RealityLab
{
    public static class RealityImagePreprocessor
    {
        // Source uses Unity bottom-left texture coordinates. Model input is top-row-first HWC RGB.
        public static float[] Convert(Color32[] pixels, int width, int height, Rect region)
        {
            if (width < 1 || height < 1 || pixels == null || pixels.Length != checked(width * height))
                throw new ArgumentException("Invalid camera image.");
            if (!(region.width > 0 && region.height > 0 && region.xMin >= 0 && region.yMin >= 0
                && region.xMax <= 1 && region.yMax <= 1))
                throw new ArgumentException("Selection must be inside the camera viewport.");
            float[] input = new float[28 * 28 * 3];
            for (int y = 0; y < 28; y++)
            for (int x = 0; x < 28; x++)
            {
                float sx = Mathf.Clamp((region.x + (x + .5f) / 28 * region.width) * width - .5f, 0, width - 1);
                float sy = Mathf.Clamp((region.yMax - (y + .5f) / 28 * region.height) * height - .5f, 0, height - 1);
                int x0 = Mathf.FloorToInt(sx), y0 = Mathf.FloorToInt(sy);
                int x1 = Mathf.Min(x0 + 1, width - 1), y1 = Mathf.Min(y0 + 1, height - 1);
                Color a = Color.Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], sx - x0);
                Color b = Color.Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], sx - x0);
                Color value = Color.Lerp(a, b, sy - y0);
                int offset = (y * 28 + x) * 3;
                input[offset] = value.r; input[offset + 1] = value.g; input[offset + 2] = value.b;
            }
            return input;
        }
    }
}
