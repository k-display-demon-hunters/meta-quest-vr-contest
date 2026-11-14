using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // The feature map: one cell per filter position, painted brighter the larger its output value.
    internal sealed class FeatureMapDisplayController : IDisposable
    {
        private const float Pitch = PictureDisplayController.Pitch;
        // Cells without a value yet are slate so they cannot be mistaken for a small (dark) value.
        private static readonly Color32 Empty = new Color32(58, 74, 90, 255);

        private readonly Transform _root;
        private readonly Texture2D _texture;
        private readonly Transform _highlight;
        private readonly Text _readout;
        private readonly WorkbenchPrimitiveFactory _factory;

        public FeatureMapDisplayController(WorkbenchPrimitiveFactory factory, Transform anchor, RawImage image, Text readout)
        {
            _factory = factory;
            _root = anchor;
            _texture = new Texture2D(PictureConvolution.MapColumns, PictureConvolution.MapRows, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            image.texture = _texture;
            image.color = Color.white;
            _highlight = factory.Highlight("Current output cell", Pitch + 0.012f, Pitch + 0.012f);
            _highlight.SetParent(_root, false);
            _readout = readout;
        }

        // Workbench-local point just in front of a cell.
        public Vector3 CellPosition(int row, int column) =>
            _factory.LocalPoint(_root.TransformPoint(CellCenter(row, column) + Vector3.back * 0.03f));

        public void Clear()
        {
            Color32[] cells = new Color32[PictureConvolution.MapColumns * PictureConvolution.MapRows];
            for (int index = 0; index < cells.Length; index++) { cells[index] = Empty; }
            _texture.SetPixels32(cells);
            _texture.Apply();
            HideCurrent();
        }

        // Outputs of this filter lie between 0 and 255, the same range as a pixel value.
        public void Record(int row, int column, float value)
        {
            float brightness = value / 255f;
            _texture.SetPixel(column, PictureConvolution.MapRows - 1 - row, new Color(brightness, brightness, brightness));
            _texture.Apply();
        }

        // Marks the cell that belongs to the filter's current position and shows its value.
        public void ShowCurrent(int row, int column, float value)
        {
            _highlight.localPosition = CellCenter(row, column) + Vector3.back * 0.01f;
            _highlight.gameObject.SetActive(true);
            _readout.text = "이 자리의 출력값 " + value.ToString("0.0");
        }

        public void HideCurrent()
        {
            _highlight.gameObject.SetActive(false);
            _readout.text = "";
        }

        public void Dispose() => UnityEngine.Object.Destroy(_texture);

        private static Vector3 CellCenter(int row, int column) => new Vector3(
            (column - (PictureConvolution.MapColumns - 1) * 0.5f) * Pitch,
            ((PictureConvolution.MapRows - 1) * 0.5f - row) * Pitch,
            0f);
    }
}
