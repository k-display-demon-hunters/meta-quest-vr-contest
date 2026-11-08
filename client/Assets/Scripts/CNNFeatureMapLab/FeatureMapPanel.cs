using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class FeatureMapPanel
    {
        private CorridorVisuals _visuals;
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
        private Color _defaultFrame = new Color32(30,58,72,255);
        [SerializeField] private Transform _root;
        internal Transform Root => _root;
        internal Vector3 Position => Root.localPosition;
        internal float Width => _width;
        internal Texture2D Texture => _texture;

        [SerializeField] private BoxCollider _hit;
        [SerializeField] private BoxCollider _switchHit;

        internal void Bind(CorridorVisuals visuals, Action<int> clicked = null)
        {
            _visuals = visuals;
            _texture = visuals.Own(UnityEngine.Object.Instantiate(_texture));
            _pixels = new Color32[_texture.width * _texture.height];
            visuals.Texture(_surface, _texture);
            if (clicked != null) { visuals.Listen(_hit, point => clicked(CellAt(point))); }
        }

        internal void BindSwitch(Action toggle)
        {
            _knobOn = _knob.localPosition;
            _visuals.Listen(_switchHit, _ => toggle());
        }

        private FeatureMapPanel() { }

#if UNITY_EDITOR
        internal FeatureMapPanel(CorridorVisuals visuals, Transform parent, Vector3 position, float yaw, int size,
            string label, string source, Action<int> clicked, int textureScale = 1, float width = 0)
        {
            _visuals = visuals;
            _size = size;
            _width = width > 0 ? width : visuals.Theme.PanelSize;
            _root = CorridorVisuals.Group(parent,label.Length == 0 ? "Feature map panel" : label,position);
            Root.localRotation = Quaternion.Euler(0,yaw,0);
            _frame = visuals.Shape(Root,"Frame",new Vector3(0,0,.047f),new Vector3(_width+.14f,_width+.14f,.08f),_defaultFrame);
            _surface = visuals.Shape(Root,"Pixels",Vector3.zero,new Vector3(_width,_width,1),Color.white,
                PrimitiveType.Quad,transparent:true);
            _texture = visuals.Pixels(size*textureScale);
            _pixels = new Color32[_texture.width*_texture.height];
            visuals.Texture(_surface,_texture);
            if (clicked != null)
            {
                BoxCollider hit = Root.gameObject.AddComponent<BoxCollider>();
                _hit = hit;
                hit.size = new Vector3(_width,_width,.06f);
                CorridorVisuals.Click(hit,point => clicked(CellAt(point)));
            }

            _border = CorridorVisuals.Group(Root,"Cell outline",new Vector3(0,0,-.028f));
            for (int i = 0; i < _borderBars.Length; i++)
                _borderBars[i] = visuals.Shape(_border,"Stroke",Vector3.zero,Vector3.one,Color.white);
            _border.gameObject.SetActive(false);
            _off = visuals.Label(Root,"OFF",new Vector3(0,0,-.055f),.38f,_width,visuals.Theme.Negative,mono:true);
            _off.gameObject.SetActive(false);
            _badge = visuals.Label(Root,"",new Vector3(0,-_width*.5f-.22f,-.03f),.18f,_width+1,visuals.Theme.Dim);
            _badge.gameObject.SetActive(false);

            if (label.Length > 0)
                visuals.Label(Root,label,new Vector3(0,_width*.5f+.41f,0),.19f,_width+2,visuals.Theme.Ink);
            if (source.Length > 0)
                visuals.Label(Root,source,new Vector3(0,_width*.5f+.18f,0),.126f,_width+2,new Color32(127,147,168,255));
        }
#endif

        private int CellAt(Vector3 world)
        {
            Vector3 local = Root.InverseTransformPoint(world);
            int column = Mathf.Clamp(Mathf.FloorToInt((local.x/_width+.5f)*_size),0,_size-1);
            int row = Mathf.Clamp(Mathf.FloorToInt((.5f-local.y/_width)*_size),0,_size-1);
            return row*_size+column;
        }

        internal Vector3 CellPosition(int index)
        {
            return new Vector3(((index%_size+.5f)/_size-.5f)*_width,
                (.5f-(index/_size+.5f)/_size)*_width,-.03f);
        }

        internal void Highlight(int firstRow, int firstColumn, int lastRow, int lastColumn, Color color)
        {
            _border.gameObject.SetActive(true);
            int top = Mathf.Clamp(firstRow,0,_size-1);
            int left = Mathf.Clamp(firstColumn,0,_size-1);
            int bottom = Mathf.Clamp(lastRow,0,_size-1);
            int right = Mathf.Clamp(lastColumn,0,_size-1);
            Vector3 center = (CellPosition(top*_size+left)+CellPosition(bottom*_size+right))*.5f;
            _border.localPosition = center;
            float w = (right-left+1)*_width/_size;
            float h = (bottom-top+1)*_width/_size;
            const float line = .018f;
            for (int i = 0; i < _borderBars.Length; i++)
            {
                Transform stroke = _borderBars[i].transform;
                stroke.localPosition = i < 2 ? new Vector3(0,(i-.5f)*h,0) : new Vector3((i-2.5f)*w,0,0);
                stroke.localScale = i < 2 ? new Vector3(w+line,line,.012f) : new Vector3(line,h,.012f);
                _visuals.Paint(_borderBars[i],color);
            }
        }

        internal void Cell(int index, Color color) => Highlight(index/_size,index%_size,index/_size,index%_size,color);
        internal void ClearHighlight() => _border.gameObject.SetActive(false);
        internal void Frame(Color? color = null) => _visuals.Paint(_frame,color ?? _defaultFrame);
        internal void Opacity(float alpha) => _visuals.Paint(_surface,new Color(1,1,1,alpha));

        internal void Badge(string caption, Color color)
        {
            _badge.gameObject.SetActive(caption.Length != 0);
            _badge.text = caption;
            _badge.color = color;
        }

        internal void UpdateMap(float[] values, float scale, bool off, int shown = int.MaxValue)
        {
            for (int y = 0; y < _size; y++)
            for (int x = 0; x < _size; x++)
            {
                int index = y*_size+x;
                _pixels[(_size-1-y)*_size+x] = off
                    ? ((x+y)%2 == 0 ? new Color32(26,20,22,255) : new Color32(31,23,26,255))
                    : index < shown ? CorridorTheme.MapColor(values[index]/scale) : new Color32(10,18,32,255);
            }
            Upload();
            _off.gameObject.SetActive(off);
            if (_switch != null) { _switchText.text = off ? CorridorDialogue.Off : CorridorDialogue.On; }
        }

        internal void DrawInput(float[][] channels, int channel)
        {
            int width = _texture.width;
            int block = width/_size;
            Color32 background = channel < 0 ? new Color32(233,228,216,255) : new Color32(20,24,31,255);
            for (int y = 0; y < width; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (_size-1-y/block)*_size+x/block;
                float dx = (x%block+.5f)/block-.5f;
                float dy = (y%block+.5f)/block-.5f;
                Color32 color;
                if (channel < 0) { color = new Color(channels[0][i],channels[1][i],channels[2][i]); }
                else
                {
                    Color tone = channel == 0 ? new Color32(255,70,70,255) : channel == 1
                        ? new Color32(70,225,95,255) : new Color32(80,130,255,255);
                    float intensity = channels[channel][i];
                    color = new Color(tone.r*intensity,tone.g*intensity,tone.b*intensity);
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

#if UNITY_EDITOR
        internal void AddSwitch(Action toggle)
        {
            _switch = CorridorVisuals.Group(Root,"Map switch",new Vector3(0,-_width*.5f-.34f,0));
            _switchTrack = _visuals.Shape(_switch,"Track",Vector3.zero,new Vector3(1.2f,.45f,.09f),
                _visuals.Theme.Cyan,PrimitiveType.Sphere);
            _knob = _visuals.Shape(_switch,"Handle",new Vector3(.36f,0,-.07f),new Vector3(.31f,.31f,.13f),
                Color.white,PrimitiveType.Sphere).transform;
            _switchText = _visuals.Label(_switch,CorridorDialogue.On,new Vector3(.9f,0,-.04f),.16f,.62f,_visuals.Theme.Cyan);
            var hit = _switch.gameObject.AddComponent<BoxCollider>();
            _switchHit = hit;
            hit.size = new Vector3(1.25f,.47f,.16f);
            CorridorVisuals.Click(hit,_ => toggle());
            ShowSwitch(false);
        }
#endif

        internal void ShowSwitch(bool visible) => _switch.gameObject.SetActive(visible);
        internal Transform Switch => _switch;

        internal void AnimateSwitch(float delta, bool enabled)
        {
            if (_switch == null || !_switch.gameObject.activeSelf) { return; }
            float next = Mathf.MoveTowards(_switchValue,enabled ? 1 : 0,delta*5);
            if (next == _switchValue) { return; }
            _switchValue = next;
            _knob.localPosition = _knobOn + Vector3.left * (_switchTravel * (1 - next));
            Color color = Color.Lerp(_visuals.Theme.Negative,_visuals.Theme.Cyan,next);
            _visuals.Paint(_switchTrack,color);
            _switchText.color = color;
        }
    }
}
