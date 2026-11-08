using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNNScreen
{
    // Immutable snapshot, top row first, left to right. Used only for RGB observation in this scene.
    public sealed class PixelPatchData
    {
        private readonly Color32[] _pixels;
        public Color32 this[int index] => _pixels[index];

        public PixelPatchData(IReadOnlyList<Color32> pixels)
        {
            if (pixels.Count != 9) { throw new ArgumentException("A pixel patch requires nine pixels.", nameof(pixels)); }
            _pixels = new Color32[9];
            for (int i = 0; i < 9; i++) { _pixels[i] = pixels[i]; }
        }
    }
}
