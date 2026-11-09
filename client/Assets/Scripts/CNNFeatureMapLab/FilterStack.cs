using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class FilterStack
    {
        private const int Capacity = 5;
        private CorridorVisuals _visuals;
        private ICnnModel _model;
        [SerializeField] private Transform _root;
        [SerializeField] private Renderer[] _frames = new Renderer[Capacity];
        [SerializeField] private Transform[] _card = new Transform[Capacity];
        [SerializeField] private Transform[] _weights = new Transform[Capacity];
        [SerializeField] private TMP_Text[] _numbers = new TMP_Text[Capacity*9];
        [SerializeField] private TMP_Text[] _badges = new TMP_Text[Capacity];
        [SerializeField] private FeatureMapPanel[] _templates = new FeatureMapPanel[Capacity];
        [SerializeField] private LineRenderer[] _links = new LineRenderer[Capacity];
        [SerializeField] private LineRenderer _resultLink;
        [SerializeField] private TMP_Text _cover;
        [SerializeField] private TMP_Text _title;
        private Vector3[] _starts = new Vector3[Capacity];
        private FeatureMapPanel[] _sources;
        private FeatureMapPanel _result;
        private Color32[] _colors;
        private int _count;
        private int _layer;
        private float _pressed = float.NegativeInfinity;
        private float _motionStart;
        private bool _expanded;
        private bool _animating;
        private bool _connected;

        internal event Action<bool> ExpansionChanged;
        internal Transform Root => _root;
        internal Transform Plate(int index) => _card[index];
        internal Texture2D TemplateTexture(int index) => _templates[index].Texture;
        internal static string WeightText(float weight) => Mathf.Abs(weight) < .005f ? "0.00"
            : (weight < 0 ? "−" : "+")+Mathf.Abs(weight).ToString("0.00",CultureInfo.InvariantCulture);
        internal Color32 WeightColor(float weight) => Mathf.Abs(weight) < .005f ? new Color32(107,122,138,255)
            : weight > 0 ? _visuals.Theme.Cyan : _visuals.Theme.Negative;

        [SerializeField] private Collider[] _cardHits = new Collider[Capacity];
        [SerializeField] private float _spreadSpacing = .95f;
        private Vector3[] _foldedPositions = new Vector3[Capacity];

        internal void Bind(CorridorVisuals visuals, ICnnModel model)
        {
            _visuals = visuals;
            _model = model;
            for (int i = 0; i < Capacity; i++)
            {
                _foldedPositions[i] = _card[i].localPosition;
                _templates[i].Bind(visuals);
                visuals.Listen(_cardHits[i], _ => Expand(), _ => SetExpanded(false,false));
            }
            Hide();
        }

        private FilterStack() { }

#if UNITY_EDITOR
        internal FilterStack(CorridorVisuals visuals, Transform parent, ICnnModel model)
        {
            _visuals = visuals;
            _model = model;
            _root = CorridorVisuals.Group(parent,"Filter stack",Vector3.zero);
            visuals.FaceCamera(_root);
            _cover = visuals.Label(_root,CorridorDialogue.Filter,new Vector3(0,0,-.08f),.29f,.8f,visuals.Theme.Amber,false);
            _title = visuals.Label(_root,"",new Vector3(0,.67f,0),.21f,3,visuals.Theme.Ink,false);
            for (int i = 0; i < Capacity; i++)
            {
                _card[i] = CorridorVisuals.Group(_root,"Filter card",Vector3.zero);
                _frames[i] = visuals.Shape(_card[i],"Card frame",Vector3.zero,new Vector3(.8f,.8f,.042f),visuals.Theme.Amber);
                var face = visuals.Shape(_card[i],"Card face",new Vector3(0,0,-.027f),new Vector3(.76f,.76f,.023f),
                    new Color32(22,32,46,255),collider:true);
                _cardHits[i] = face.GetComponent<Collider>();
                _foldedPositions[i] = new Vector3(i*.07f,i*.07f,i*.04f);
                _card[i].localPosition = _foldedPositions[i];
                CorridorVisuals.Click(face.GetComponent<Collider>(),_ => Press());
                _weights[i] = CorridorVisuals.Group(_card[i],"Kernel weights",Vector3.zero);
                for (int n = 0; n < 9; n++)
                {
                    Vector3 position = new Vector3((n%3-1)*.249f,(1-n/3)*.249f,-.049f);
                    _numbers[(i)*9+n] = visuals.Label(_weights[i],"",position,.117f,.245f,visuals.Theme.Cyan,false,true);
                }
                for (int j = -1; j <= 1; j += 2)
                {
                    Color line = new Color32(58,74,90,255);
                    visuals.Shape(_weights[i],"Grid",new Vector3(j*.125f,0,-.043f),new Vector3(.008f,.748f,.005f),line);
                    visuals.Shape(_weights[i],"Grid",new Vector3(0,j*.125f,-.043f),new Vector3(.748f,.008f,.005f),line);
                }
                _badges[i] = visuals.Label(_card[i],"",new Vector3(0,-.55f,-.06f),.14f,.8f,visuals.Theme.Ink,false);
                _templates[i] = new FeatureMapPanel(visuals,_card[i],new Vector3(0,0,-.062f),0,model.Size,"","",null,width:.7f);
                _links[i] = CreateLine(parent,"Filter source");
            }
            _resultLink = CreateLine(parent,"Filter result");
            Hide();
        }
#endif

#if UNITY_EDITOR
        private LineRenderer CreateLine(Transform parent, string name)
        {
            var line = CorridorVisuals.Group(parent,name,Vector3.zero).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            _visuals.Line(line,_visuals.Theme.Amber,.028f);
            return line;
        }
#endif

        internal void Configure(int layer, int map, int digit, FeatureMapPanel[] sources, FeatureMapPanel result, Vector3 destination)
        {
            _layer = layer;
            _sources = sources;
            _result = result;
            _colors = layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs;
            _count = sources.Length;
            float sourceZ = 0;
            for (int i = 0; i < _count; i++)
            {
                sourceZ += sources[i].Position.z;
                _card[i].gameObject.SetActive(true);
                _card[i].localPosition = Folded(i);
                _badges[i].color = _colors[i];
                _badges[i].text = CorridorDialogue.Marks[i]+" "+(layer == 1 ? CorridorDialogue.Channels[i] : $"{layer-1}-{i+1}");
                if (layer == 4)
                {
                    _templates[i].Frame(_colors[i]);
                    _templates[i].UpdateMap(_model.OutputTemplate(digit,i),1,false);
                    continue;
                }
                float[,] kernel = _model.Kernel(layer,map,i);
                for (int n = 0; n < 9; n++)
                {
                    float value = kernel[n/3,n%3];
                    TMP_Text number = _numbers[(i)*9+n];
                    number.text = WeightText(value);
                    number.color = WeightColor(value);
                }
            }
            for (int i = _count; i < Capacity; i++) { _card[i].gameObject.SetActive(false); }
            float sourceDepth = layer == 1 ? -7.75f : sourceZ/_count;
            _root.localPosition = new Vector3(destination.x*.3f,layer == 4 ? 1.6f : destination.y+1.7f,
                (destination.z+sourceDepth)*.5f);
            _title.text = CorridorDialogue.FilterTitle(layer,_count,layer == 4 ? _model.Size : 3);
            _root.gameObject.SetActive(true);
            _pressed = float.NegativeInfinity;
            _connected = true;
            SetExpanded(false,true);
        }

        private void Expand()
        {
            if (_expanded) { return; }
            SetExpanded(true,false);
            ExpansionChanged?.Invoke(true);
        }

        private void Press()
        {
            float now = Time.unscaledTime;
            if (!_expanded)
            {
                _pressed = now;
                SetExpanded(true,false);
                ExpansionChanged?.Invoke(true);
            }
            else if (now-_pressed <= .4f)
            {
                _pressed = float.NegativeInfinity;
                SetExpanded(false,false);
                ExpansionChanged?.Invoke(false);
            }
            else { _pressed = now; }
        }

        internal void SetExpanded(bool expanded, bool instant)
        {
            _expanded = expanded;
            _motionStart = Time.unscaledTime;
            _animating = !instant;
            _cover.gameObject.SetActive(!expanded);
            _title.gameObject.SetActive(expanded);
            for (int i = 0; i < _count; i++)
            {
                _starts[i] = _card[i].localPosition;
                _weights[i].gameObject.SetActive(expanded && _layer != 4);
                _templates[i].Root.gameObject.SetActive(expanded && _layer == 4);
                _badges[i].gameObject.SetActive(expanded);
                _visuals.Paint(_frames[i],expanded ? _colors[i] : i == 0 ? _visuals.Theme.Amber : new Color32(58,74,90,255));
                if (instant) { _card[i].localPosition = expanded ? Spread(i) : Folded(i); }
            }
            SetLinks(expanded && _connected);
        }

        private Vector3 Folded(int i) => _foldedPositions[i];
        private Vector3 Spread(int i) => Vector3.right*(i-(_count-1)*.5f)*_spreadSpacing;

        private void SetLinks(bool enabled)
        {
            for (int i = 0; i < Capacity; i++) { _links[i].enabled = enabled && i < _count; }
            _resultLink.enabled = enabled && _result != null;
        }

        internal void ShowCards(bool visible) => _root.gameObject.SetActive(visible);
        internal void DimDuringScan(bool dim)
        {
            _connected = !dim;
            SetLinks(!dim && _expanded);
            if (!_expanded) { return; }
            for (int i = 0; i < _count; i++)
            {
                // Use the same physical card at reduced brightness while its plate is in flight.
                _visuals.Paint(_frames[i],(Color)_colors[i]*(dim ? .25f : 1));
                if (_layer == 4) { _templates[i].Opacity(dim ? .25f : 1); }
                else for (int n = 0; n < 9; n++) { _numbers[(i)*9+n].alpha = dim ? .25f : 1; }
            }
        }

        internal void Hide()
        {
            _root.gameObject.SetActive(false);
            SetLinks(false);
        }

        internal void Tick(float now)
        {
            if (!_root.gameObject.activeSelf) { return; }
            if (_animating)
            {
                float t = Mathf.Clamp01((now-_motionStart)/.5f);
                for (int i = 0; i < _count; i++)
                    _card[i].localPosition = Vector3.Lerp(_starts[i],_expanded ? Spread(i) : Folded(i),Mathf.SmoothStep(0,1,t));
                _animating = t < 1;
            }
            if (!_expanded || !_connected) { return; }
            for (int i = 0; i < _count; i++)
            {
                _visuals.Paint(_links[i],_colors[i]);
                _links[i].SetPosition(0,_sources[i].Root.position);
                _links[i].SetPosition(1,_card[i].position);
            }
            if (_result == null) { return; }
            _resultLink.SetPosition(0,_root.position);
            _resultLink.SetPosition(1,_result.Root.position);
        }
    }
}
