using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // The input picture as a pixel grid, and where a 3×3 filter sits on it.
    internal sealed class PictureDisplayController : IDisposable
    {
        public const float Pitch = 0.05f;
        private const float Width = PictureSampler.Columns * Pitch;
        private const float Height = PictureSampler.Rows * Pitch;
        // The filter floats just far enough in front for its green and blue layers to clear the picture.
        private const float FilterDepth = 0.045f;

        private readonly Transform _root;
        private readonly Texture2D _texture;

        public PictureDisplayController(WorkbenchPrimitiveFactory factory, Transform anchor)
        {
            _root = factory.Root("Input picture");
            _root.SetParent(anchor, false);
            factory.Tile(_root, new Vector3(0f, 0f, 0.03f), new Vector3(Width + 0.06f, Height + 0.06f, 0.026f), WorkbenchColors.PanelFace);
            factory.Frame(_root, Width + 0.06f, Height + 0.06f, 0.04f, WorkbenchColors.PanelFrame, 0.012f);
            Transform heading = factory.PanelHeading(_root);
            heading.localPosition = new Vector3(0f, Height * 0.5f + 0.14f, -0.025f);
            Text title = factory.Caption(heading, "입력 그림", new Vector3(0f, 0f, -0.027f), 0.88f);
            title.fontSize = WorkbenchColors.HeadingFontSize;
            RawImage image = factory.Picture(_root, new Vector3(0f, 0f, 0.01f), Width, Height);
            _texture = new Texture2D(PictureSampler.Columns, PictureSampler.Rows, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            image.texture = _texture;
        }

        public void Show(Color32[] pixels)
        {
            _texture.SetPixels32(pixels);
            _texture.Apply();
        }

        // Where the filter sits when its top-left cell is over (row, column).
        public Pose WindowPose(int row, int column) =>
            new Pose(_root.TransformPoint(WindowCenter(row, column) + Vector3.back * FilterDepth), _root.rotation);

        // The filter position closest to a point in space, kept inside the picture. x = column, y = row.
        public Vector2Int NearestWindow(Vector3 worldPoint)
        {
            Vector3 local = _root.InverseTransformPoint(worldPoint);
            int column = Mathf.RoundToInt(local.x / Pitch + (PictureSampler.Columns - 1) * 0.5f - 1f);
            int row = Mathf.RoundToInt((PictureSampler.Rows - 1) * 0.5f - 1f - local.y / Pitch);
            return new Vector2Int(Mathf.Clamp(column, 0, PictureConvolution.MapColumns - 1),
                Mathf.Clamp(row, 0, PictureConvolution.MapRows - 1));
        }

        public void Dispose() => UnityEngine.Object.Destroy(_texture);

        private static Vector3 WindowCenter(int row, int column) => new Vector3(
            (column + 1 - (PictureSampler.Columns - 1) * 0.5f) * Pitch,
            ((PictureSampler.Rows - 1) * 0.5f - (row + 1)) * Pitch,
            0f);
    }
}
