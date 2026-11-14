using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // The input picture as a pixel grid, and where a 3×3 filter sits on it.
    internal sealed class PictureDisplayController : IDisposable
    {
        public const float Pitch = 0.05f;
        // The filter floats just far enough in front for its green and blue layers to clear the picture.
        private const float FilterDepth = 0.045f;

        private readonly Transform _root;
        private readonly Texture2D _source;
        private readonly Texture2D _texture;

        public PictureDisplayController(Transform anchor, RawImage image)
        {
            _root = anchor;
            _source = (Texture2D)image.texture;
            _texture = new Texture2D(PictureSampler.Columns, PictureSampler.Rows, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            image.texture = _texture;
        }

        public Color32[] Show()
        {
            Color32[] pixels = PictureSampler.Sample(_source);
            _texture.SetPixels32(pixels);
            _texture.Apply();
            return pixels;
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
