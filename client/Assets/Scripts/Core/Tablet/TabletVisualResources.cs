using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Object;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletVisualResources : IDisposable
    {
        private Sprite _roundedSprite;
        private readonly List<Sprite> _generatedSprites = new List<Sprite>();
        private MeshFilter _bodyFilter;
        private Mesh _originalBodyMesh;
        private Mesh _bodyMesh;
        private Vector3 _originalBodyScale;
        public Sprite RoundedSprite => _roundedSprite;
        public void CreateRuntimeSprites() => _roundedSprite = CreateRoundedSprite(256, 24);

        public void BuildRoundedBody(MeshFilter bodyFilter)
        {
            _bodyFilter = bodyFilter;
            Transform body = bodyFilter.transform;
            _originalBodyMesh = _bodyFilter.sharedMesh;
            _originalBodyScale = body.localScale;
            const int cornerSegments = 12;
            const int ringCount = 4 * cornerSegments;
            float halfWidth = TabletWorldWidth * 0.5f;
            float halfHeight = TabletWorldWidth * CanvasHeight / CanvasWidth * 0.5f;
            const float radius = 0.0175f;
            Vector3[] vertices = new Vector3[ringCount * 2 + 2];
            List<int> triangles = new List<int>(ringCount * 12);
            for (int index = 0; index < ringCount; index++)
            {
                int corner = index / cornerSegments;
                float angle = (corner * 90f + index % cornerSegments * 90f / cornerSegments) * Mathf.Deg2Rad;
                float centerX = corner == 0 || corner == 3 ? halfWidth - radius : -halfWidth + radius;
                float centerY = corner < 2 ? halfHeight - radius : -halfHeight + radius;
                float x = centerX + Mathf.Cos(angle) * radius;
                float y = centerY + Mathf.Sin(angle) * radius;
                vertices[index] = new Vector3(x, y, -0.006f);
                vertices[index + ringCount] = new Vector3(x, y, 0.006f);
                int next = (index + 1) % ringCount;
                triangles.AddRange(new[] { ringCount * 2, next, index, ringCount * 2 + 1, index + ringCount, next + ringCount,
                    index, next, next + ringCount, index, next + ringCount, index + ringCount });
            }
            vertices[ringCount * 2] = new Vector3(0f, 0f, -0.006f);
            vertices[ringCount * 2 + 1] = new Vector3(0f, 0f, 0.006f);
            _bodyMesh = new Mesh { name = "Rounded Tablet Body" };
            _bodyMesh.vertices = vertices;
            _bodyMesh.triangles = triangles.ToArray();
            _bodyMesh.RecalculateNormals();
            _bodyMesh.RecalculateBounds();
            _bodyFilter.sharedMesh = _bodyMesh;
            body.localScale = Vector3.one;
        }

        public Sprite CreateGradientSprite(int radius, Color top, Color bottom)
        {
            Sprite sprite = CreateRoundedSprite(256, radius, 0f, top, bottom);
            _generatedSprites.Add(sprite);
            return sprite;
        }

        private Sprite CreateRoundedSprite(int size, int radius, float softness = 0f, Color? topColor = null, Color? bottomColor = null)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Tablet Rounded UI";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            float halfSize = size * 0.5f;
            float extent = halfSize - Mathf.Max(softness, 0.5f);
            float effectiveRadius = Mathf.Min(radius, extent);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(Mathf.Abs(x + 0.5f - halfSize), Mathf.Abs(y + 0.5f - halfSize))
                        - Vector2.one * (extent - effectiveRadius);
                    float distance = new Vector2(Mathf.Max(delta.x, 0f), Mathf.Max(delta.y, 0f)).magnitude
                        + Mathf.Min(Mathf.Max(delta.x, delta.y), 0f) - effectiveRadius;
                    float alpha = Mathf.Clamp01(0.5f - distance / Mathf.Max(1f, softness * 2f));
                    Color color = Color.Lerp(bottomColor ?? Color.white, topColor ?? Color.white, y / (float)(size - 1));
                    color.a *= alpha;
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Vector4 border = Vector4.one * Mathf.Min(halfSize, radius + softness);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        private static void DestroySprite(Sprite sprite)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }

        public void Dispose()
        {
            DestroySprite(_roundedSprite);
            foreach (Sprite sprite in _generatedSprites) { DestroySprite(sprite); }
            if (_bodyFilter != null && _bodyFilter.sharedMesh == _bodyMesh)
            {
                _bodyFilter.sharedMesh = _originalBodyMesh;
                _bodyFilter.transform.localScale = _originalBodyScale;
            }
            Destroy(_bodyMesh);
        }
    }
}
