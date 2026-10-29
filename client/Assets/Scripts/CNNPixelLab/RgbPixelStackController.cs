using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Spatially separates the three components of the same selected pixels.
    // Layer depth is an illustration of RGB structure, not a physical property of a display.
    public sealed class RgbPixelStackController : MonoBehaviour
    {
        private readonly Transform[] _layers = new Transform[3];
        private readonly MeshRenderer[,] _blocks = new MeshRenderer[3, 9];
        private readonly Text[,] _values = new Text[3, 9];
        private readonly CanvasGroup[] _labels = new CanvasGroup[3];
        private MaterialPropertyBlock _properties;
        private Vector2 _boardSize;
        private const float Cell = 0.19f;

        public void Initialize(Font font, Vector2 boardSize, Mesh mesh, Material material)
        {
            _properties = new MaterialPropertyBlock();
            _boardSize = boardSize;
            string[] names = { "R · 빨강", "G · 초록", "B · 파랑" };
            for (int c = 0; c < 3; c++)
            {
                Transform layer = new GameObject(names[c]).transform;
                layer.SetParent(transform, false);
                _layers[c] = layer;
                GameObject canvasObject = new GameObject("Pixel values", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                canvasObject.transform.SetParent(layer, false);
                canvasObject.transform.localPosition = Vector3.back * 0.035f;
                canvasObject.transform.localScale = Vector3.one * 0.001f;
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
                _labels[c] = canvasObject.GetComponent<CanvasGroup>();
                _labels[c].blocksRaycasts = false;
                Label(canvasObject.transform, font, names[c], new Vector2(0, 345), 220, 45, 30);
                for (int i = 0; i < 9; i++)
                {
                    Vector3 position = new Vector3((i % 3 - 1) * Cell, (1 - i / 3) * Cell, 0);
                    _blocks[c, i] = PixelVolumeMesh.Add(layer, "Pixel " + i, mesh, material,
                        position, new Vector3(Cell * 0.94f, Cell * 0.94f, 0.055f));
                    _values[c, i] = Label(canvasObject.transform, font, "", new Vector2(position.x, position.y) * 1000,
                        175, 130, 48);
                }
            }
        }

        public IEnumerator Unfold(IReadOnlyList<Color32> pixels, int columns, int rows, Vector2Int selected)
        {
            gameObject.SetActive(true);
            Vector3 source = new Vector3(((selected.x + 1.5f) / columns - 0.5f) * _boardSize.x,
                ((selected.y + 1.5f) / rows - 0.5f) * _boardSize.y, -0.13f);
            float initialScale = _boardSize.x / columns / Cell;
            for (int c = 0; c < 3; c++)
            {
                _labels[c].alpha = 0;
                for (int i = 0; i < 9; i++)
                {
                    Color32 pixel = pixels[(selected.y + 2 - i / 3) * columns + selected.x + i % 3];
                    int value = c == 0 ? pixel.r : c == 1 ? pixel.g : pixel.b;
                    Color color = c == 0 ? new Color(value / 255f, 0, 0, 0.9f)
                        : c == 1 ? new Color(0, value / 255f, 0, 0.9f) : new Color(0, 0, value / 255f, 0.9f);
                    _properties.SetColor("_BaseColor", color);
                    _blocks[c, i].SetPropertyBlock(_properties);
                    _values[c, i].text = value.ToString();
                    _values[c, i].color = c == 1 && value > 175 ? Color.black : Color.white;
                }
            }
            float elapsed = 0;
            const float duration = 2.6f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int c = 0; c < 3; c++)
                {
                    float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - c * 0.25f) / 2f));
                    // A slight diagonal offset exposes the edges while preserving a stacked volume.
                    Vector3 target = new Vector3((c - 1) * 0.14f, (c - 1) * 0.07f, -0.42f - c * 0.22f);
                    _layers[c].localPosition = Vector3.Lerp(source, target, progress);
                    _layers[c].localScale = Vector3.one * Mathf.Lerp(initialScale, 1, progress);
                    _labels[c].alpha = Mathf.InverseLerp(0.45f, 1, progress);
                }
                yield return null;
            }
        }

        private static Text Label(Transform parent, Font font, string caption, Vector2 position, float width, float height, int fontSize)
        {
            GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            Text text = obj.GetComponent<Text>();
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            text.font = font;
            text.fontSize = fontSize;
            text.text = caption;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
