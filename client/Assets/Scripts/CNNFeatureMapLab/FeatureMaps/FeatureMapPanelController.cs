using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class FeatureMapPanelController
    {
        private CorridorRenderUtility _visuals;
        [SerializeField] private Renderer _frame;
        [SerializeField] private Renderer _surface;
        [SerializeField] private Texture2D _texture;
        private Color32[] _pixels;
        [SerializeField] private Transform _border;
        [SerializeField] private Renderer[] _borderBars = new Renderer[4];
        [SerializeField] private TMP_Text _off;
        [SerializeField] private TMP_Text _badge;
        [SerializeField] private Transform _switch;
        [SerializeField] private Renderer _switchTrack;
        [SerializeField] private Transform _knob;
        [SerializeField] private TMP_Text _switchText;
        private float _switchValue = 1;
        private Vector3 _knobOn;
        [SerializeField] private float _switchTravel = .72f;
        [SerializeField] private int _size;
        [SerializeField] private float _width;
        private Transform _hoverBorder;
        private Renderer[] _hoverBars;
        internal string HoverCell
        {
            get;
            private set;
        }
        = "";
        private Color _defaultFrame = new Color32(30, 58, 72, 255);
        [SerializeField] private Transform _root;
        internal Transform Root => _root;
        internal Vector3 Position => Root.localPosition;
        internal float Width => _width;
        internal Texture2D Texture => _texture;
        [SerializeField] private XRSimpleInteractable _hit;
        [SerializeField] private XRSimpleInteractable _switchHit;
        internal void Bind(CorridorRenderUtility visuals, InteractionManager interaction = null, Action<int> clicked = null, Action doubleClicked = null)
        {
            _visuals = visuals;
            _texture = UnityEngine.Object.Instantiate(_texture);
            _pixels = new Color32[_texture.width * _texture.height];
            visuals.Texture(_surface, _texture);
            if (clicked != null)
            {
                _hoverBorder = UnityEngine.Object.Instantiate(_border.gameObject, Root).transform;
                _hoverBorder.name = "Pointer cell outline";
                _hoverBars = _hoverBorder.GetComponentsInChildren<Renderer>(true);
                _hoverBorder.gameObject.SetActive(false);
                interaction.Bind(_hit, point =>
                {
                    int cell = CellAt(point);
                    LogCell(_root.name, cell % _size, cell / _size);
                    clicked(cell);
                }, doubleClicked == null ? (Action<Vector3>)null : _ => doubleClicked(), Hover);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogCell(string name, int column, int row)
        {
            Debug.Log($"[Corridor] Cell {name}: ({column},{row})");
        }

        internal void BindSwitch(InteractionManager interaction, Action toggle)
        {
            _knobOn = _knob.localPosition;
            Vector3 scale = _switchHit.transform.localScale;
            interaction.Bind(_switchHit, _ => toggle(), null, point => _switchHit.transform.localScale = scale*(point.HasValue ? 1.04f : 1f));
        }

        internal void Dispose()
        {
            UnityEngine.Object.Destroy(_texture);
            if (_hoverBorder != null)
            {
                UnityEngine.Object.Destroy(_hoverBorder.gameObject);
            }
        }

        private FeatureMapPanelController()
        {
        }

        private void Hover(Vector3? point)
        {
            _hoverBorder.gameObject.SetActive(point.HasValue);
            if (!point.HasValue)
            {
                HoverCell = "";
                return;
            }
            int cell = CellAt(point.Value);
            HoverCell = $"({cell % _size},{cell / _size})";
            _hoverBorder.localPosition = CellPosition(cell) + Vector3.back * .008f;
            float width = _width / _size;
            for (int i = 0; i < 4; i++)
            {
                _hoverBars[i].transform.localPosition = i < 2 ? new Vector3(0, (i-.5f)*width, 0)
                : new Vector3((i-2.5f)*width, 0, 0);
                _hoverBars[i].transform.localScale = i < 2 ? new Vector3(width, .012f, .012f)
                : new Vector3(.012f, width, .012f);
                _visuals.Paint(_hoverBars[i], Color.white);
            }
        }

        private int CellAt(Vector3 world)
        {
            Vector3 local = Root.InverseTransformPoint(world);
            int column = Mathf.Clamp(Mathf.FloorToInt((local.x/_width+.5f)*_size), 0, _size-1);
            int row = Mathf.Clamp(Mathf.FloorToInt((.5f-local.y/_width)*_size), 0, _size-1);
            return row*_size+column;
        }

        internal Vector3 CellPosition(int index)
        {
            return new Vector3(((index%_size+.5f)/_size-.5f)*_width, (.5f-(index/_size+.5f)/_size)*_width, -.03f);
        }

        internal void Highlight(int firstRow, int firstColumn, int lastRow, int lastColumn, Color color)
        {
            _border.gameObject.SetActive(true);
            int top = Mathf.Clamp(firstRow, 0, _size-1);
            int left = Mathf.Clamp(firstColumn, 0, _size-1);
            int bottom = Mathf.Clamp(lastRow, 0, _size-1);
            int right = Mathf.Clamp(lastColumn, 0, _size-1);
            Vector3 center = (CellPosition(top*_size+left)+CellPosition(bottom*_size+right))*.5f;
            _border.localPosition = center;
            float w = (right-left+1)*_width/_size;
            float h = (bottom-top+1)*_width/_size;
            const float line = .018f;
            for (int i = 0; i < _borderBars.Length; i++)
            {
                Transform stroke = _borderBars[i].transform;
                stroke.localPosition = i < 2 ? new Vector3(0, (i-.5f)*h, 0) : new Vector3((i-2.5f)*w, 0, 0);
                stroke.localScale = i < 2 ? new Vector3(w+line, line, .012f) : new Vector3(line, h, .012f);
                _visuals.Paint(_borderBars[i], color);
            }
        }

        internal void Cell(int index, Color color) => Highlight(index/_size, index%_size, index/_size, index%_size, color);
        internal void ClearHighlight() => _border.gameObject.SetActive(false);
        internal void Frame(Color? color = null) => _visuals.Paint(_frame, color ?? _defaultFrame);
        internal void Opacity(float alpha) => _visuals.Paint(_surface, new Color(1, 1, 1, alpha));
        internal void Badge(string caption, Color color)
        {
            _badge.gameObject.SetActive(caption.Length != 0);
            _badge.text = caption;
            _badge.color = color;
        }

        internal void UpdateMap(IReadOnlyList<float> values, float scale, bool off, int shown = int.MaxValue)
        {
            for (int y = 0; y < _size; y++)
            for (int x = 0; x < _size; x++)
            {
                int index = y*_size+x;
                _pixels[(_size-1-y)*_size+x] = off
                ? ((x+y)%2 == 0 ? new Color32(26, 20, 22, 255) : new Color32(31, 23, 26, 255))
                : index < shown ? CorridorTheme.MapColor(values[index]/scale) : new Color32(10, 18, 32, 255);
            }
            Upload();
            _off.gameObject.SetActive(off);
            if (_switch != null)
            {
                _switchText.text = off ? FeatureMapLabels.Off : FeatureMapLabels.On;
            }
        }

        internal void DrawInput(IReadOnlyList<IReadOnlyList<float>> channels, int channel)
        {
            int width = _texture.width;
            int block = width/_size;
            Color32 background = channel < 0 ? new Color32(233, 228, 216, 255) : new Color32(20, 24, 31, 255);
            for (int y = 0; y < width; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (_size-1-y/block)*_size+x/block;
                float dx = (x%block+.5f)/block-.5f;
                float dy = (y%block+.5f)/block-.5f;
                Color32 color;
                if (channel < 0)
                {
                    color = new Color(channels[0][i], channels[1][i], channels[2][i]);
                }
                else
                {
                    Color tone = channel == 0 ? new Color32(255, 70, 70, 255) : channel == 1
                    ? new Color32(70, 225, 95, 255) : new Color32(80, 130, 255, 255);
                    float intensity = channels[channel][i];
                    color = new Color(tone.r*intensity, tone.g*intensity, tone.b*intensity);
                }
                _pixels[y*width+x] = dx*dx+dy*dy <= .44f*.44f ? color : background;
            }
            Upload();
        }

        private void Upload()
        {
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        internal void ShowSwitch(bool visible) => _switch.gameObject.SetActive(visible);
        internal Transform Switch => _switch;
        internal void AnimateSwitch(float delta, bool enabled)
        {
            if (_switch == null || !_switch.gameObject.activeSelf)
            {
                return;
            }
            float next = Mathf.MoveTowards(_switchValue, enabled ? 1 : 0, delta*5);
            if (next == _switchValue)
            {
                return;
            }
            _switchValue = next;
            _knob.localPosition = _knobOn + Vector3.left * (_switchTravel * (1 - next));
            Color color = Color.Lerp(_visuals.Theme.Negative, _visuals.Theme.Cyan, next);
            _visuals.Paint(_switchTrack, color);
            _switchText.color = color;
        }
    }
}
