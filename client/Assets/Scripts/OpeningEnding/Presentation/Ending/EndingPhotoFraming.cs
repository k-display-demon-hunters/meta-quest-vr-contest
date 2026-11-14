using UnityEngine;

namespace Cinematic
{
    internal static class EndingPhotoFraming
    {
        internal static Rect Calculate(EndingSequenceData.MontageEntry entry, float frameAspect, out Rect visibleFocus)
        {
            Rect uv = entry.Clip.UV;
            Rect focus = entry.Focus;
            Texture2D still = entry.Clip.Still;
            float sourceAspect = still != null ? still.width * uv.width / (still.height * uv.height) : frameAspect;
            float width = Mathf.Min(1f, frameAspect / sourceAspect);
            float height = Mathf.Min(1f, sourceAspect / frameAspect);
            float x = CoverOffset(width, focus.xMin, focus.xMax);
            float y = CoverOffset(height, focus.yMin, focus.yMax);

            visibleFocus = Rect.MinMaxRect(
                Mathf.Clamp01((focus.xMin - x) / width), Mathf.Clamp01((focus.yMin - y) / height),
                Mathf.Clamp01((focus.xMax - x) / width), Mathf.Clamp01((focus.yMax - y) / height));
            return new Rect(uv.x + x * uv.width, uv.y + y * uv.height, width * uv.width, height * uv.height);
        }

        private static float CoverOffset(float size, float targetMin, float targetMax)
        {
            // Keep a centered crop where possible, shifting just enough to keep the target in view.
            float min = Mathf.Max(0f, targetMax - size);
            float max = Mathf.Min(1f - size, targetMin);
            return min <= max
                ? Mathf.Clamp((1f - size) * .5f, min, max)
                : Mathf.Clamp((targetMin + targetMax - size) * .5f, 0f, 1f - size);
        }

    }
}
