using System;
using UnityEngine;

namespace CNNScreen
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class PixelGridData : MonoBehaviour
    {
        [SerializeField] private Texture2D _sourceTexture;
        [Min(1)] public int gridWidth = 48;
        [Min(1)] public int gridHeight = 27;

        private Color32[] _colors;
        private Texture2D _sampledTexture;
        private Vector2Int _sampledSize;
        private uint _sampledRevision;

        internal Bounds SurfaceBounds => GetComponent<MeshFilter>().sharedMesh.bounds;
        internal bool SourceIsSrgb => _sourceTexture.isDataSRGB;

        public Color32 GetColor(int x, int y)
        {
            if (x < 0 || x >= gridWidth) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= gridHeight) throw new ArgumentOutOfRangeException(nameof(y));
            if (!TryPrepare(out string error)) throw new InvalidOperationException(error);

            return _colors[y * gridWidth + x];
        }

        // Like the source UVs, (0, 0) is the bottom-left cell. Depth is ignored.
        public bool TryWorldToGrid(Vector3 worldPosition, out Vector2Int cell)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            Bounds bounds = SurfaceBounds;
            float u = (local.x - bounds.min.x) / bounds.size.x;
            float v = (local.y - bounds.min.y) / bounds.size.y;
            const float edgeTolerance = 0.00001f;
            if (gridWidth < 1 || gridHeight < 1 || u < -edgeTolerance || u > 1f + edgeTolerance || v < -edgeTolerance || v > 1f + edgeTolerance)
            {
                cell = default;
                return false;
            }

            cell = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(u * gridWidth), 0, gridWidth - 1),
                Mathf.Clamp(Mathf.FloorToInt(v * gridHeight), 0, gridHeight - 1));
            return true;
        }

        internal bool TryGetPatchCenter(Vector3 worldPosition, out Vector2Int center)
        {
            if (!TryWorldToGrid(worldPosition, out center)) return false;

            center.x = gridWidth >= 3 ? Mathf.Clamp(center.x, 1, gridWidth - 2) : center.x;
            center.y = gridHeight >= 3 ? Mathf.Clamp(center.y, 1, gridHeight - 2) : center.y;
            return true;
        }

        internal bool TryCapturePatch(Vector3 worldPosition, out PixelPatchData patch)
        {
            patch = null;
            if (gridWidth < 3 || gridHeight < 3 || !TryGetPatchCenter(worldPosition, out Vector2Int center))
            {
                Debug.LogWarning("A 3×3 RGB patch requires at least three rows and columns and a selection on the board.", this);
                return false;
            }
            if (!TryPrepare(out string error))
            {
                Debug.LogWarning(error, this);
                return false;
            }

            const int patchSide = 3;
            Color32[] colors = new Color32[patchSide * patchSide];
            for (int row = 0; row < patchSide; row++)
            {
                for (int column = 0; column < patchSide; column++)
                {
                    int x = center.x - 1 + column;
                    int y = center.y + 1 - row;
                    Color32 color = GetColor(x, y);
                    colors[row * patchSide + column] = color;
                }
            }

            patch = new PixelPatchData(colors);
            return true;
        }

        internal bool TryPrepare(out string error)
        {
            error = null;
            if (_sourceTexture == null || gridWidth < 1 || gridHeight < 1)
            {
                error = "Assign a Source Texture and use positive grid dimensions.";
                return false;
            }
            if (!_sourceTexture.isReadable)
            {
                error = $"Enable Read/Write in the import settings for {_sourceTexture.name}.";
                return false;
            }

            Vector2Int size = new Vector2Int(gridWidth, gridHeight);
            uint revision = _sourceTexture.updateCount;
            if (_colors != null && _sampledTexture == _sourceTexture && _sampledSize == size && _sampledRevision == revision)
            {
                return true;
            }

            Color32[] source = _sourceTexture.GetPixels32();
            int width = _sourceTexture.width;
            int height = _sourceTexture.height;
            Color32[] colors = new Color32[gridWidth * gridHeight];
            for (int y = 0; y < gridHeight; y++)
            {
                double bottom = (double)y * height / gridHeight;
                double top = (double)(y + 1) * height / gridHeight;
                for (int x = 0; x < gridWidth; x++)
                {
                    double left = (double)x * width / gridWidth;
                    double right = (double)(x + 1) * width / gridWidth;
                    colors[y * gridWidth + x] = Average(source, width, left, right, bottom, top);
                }
            }

            _colors = colors;
            _sampledTexture = _sourceTexture;
            _sampledSize = size;
            _sampledRevision = revision;
            return true;
        }

        private static Color32 Average(Color32[] source, int stride, double left, double right, double bottom, double top)
        {
            double red = 0, green = 0, blue = 0;
            int endX = (int)Math.Ceiling(right);
            int endY = (int)Math.Ceiling(top);
            for (int y = (int)Math.Floor(bottom); y < endY; y++)
            {
                double overlapY = Math.Min(top, y + 1) - Math.Max(bottom, y);
                for (int x = (int)Math.Floor(left); x < endX; x++)
                {
                    // A source texel cut by a cell edge contributes only the intersecting area.
                    double weight = overlapY * (Math.Min(right, x + 1) - Math.Max(left, x));
                    Color32 color = source[y * stride + x];
                    red += color.r * weight;
                    green += color.g * weight;
                    blue += color.b * weight;
                }
            }

            double area = (right - left) * (top - bottom);
            return new Color32((byte)Math.Round(red / area), (byte)Math.Round(green / area),
                (byte)Math.Round(blue / area), 255);
        }
    }
}
