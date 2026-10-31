using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public sealed class SelectedPixelsController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer[] _pixels;
        private IReadOnlyList<Color32> _source;
        private int _columns;
        private MaterialPropertyBlock _properties;

        public void Initialize(IReadOnlyList<Color32> pixels, int columns)
        {
            _source = pixels;
            _columns = columns;
            _properties = new MaterialPropertyBlock();
        }
        public void SetVisible(bool visible) { gameObject.SetActive(visible); }
        public void SetOrigin(Vector2Int origin)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                Color color = _source[(origin.y + i / 3) * _columns + origin.x + i % 3];
                color.a = 0.94f;
                _properties.SetColor("_BaseColor", color);
                _pixels[i].SetPropertyBlock(_properties);
            }
        }
    }
}
