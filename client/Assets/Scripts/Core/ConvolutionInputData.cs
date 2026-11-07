using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    // A detached snapshot of actual image pixels, top row first and left to right.
    public sealed class ConvolutionInputData
    {
        public const int Size = 5;
        private readonly Color32[] _pixels;

        public Color32 this[int index] => _pixels[index];
        public int SelectedRow { get; }
        public int SelectedColumn { get; }
        public bool WindowShifted => SelectedRow != 0 || SelectedColumn != 0;

        public ConvolutionInputData(IReadOnlyList<Color32> pixels, int selectedRow, int selectedColumn)
        {
            if (pixels.Count != Size * Size)
            {
                throw new ArgumentException("A convolution input requires twenty-five pixels.", nameof(pixels));
            }
            if (selectedRow < 0 || selectedRow > 2 || selectedColumn < 0 || selectedColumn > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedRow), "The selected 3 by 3 region must fit in the input.");
            }
            _pixels = new Color32[Size * Size];
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = pixels[i];
            }
            SelectedRow = selectedRow;
            SelectedColumn = selectedColumn;
        }
    }
}
