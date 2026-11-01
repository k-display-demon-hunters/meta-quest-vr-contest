using Core;
using System;
using System.Collections;
using UnityEngine;

namespace CNNScreen
{
    public sealed class RgbPixelStackController : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private Transform[] _layers;
        [SerializeField] private MeshRenderer[] _blocks;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private Transform _sourceBoard;
        private MaterialPropertyBlock _properties;
        private const float Cell = 0.19f;
        public event Action<int, float> ProgressChanged;

        public void Hide() { _content.SetActive(false); }
        public IEnumerator Unfold(PixelPatchData patch, int columns, int rows, Vector2Int selected)
        {
            _content.SetActive(true);
            _properties = new MaterialPropertyBlock();
            Vector3 source = new Vector3(((selected.x + 1.5f) / columns - 0.5f) * _boardSize.x,
                ((selected.y + 1.5f) / rows - 0.5f) * _boardSize.y, -0.13f);
            source = _content.transform.InverseTransformPoint(_sourceBoard.TransformPoint(source));
            float initialScale = _sourceBoard.lossyScale.x / _content.transform.lossyScale.x * _boardSize.x / columns / Cell;
            for (int c = 0; c < 3; c++)
            {
                _layers[c].localPosition = source;
                _layers[c].localScale = Vector3.one * initialScale;
                for (int i = 0; i < 9; i++)
                {
                    Color32 pixel = patch[i];
                    int value = c == 0 ? pixel.r : c == 1 ? pixel.g : pixel.b;
                    Color color = c == 0 ? new Color(value / 255f, 0, 0, 0.9f)
                        : c == 1 ? new Color(0, value / 255f, 0, 0.9f) : new Color(0, 0, value / 255f, 0.9f);
                    _properties.SetColor("_BaseColor", color);
                    _blocks[c * 9 + i].SetPropertyBlock(_properties);
                }
            }
            float elapsed = 0;
            while (elapsed < 2.6f)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int c = 0; c < 3; c++)
                {
                    float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - c * 0.25f) / 2f));
                    Vector3 target = new Vector3((c - 1) * 0.65f, c * 0.08f, c * 0.1f);
                    _layers[c].localPosition = Vector3.Lerp(source, target, progress);
                    _layers[c].localScale = Vector3.one * Mathf.Lerp(initialScale, 1, progress);
                    ProgressChanged?.Invoke(c, Mathf.InverseLerp(0.45f, 1, progress));
                }
                yield return null;
            }
        }
    }
}
