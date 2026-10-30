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
        private Sprite _circleSprite;
        private Sprite _sheetSprite;
        private Sprite _shadowSprite;
        private Sprite _panelSprite;
        private Sprite _selectedSprite;
        private Sprite _primarySprite;
        private Sprite _blueCircleSprite;
        private Sprite _mutedCircleSprite;
        private Sprite _underlineSprite;
        private readonly List<Sprite> _generatedSprites = new List<Sprite>();
        private readonly List<Sprite> _pixelSprites = new List<Sprite>();
        private MeshFilter _bodyFilter;
        private Mesh _originalBodyMesh;
        private Mesh _bodyMesh;
        private Vector3 _originalBodyScale;
        private Texture2D _symbolAtlas;
        private readonly Dictionary<TabletSymbol, Sprite> _symbolSprites = new Dictionary<TabletSymbol, Sprite>();

        public Sprite RoundedSprite => _roundedSprite;
        public Sprite CircleSprite => _circleSprite;
        public Sprite SheetSprite => _sheetSprite;
        public Sprite ShadowSprite => _shadowSprite;
        public Sprite PanelSprite => _panelSprite;
        public Sprite SelectedSprite => _selectedSprite;
        public Sprite PrimarySprite => _primarySprite;
        public Sprite BlueCircleSprite => _blueCircleSprite;
        public Sprite MutedCircleSprite => _mutedCircleSprite;
        public Sprite UnderlineSprite => _underlineSprite;
        public IReadOnlyList<Sprite> PixelSprites => _pixelSprites;
        public IReadOnlyDictionary<TabletSymbol, Sprite> SymbolSprites => _symbolSprites;

        public void CreateRuntimeSprites()
        {
            _roundedSprite = CreateRoundedSprite(256, 24);
            _circleSprite = CreateRoundedSprite(128, 64);
            _sheetSprite = CreateRoundedSprite(256, 36);
            _shadowSprite = CreateRoundedSprite(256, 44, 12f);
            _panelSprite = CreateGradientSprite(36, new Color(1f, 1f, 1f, 0.88f), new Color(0.94f, 0.97f, 0.98f, 0.7f));
            _selectedSprite = CreateGradientSprite(24, new Color(0.90f, 0.94f, 1f), new Color(0.82f, 0.88f, 0.98f));
            _primarySprite = CreateGradientSprite(36, new Color(0.37f, 0.52f, 0.72f), new Color(0.09f, 0.20f, 0.38f));
            _blueCircleSprite = CreateGradientSprite(128, new Color(0.49f, 0.66f, 0.88f), new Color(0.16f, 0.30f, 0.52f));
            _mutedCircleSprite = CreateGradientSprite(128, new Color(0.81f, 0.84f, 0.88f), new Color(0.68f, 0.72f, 0.77f));
            _underlineSprite = CreateUnderlineSprite();
            CreateSymbolSprites();
            for (int channel = -1; channel < 3; channel++)
            {
                _pixelSprites.Add(CreatePixelSprite(channel));
            }
        }

        public void BuildRoundedBody(Transform tablet)
        {
            Transform body = tablet.Find("Tablet Body");
            _bodyFilter = body != null ? body.GetComponent<MeshFilter>() : null;
            if (_bodyFilter == null)
            {
                return;
            }
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

        private Sprite CreatePixelSprite(int channel)
        {
            const int cellSize = 32;
            int columns = channel < 0 ? 9 : 5;
            int width = columns * cellSize;
            const int height = 5 * cellSize;
            Color32[] pixels = new Color32[width * height];
            Color[] channelColors = { new Color(1f, 0.35f, 0.38f), new Color(0.38f, 1f, 0.57f), new Color(0.47f, 0.59f, 1f) };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int cellX = x / cellSize + (channel < 0 ? 0 : 2);
                    int cellY = y / cellSize;
                    float value = 0.07f + (Mathf.Sin(cellX * 3.7f + cellY * 2.3f) + 1f) * 0.045f;
                    if (cellX == 4 && cellY == 2)
                    {
                        value = 1f;
                    }
                    else if (Mathf.Abs(cellX - 4) + Mathf.Abs(cellY - 2) == 1)
                    {
                        value = 0.31f;
                    }
                    Color source = new Color(value, value, value);
                    if (cellX == 5 && cellY == 2)
                    {
                        source = new Color(1f, 0.16f, 0.17f);
                    }
                    Color color = source;
                    if (channel >= 0)
                    {
                        float amount = channel == 0 ? source.r : channel == 1 ? source.g : source.b;
                        color = Color.Lerp(new Color(0.055f, 0.065f, 0.085f),
                            Color.Lerp(Color.white, channelColors[channel], 0.26f), amount);
                    }
                    if (x % cellSize == 0 || y % cellSize == 0)
                    {
                        color = Color.Lerp(color, new Color(0.45f, 0.50f, 0.58f), 0.28f);
                    }
                    float edgeX = Mathf.Max(Mathf.Max(8f - x, x - (width - 9f)), 0f);
                    float edgeY = Mathf.Max(Mathf.Max(8f - y, y - (height - 9f)), 0f);
                    color.a = Mathf.Clamp01(8.5f - Mathf.Sqrt(edgeX * edgeX + edgeY * edgeY));
                    pixels[y * width + x] = color;
                }
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = channel < 0 ? "Original Pixel Preview" : "RGB Pixel Preview " + channel;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateUnderlineSprite()
        {
            const int width = 320;
            const int height = 40;
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = x / (float)(width - 1);
                    float curve = 5f + 29f * (1f - t) * (1f - t);
                    float alpha = Mathf.Clamp01(3f - Mathf.Abs(height - 1f - y - curve)) * (1f - t * 0.75f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
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

        private void CreateSymbolSprites()
        {
            const int cellSize = 64;
            const int columns = 4;
            TabletSymbol[] symbols = (TabletSymbol[])Enum.GetValues(typeof(TabletSymbol));
            int width = columns * cellSize;
            int height = Mathf.CeilToInt(symbols.Length / (float)columns) * cellSize;
            Color32[] pixels = new Color32[width * height];
            foreach (TabletSymbol symbol in symbols)
            {
                List<Vector2[]> paths = GetSymbolPaths(symbol);
                int offsetX = (int)symbol % columns * cellSize;
                int offsetY = (int)symbol / columns * cellSize;
                for (int y = 0; y < cellSize; y++)
                {
                    for (int x = 0; x < cellSize; x++)
                    {
                        Vector2 point = new Vector2((x + 0.5f) * 24f / cellSize, 24f - (y + 0.5f) * 24f / cellSize);
                        float distanceSquared = float.PositiveInfinity;
                        foreach (Vector2[] path in paths)
                        {
                            for (int index = 1; index < path.Length; index++)
                            {
                                Vector2 segment = path[index] - path[index - 1];
                                float t = Mathf.Clamp01(Vector2.Dot(point - path[index - 1], segment) / segment.sqrMagnitude);
                                distanceSquared = Mathf.Min(distanceSquared, (point - path[index - 1] - segment * t).sqrMagnitude);
                            }
                        }
                        float alpha = Mathf.Clamp01((1f - Mathf.Sqrt(distanceSquared)) * cellSize / 24f + 0.5f);
                        pixels[(offsetY + y) * width + offsetX + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }
            }
            // Rasterize once and share one atlas across every tablet symbol.
            _symbolAtlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _symbolAtlas.name = "Tablet Lucide Symbols";
            _symbolAtlas.wrapMode = TextureWrapMode.Clamp;
            _symbolAtlas.filterMode = FilterMode.Bilinear;
            _symbolAtlas.SetPixels32(pixels);
            _symbolAtlas.Apply(false, true);
            foreach (TabletSymbol symbol in symbols)
            {
                Rect rect = new Rect((int)symbol % columns * cellSize, (int)symbol / columns * cellSize, cellSize, cellSize);
                _symbolSprites.Add(symbol, Sprite.Create(_symbolAtlas, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect));
            }
        }

        // Simplified Lucide line geometry: https://github.com/lucide-icons/lucide
        // ISC License: Copyright (c) 2026 Lucide Icons and Contributors
        // Permission to use, copy, modify, and/or distribute this software for any
        // purpose with or without fee is hereby granted, provided that the above
        // copyright notice and this permission notice appear in all copies.
        // THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
        // WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
        // MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
        // ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
        // WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
        // ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
        // OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
        //
        // Feather-derived check, chevrons and log-out also use the MIT License:
        // Copyright (c) 2013-present Cole Bemis
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        // The above copyright notice and this permission notice shall be included in
        // all copies or substantial portions of the Software.
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        // THE SOFTWARE.
        private List<Vector2[]> GetSymbolPaths(TabletSymbol symbol)
        {
            List<Vector2[]> paths = new List<Vector2[]>();
            switch (symbol)
            {
                case TabletSymbol.Check:
                    AddSymbolPath(paths, 20, 6, 9, 17, 4, 12);
                    break;
                case TabletSymbol.ChevronLeft:
                    AddSymbolPath(paths, 15, 18, 9, 12, 15, 6);
                    break;
                case TabletSymbol.ChevronRight:
                    AddSymbolPath(paths, 9, 18, 15, 12, 9, 6);
                    break;
                case TabletSymbol.Sun:
                    AddSymbolArc(paths, new Vector2(12f, 12f), 4f, 0f, 360f);
                    AddSymbolPath(paths, 12, 2, 12, 4);
                    AddSymbolPath(paths, 12, 20, 12, 22);
                    AddSymbolPath(paths, 4.93f, 4.93f, 6.34f, 6.34f);
                    AddSymbolPath(paths, 17.66f, 17.66f, 19.07f, 19.07f);
                    AddSymbolPath(paths, 2, 12, 4, 12);
                    AddSymbolPath(paths, 20, 12, 22, 12);
                    AddSymbolPath(paths, 6.34f, 17.66f, 4.93f, 19.07f);
                    AddSymbolPath(paths, 19.07f, 4.93f, 17.66f, 6.34f);
                    break;
                case TabletSymbol.Volume:
                    AddSymbolPath(paths, 11, 5, 6, 8, 3, 8, 2, 9, 2, 15, 3, 16, 6, 16, 11, 19, 11, 5);
                    AddSymbolArc(paths, new Vector2(12f, 12f), 5f, -36.87f, 36.87f);
                    AddSymbolArc(paths, new Vector2(13f, 12f), 9f, -45f, 45f);
                    break;
                case TabletSymbol.Settings:
                    AddSymbolPath(paths, 3, 5, 10, 5);
                    AddSymbolPath(paths, 14, 3, 14, 7);
                    AddSymbolPath(paths, 14, 5, 21, 5);
                    AddSymbolPath(paths, 3, 12, 8, 12);
                    AddSymbolPath(paths, 8, 10, 8, 14);
                    AddSymbolPath(paths, 12, 12, 21, 12);
                    AddSymbolPath(paths, 3, 19, 12, 19);
                    AddSymbolPath(paths, 16, 17, 16, 21);
                    AddSymbolPath(paths, 16, 19, 21, 19);
                    break;
                case TabletSymbol.Gear:
                    Vector2[] gear = new Vector2[49];
                    for (int index = 0; index < gear.Length; index++)
                    {
                        float angle = index * Mathf.PI * 2f / 48f;
                        float radius = index % 6 == 0 || index % 6 == 1 || index % 6 == 5 ? 10f : 7.8f;
                        gear[index] = new Vector2(12f, 12f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    }
                    paths.Add(gear);
                    AddSymbolArc(paths, new Vector2(12f, 12f), 3.2f, 0f, 360f);
                    break;
                case TabletSymbol.User:
                    AddSymbolArc(paths, new Vector2(12f, 6f), 3.5f, 0f, 360f);
                    AddSymbolArc(paths, new Vector2(12f, 21f), 7f, 180f, 360f);
                    break;
                case TabletSymbol.Bulb:
                    AddSymbolArc(paths, new Vector2(12f, 8f), 6f, 140f, 400f);
                    AddSymbolPath(paths, 7.4f, 11.9f, 9, 16, 15, 16, 16.6f, 11.9f);
                    AddSymbolPath(paths, 9, 19, 15, 19);
                    AddSymbolPath(paths, 10, 22, 14, 22);
                    break;
                case TabletSymbol.Download:
                    AddSymbolPath(paths, 12, 3, 12, 15);
                    AddSymbolPath(paths, 7, 10, 12, 15, 17, 10);
                    AddSymbolPath(paths, 3, 15, 3, 20, 4, 21, 20, 21, 21, 20, 21, 15);
                    break;
                case TabletSymbol.Expand:
                    AddSymbolPath(paths, 14, 3, 21, 3, 21, 10);
                    AddSymbolPath(paths, 21, 3, 14, 10);
                    AddSymbolPath(paths, 10, 21, 3, 21, 3, 14);
                    AddSymbolPath(paths, 3, 21, 10, 14);
                    break;
                case TabletSymbol.Box:
                    AddSymbolPath(paths, 3, 7, 12, 2, 21, 7, 21, 17, 12, 22, 3, 17, 3, 7);
                    AddSymbolPath(paths, 3.3f, 7, 12, 12, 20.7f, 7);
                    AddSymbolPath(paths, 12, 22, 12, 12);
                    break;
                case TabletSymbol.Exit:
                    AddSymbolPath(paths, 16, 17, 21, 12, 16, 7);
                    AddSymbolPath(paths, 21, 12, 9, 12);
                    AddSymbolPath(paths, 9, 21, 5, 21, 3, 19, 3, 5, 5, 3, 9, 3);
                    break;
                case TabletSymbol.Bookmark:
                    AddSymbolPath(paths, 5, 20, 5, 5, 7, 3, 17, 3, 19, 5, 19, 20, 12, 16, 5, 20);
                    break;
                case TabletSymbol.Vibrate:
                    AddSymbolPath(paths, 2, 8, 4, 10, 2, 12, 4, 14, 2, 16);
                    AddSymbolPath(paths, 22, 8, 20, 10, 22, 12, 20, 14, 22, 16);
                    AddSymbolPath(paths, 9, 5, 15, 5, 16, 6, 16, 18, 15, 19, 9, 19, 8, 18, 8, 6, 9, 5);
                    break;
                case TabletSymbol.Grid:
                    AddSymbolPath(paths, 5, 3, 19, 3, 21, 5, 21, 19, 19, 21, 5, 21, 3, 19, 3, 5, 5, 3);
                    AddSymbolPath(paths, 12, 3, 12, 21);
                    AddSymbolPath(paths, 3, 12, 21, 12);
                    break;
                case TabletSymbol.Book:
                    AddSymbolPath(paths, 12, 5, 8, 3, 4, 3, 2, 5, 2, 17, 4, 19, 8, 19, 12, 21,
                        16, 19, 20, 19, 22, 17, 22, 5, 20, 3, 16, 3, 12, 5, 12, 21);
                    break;
            }
            return paths;
        }

        private void AddSymbolPath(List<Vector2[]> paths, params float[] coordinates)
        {
            Vector2[] points = new Vector2[coordinates.Length / 2];
            for (int index = 0; index < points.Length; index++)
            {
                points[index] = new Vector2(coordinates[index * 2], coordinates[index * 2 + 1]);
            }
            paths.Add(points);
        }

        private void AddSymbolArc(List<Vector2[]> paths, Vector2 center, float radius, float startDegrees, float endDegrees)
        {
            const int segments = 24;
            Vector2[] points = new Vector2[segments + 1];
            for (int index = 0; index <= segments; index++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segments) * Mathf.Deg2Rad;
                points[index] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            paths.Add(points);
        }

        private void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                Destroy(sprite.texture);
                Destroy(sprite);
            }
        }

        public void Dispose()
        {
            DestroyRuntimeSprite(_roundedSprite);
            DestroyRuntimeSprite(_circleSprite);
            DestroyRuntimeSprite(_sheetSprite);
            DestroyRuntimeSprite(_shadowSprite);
            DestroyRuntimeSprite(_underlineSprite);
            foreach (Sprite sprite in _generatedSprites)
            {
                DestroyRuntimeSprite(sprite);
            }
            foreach (Sprite sprite in _pixelSprites)
            {
                DestroyRuntimeSprite(sprite);
            }
            foreach (Sprite sprite in _symbolSprites.Values)
            {
                Destroy(sprite);
            }
            Destroy(_symbolAtlas);
            if (_bodyFilter != null && _bodyFilter.sharedMesh == _bodyMesh)
            {
                _bodyFilter.sharedMesh = _originalBodyMesh;
                _bodyFilter.transform.localScale = _originalBodyScale;
            }
            Destroy(_bodyMesh);
        }

        internal enum TabletSymbol
        {
            Check, ChevronLeft, ChevronRight, Sun, Volume, Settings, Box, Exit, Bookmark, Vibrate, Grid, Book,
            Gear, User, Bulb, Download, Expand
        }
    }
}
