using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Parchment
{
    public sealed class ParchmentDigitIllustration : MonoBehaviour
    {
        private const int GridSide = 8;
        private const int TexelsPerCell = 16;
        private static readonly Vector2 GridCenter = new Vector2(14f, 24f);
        private static readonly Color32 TileGap = new Color32(10, 23, 36, 255);
        private static readonly Color32 Ink = new Color32(209, 246, 255, 255);
        private static readonly Color32[] ChannelColors =
        {
            new Color32(255, 104, 99, 255), new Color32(111, 238, 165, 255), new Color32(115, 177, 255, 255)
        };
        private static readonly string[] ChannelNames = { "R", "G", "B" };
        private static readonly byte[] Brightness =
        {
              0,  22,  72, 142, 166,  80,  28,   0,
             14,  96, 234, 255, 255, 232,  85,  12,
             18,  44,  93, 109, 148, 255, 171,  22,
              0,  20,  47,  94, 223, 236,  81,  10,
              0,  16,  74, 225, 244, 102,  38,   0,
              8,  63, 239, 247, 115,  43,  14,   0,
             22, 112, 255, 255, 246, 237, 193,  44,
              0,  27,  95, 122, 140, 133,  92,  16
        };
        private static readonly string[] ColorDigit =
        {
            "........", "..#####.", "......#.", ".....#..",
            "...###..", "......#.", "..#####.", "........"
        };
        private static readonly Color32[] WarmTiles =
        {
            new Color32(248, 167, 90, 255), new Color32(237, 123, 93, 255),
            new Color32(253, 200, 119, 255), new Color32(223, 137, 101, 255)
        };
        private static readonly Color32[] CoolTiles =
        {
            new Color32(68, 139, 151, 255), new Color32(68, 103, 151, 255),
            new Color32(92, 156, 139, 255), new Color32(79, 122, 133, 255)
        };

        [SerializeField] private Font _labelFont;
        [SerializeField, Range(192f, 224f)] private float _gridSize = 216f;
        [SerializeField] private Vector3 _channelStep = new Vector3(-32f, 12f, 32f);
        [SerializeField, Min(0f)] private float _pixelHold = 0.23f;
        [SerializeField, Min(0.05f)] private float _pixelTransition = 0.55f;
        [SerializeField, Min(0f)] private float _rgbHold = 0.45f;
        [SerializeField, Min(0.05f)] private float _rgbTransition = 0.65f;

        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly RectTransform[] _channels = new RectTransform[3];
        private readonly CanvasGroup[] _channelOpacity = new CanvasGroup[3];
        private RectTransform _pixels;
        private RectTransform _grayscale;
        private RectTransform _rgb;
        private Text _caption;
        private RawImage _smoothDigit;
        private RawImage _pixelGrid;
        private RawImage _colorGrid;
        private ParchmentIllustrationType _current;
        private float _elapsed;
        private bool _playing;

        internal void Show(ParchmentIllustrationType illustration, bool animate)
        {
            if (_caption == null) Build();
            _current = illustration;
            _pixels.gameObject.SetActive(illustration == ParchmentIllustrationType.PixelDigit);
            _grayscale.gameObject.SetActive(illustration == ParchmentIllustrationType.GrayscaleDigit);
            _rgb.gameObject.SetActive(illustration == ParchmentIllustrationType.RgbDigit);
            _caption.text = illustration switch
            {
                ParchmentIllustrationType.PixelDigit => "8 × 8 = 64 픽셀",
                ParchmentIllustrationType.GrayscaleDigit => "흑백 = 밝기 숫자 하나",
                _ => "RGB = 채널 3장"
            };
            ResetAnimation(animate);
        }

        internal void Play() => ResetAnimation(true);

        private void OnDisable() => _playing = false;

        private void OnDestroy()
        {
            foreach (Texture2D texture in _textures) Destroy(texture);
        }

        private void ResetAnimation(bool play)
        {
            _elapsed = 0f;
            _playing = play && _current != ParchmentIllustrationType.GrayscaleDigit;
            if (_current == ParchmentIllustrationType.PixelDigit) DrawPixels(0f);
            if (_current == ParchmentIllustrationType.RgbDigit) DrawChannels(0f);
        }

        private void Update()
        {
            if (!_playing) return;

            _elapsed += Time.unscaledDeltaTime;
            bool pixelPage = _current == ParchmentIllustrationType.PixelDigit;
            float hold = pixelPage ? _pixelHold : _rgbHold;
            float duration = pixelPage ? _pixelTransition : _rgbTransition;
            float time = Mathf.Clamp01((_elapsed - hold) / duration);
            float progress = Mathf.SmoothStep(0f, 1f, time);
            if (pixelPage) DrawPixels(progress);
            else DrawChannels(progress);
            _playing = time < 1f;
        }

        private void DrawPixels(float progress)
        {
            // The grid samples the same smooth strokes, so the silhouette stays in place.
            _smoothDigit.gameObject.SetActive(progress < 1f);
            _pixelGrid.color = new Color(1f, 1f, 1f, progress);
        }

        private void DrawChannels(float progress)
        {
            _colorGrid.gameObject.SetActive(progress < 1f);
            for (int channel = 0; channel < _channels.Length; channel++)
            {
                // Same left/up/back progression as the RGB plates in the workbench.
                _channels[channel].localPosition = (Vector3)GridCenter + _channelStep * (channel * progress);
                _channelOpacity[channel].alpha = progress;
            }
            _colorGrid.color = new Color(1f, 1f, 1f, 1f - progress);
        }

        private void Build()
        {
            _pixels = AddRect(transform, "Digit 1 · pixels", Vector2.zero, Vector2.zero);
            _grayscale = AddRect(transform, "Digit 2 · brightness", Vector2.zero, Vector2.zero);
            _rgb = AddRect(transform, "Digit 3 · RGB", Vector2.zero, Vector2.zero);
            Vector2 gridSize = Vector2.one * _gridSize;

            int resolution = GridSide * TexelsPerCell;
            Texture2D smooth = CreateTexture("Smooth digit 1", resolution, resolution,
                (x, y) => Color.Lerp(TileGap, Ink, DigitOneCoverage(new Vector2(x + 0.5f, y + 0.5f) / TexelsPerCell)));
            smooth.filterMode = FilterMode.Bilinear;
            _smoothDigit = AddPicture(_pixels, smooth, GridCenter, gridSize);
            _pixelGrid = AddPicture(_pixels, GridTexture("Pixel digit 1", (x, y) =>
                Color.Lerp(TileGap, Ink, DigitOneCoverage(new Vector2(x + 0.5f, y + 0.5f)))), GridCenter, gridSize);

            BuildGrayscale(gridSize);
            for (int channel = _channels.Length - 1; channel >= 0; channel--) BuildChannel(channel, gridSize);
            _colorGrid = AddPicture(_rgb, GridTexture("Color digit 3", ColorAt), GridCenter, gridSize);
            _caption = AddLabel(transform, "", new Vector2(GridCenter.x, -129f), new Vector2(274f, 26f), 20, Ink);
        }

        private void BuildGrayscale(Vector2 gridSize)
        {
            AddPicture(_grayscale, GridTexture("Grayscale digit 2", (x, y) =>
            {
                byte value = Brightness[y * GridSide + x];
                return new Color32(value, value, value, 255);
            }), GridCenter, gridSize);

            float cell = _gridSize / GridSide;
            for (int row = 0; row < GridSide; row++)
            {
                for (int column = 0; column < GridSide; column++)
                {
                    byte value = Brightness[row * GridSide + column];
                    Vector2 center = GridCenter + new Vector2(column - 3.5f, 3.5f - row) * cell;
                    AddLabel(_grayscale, value.ToString(), center, Vector2.one * cell, 15,
                        value >= 118 ? Color.black : Color.white);
                }
            }

            float barY = GridCenter.y - _gridSize * 0.5f - 17f;
            Texture2D gradient = CreateTexture("Brightness range 0–255", 256, 1,
                (x, _) => new Color32((byte)x, (byte)x, (byte)x, 255));
            AddPicture(_grayscale, gradient, new Vector2(GridCenter.x, barY), new Vector2(_gridSize - 62f, 8f));
            float endpoint = _gridSize * 0.5f - 15f;
            AddLabel(_grayscale, "0", new Vector2(GridCenter.x - endpoint, barY), new Vector2(28f, 22f), 17, Ink);
            AddLabel(_grayscale, "255", new Vector2(GridCenter.x + endpoint, barY), new Vector2(29f, 22f), 17, Ink);
        }

        private void BuildChannel(int channel, Vector2 gridSize)
        {
            string label = ChannelNames[channel];
            RectTransform layer = AddRect(_rgb, label + " channel", GridCenter, gridSize);
            _channels[channel] = layer;
            CanvasGroup opacity = layer.gameObject.AddComponent<CanvasGroup>();
            opacity.blocksRaycasts = false;
            opacity.interactable = false;
            _channelOpacity[channel] = opacity;

            Color32 tint = ChannelColors[channel];
            Texture2D texture = GridTexture(label + " values", (x, y) =>
            {
                Color32 rgb = ColorAt(x, y);
                float value = (channel == 0 ? rgb.r : channel == 1 ? rgb.g : rgb.b) / 255f;
                return Color.Lerp(TileGap, tint, value);
            });
            AddPicture(layer, texture, Vector2.zero, gridSize);
            Vector2 tagPosition = new Vector2(-_gridSize * 0.5f + 15f, _gridSize * 0.5f - 15f);
            Text tag = AddLabel(layer, label, tagPosition, new Vector2(24f, 24f), 19, Color.white);
            Outline outline = tag.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(4, 14, 22, 255);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Texture2D GridTexture(string name, System.Func<int, int, Color32> cellColor)
        {
            int size = GridSide * TexelsPerCell;
            return CreateTexture(name, size, size, (x, y) =>
                x % TexelsPerCell == 0 || y % TexelsPerCell == 0
                    ? TileGap
                    : cellColor(x / TexelsPerCell, y / TexelsPerCell));
        }

        private Texture2D CreateTexture(string name, int width, int height, System.Func<int, int, Color32> colorAt)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    pixels[(height - 1 - row) * width + column] = colorAt(column, row);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _textures.Add(texture);
            return texture;
        }

        private static Color32 ColorAt(int x, int y)
        {
            int variation = (x * 3 + y * 5 + x * y) % WarmTiles.Length;
            return ColorDigit[y][x] == '#' ? WarmTiles[variation] : CoolTiles[variation];
        }

        private static float DigitOneCoverage(Vector2 point)
        {
            Vector2 top = new Vector2(4.2f, 1.2f);
            Vector2 foot = new Vector2(4.2f, 6.6f);
            float distance = Mathf.Min(
                DistanceToStroke(point, new Vector2(2.65f, 2.55f), top),
                Mathf.Min(DistanceToStroke(point, top, foot),
                    DistanceToStroke(point, new Vector2(2.6f, foot.y), new Vector2(5.7f, foot.y))));
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 0.86f, distance));
        }

        private static float DistanceToStroke(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 stroke = end - start;
            float amount = Mathf.Clamp01(Vector2.Dot(point - start, stroke) / stroke.sqrMagnitude);
            return Vector2.Distance(point, start + stroke * amount);
        }

        private static RawImage AddPicture(Transform parent, Texture2D texture, Vector2 center, Vector2 size)
        {
            RectTransform rect = AddRect(parent, texture.name, center, size);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private Text AddLabel(Transform parent, string value, Vector2 center, Vector2 size, int fontSize, Color color)
        {
            RectTransform rect = AddRect(parent, value, center, size);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _labelFont;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform AddRect(Transform parent, string name, Vector2 center, Vector2 size)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
