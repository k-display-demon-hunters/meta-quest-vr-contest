using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class RoundPlayer
    {
        private CorridorVisuals _visuals;
        private ICnnModel _model;
        private InputPanel _input;
        private FeatureMapPanel[][] _maps;
        private OutputStage _output;
        [SerializeField] private FilterStack _filter;
        private FeatureMapPanel[] _rgb;
        [SerializeField] private FeatureMapPanel[] _images;
        [SerializeField] private Renderer[] _scanWindows;
        [SerializeField] private TMPro.TMP_Text[] _scanWeights = new TMPro.TMP_Text[5*9];
        [SerializeField] private Texture2D _plain;
        private Vector3[] _mergePositions = new Vector3[5];
        private float[] _scales = new float[5];
        private int _size;
        [SerializeField] private Transform _root;
        private float[][] _results;
        private float[] _selectedTerms;
        private FeatureMapPanel[] _sources;
        private FeatureMapPanel _target;
        private Vector3 _destination;
        private LayerOutput _outputCell;
        private int _lastScan;
        private float _started;
        private int _count;
        private int _digit;
        internal int Layer { get; private set; }
        internal int Map { get; private set; }
        internal int Index { get; private set; }
        internal int Stage { get; private set; }
        internal bool IsOutput => Layer == 4;
        internal int LastStage => IsOutput ? 8 : 5;
        internal Vector3 FilterPosition => _filter.Root.localPosition;
        internal float ButtonReadyAt { get; private set; }
        internal event Action<bool> FilterPressed;


        internal void Bind(CorridorVisuals visuals, ICnnModel model, InputPanel input, FeatureMapPanel[][] maps, OutputStage output)
        {
            _visuals = visuals;
            _model = model;
            _input = input;
            _maps = maps;
            _output = output;
            _size = model.Size;
            _rgb = new[] { input.Channel(0), input.Channel(1), input.Channel(2) };
            _filter.Bind(visuals, model);
            foreach (FeatureMapPanel image in _images) { image.Bind(visuals); }
        }

        private void OnFilterExpansionChanged(bool expanded) => FilterPressed?.Invoke(expanded);
        internal void EnableInput() => _filter.ExpansionChanged += OnFilterExpansionChanged;
        internal void DisableInput() => _filter.ExpansionChanged -= OnFilterExpansionChanged;

        private RoundPlayer() { }

#if UNITY_EDITOR
        internal RoundPlayer(CorridorVisuals visuals, Transform root, ICnnModel model, InputPanel input,
            FeatureMapPanel[][] maps, OutputStage output)
        {
            _visuals = visuals;
            _root = root;
            _model = model;
            _input = input;
            _maps = maps;
            _output = output;
            _size = model.Size;
            _rgb = new[] { input.Channel(0),input.Channel(1),input.Channel(2) };
            _filter = new FilterStack(visuals,root,model);
            _filter.ExpansionChanged += expanded => FilterPressed?.Invoke(expanded);
            _plain = visuals.Pixels(1);
            _plain.SetPixel(0,0,Color.white);
            _plain.Apply();
            _images = new FeatureMapPanel[5];
            _scanWindows = new Renderer[5];
            for (int i = 0; i < _images.Length; i++)
            {
                _images[i] = new FeatureMapPanel(visuals,root,Vector3.zero,0,_size,"","",null);
                _images[i].Root.gameObject.SetActive(false);
                _scanWindows[i] = visuals.Shape(root,"Moving filter plate",Vector3.zero,Vector3.one,
                    Color.white,PrimitiveType.Quad,transparent:true);
                for (int n = 0; n < 9; n++)
                    _scanWeights[(i)*9+n] = visuals.Label(_scanWindows[i].transform,"",
                        new Vector3((n%3-1)/3f,(1-n/3)/3f,-.016f),.11f,.32f,Color.white,false,true);
                _scanWindows[i].gameObject.SetActive(false);
            }
        }
#endif

        internal void Start(int layer, int map, int digit, int index, float[][] input, LayerOutput[][] forward)
        {
            Layer = layer;
            Map = map;
            Index = index;
            _digit = digit;
            _sources = layer == 1 ? _rgb : _maps[layer-2];
            _target = IsOutput ? null : _maps[layer-1][map];
            _destination = IsOutput ? _output.Tin(digit)+Vector3.up*.4f : _target.Position;
            _count = _sources.Length;
            _results = new float[_count][];
            _selectedTerms = new float[_count];
            float outputDenominator = 0;
            if (IsOutput)
            {
                for (int i = 0; i < _count; i++)
                foreach (float value in _model.OutputTemplate(digit,i))
                    outputDenominator += value*value;
            }
            else { _outputCell = forward[layer-1][map]; }
            for (int i = 0; i < _count; i++)
            {
                float[] source = layer == 1 ? input[i] : forward[layer-2][i].act;
                var values = new float[_size*_size];
                _results[i] = values;
                if (IsOutput)
                {
                    float[] template = _model.OutputTemplate(digit,i);
                    float scale = _model.Scale(3,i)*outputDenominator;
                    for (int j = 0; j < values.Length; j++) { values[j] = source[j]*template[j]/scale; }
                    _scales[i] = 1/Mathf.Max(.001f,outputDenominator);
                }
                else
                {
                    float[,] kernel = _model.Kernel(layer,map,i);
                    float max = .001f;
                    for (int j = 0; j < values.Length; j++)
                    {
                        values[j] = CorridorMath.Contribution(source,kernel,_size,j);
                        max = Mathf.Max(max,Mathf.Abs(values[j]));
                    }
                    _scales[i] = max;
                    _selectedTerms[i] = values[index];
                }
            }
            _filter.Configure(layer,map,digit,_sources,_target,_destination);
            Show(0);
        }

        internal void Show(int stage, bool filterInput = false)
        {
            Stage = stage;
            _started = Time.unscaledTime;
            ButtonReadyAt = stage == 1 && filterInput ? _started+.7f : _started;
            if (IsOutput && stage <= 6) { _output.Reset(); }
            _filter.ShowCards(stage <= 4);
            if (!filterInput) { _filter.SetExpanded(stage > 0,true); }
            _filter.DimDuringScan(stage == 2);
            _input.MarkChannels(Layer == 1 && stage >= 1);
            if (Layer == 1 && stage >= 1) { _input.SetExpanded(true); }
            if (_target != null)
            {
                _target.Frame(_visuals.Theme.Amber);
                _target.Cell(Index,_visuals.Theme.Yellow);
            }

            Color32[] colors = Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs;
            _lastScan = -1;
            for (int i = 0; i < _count; i++)
            {
                FeatureMapPanel source = _sources[i];
                source.Frame(colors[i]);
                if (Layer != 1)
                    source.Badge(CorridorDialogue.Marks[i]+" "+CorridorDialogue.MapId(Layer-1,i),colors[i]);
                if (!IsOutput)
                {
                    int row = Index/_size;
                    int col = Index%_size;
                    source.Highlight(row-1,col-1,row+1,col+1,Color.white);
                }
                FeatureMapPanel image = _images[i];
                image.Root.gameObject.SetActive(stage >= 2 && stage <= 5);
                _scanWindows[i].gameObject.SetActive(stage == 2);
                if (stage == 2) { SetScanPlate(i); }
                if (stage < 2 || stage > 5) { continue; }
                PositionImage(i);
                _mergePositions[i] = image.Position;
                image.Frame(colors[i]);
                image.Opacity(1);
                image.ClearHighlight();
                image.UpdateMap(_results[i],_scales[i],false,stage == 2 ? 0 : int.MaxValue);
                if (stage >= 4 && !IsOutput) { image.Cell(Index,_visuals.Theme.Yellow); }
                Color color = colors[i];
                color.a = .5f;
                _visuals.Paint(_scanWindows[i],color);
            }
            if (stage == 6) { _output.StartMatch(); }
            if (stage == 7) { _output.FinishMatch(); _output.StartFill(true); }
            if (stage == 8) { _output.FinishFill(); _output.Fly(true); }
        }

        private void SetScanPlate(int src)
        {
            _visuals.Texture(_scanWindows[src],IsOutput ? _filter.TemplateTexture(src) : _plain);
            float[,] weights = IsOutput ? null : _model.Kernel(Layer,Map,src);
            for (int n = 0; n < 9; n++)
            {
                TMPro.TMP_Text label = _scanWeights[(src)*9+n];
                label.gameObject.SetActive(!IsOutput);
                if (IsOutput) { continue; }
                float weight = weights[n/3,n%3];
                label.text = FilterStack.WeightText(weight);
                label.color = _filter.WeightColor(weight);
            }
        }

        private void PositionImage(int index)
        {
            FeatureMapPanel source = _sources[index];
            float gap = Layer == 1 ? .12f : .85f;
            _images[index].Root.localPosition = source.Position+Vector3.up*(source.Width+gap);
            _images[index].Root.localRotation = source.Root.localRotation;
            _images[index].Root.localScale = Vector3.one;
        }

        internal void Tick(float now)
        {
            _filter.Tick(now);
            float elapsed = now-_started;
            if (Stage == 2) { Scan(elapsed); }
            if (Stage == 5) { Merge(elapsed); }
        }

        private void Scan(float elapsed)
        {
            float duration = IsOutput ? 3.5f : 10;
            int shown = Mathf.Min(_size*_size,Mathf.FloorToInt(_size*_size*elapsed/duration));
            if (shown == _lastScan) { return; }
            _lastScan = shown;
            float fly = Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.43f));
            for (int i = 0; i < _count; i++)
            {
                PositionImage(i);
                _images[i].UpdateMap(_results[i],_scales[i],false,shown);
                _scanWindows[i].gameObject.SetActive(shown < _size*_size);
                if (shown == _size*_size) { continue; }
                Transform window = _scanWindows[i].transform;
                FeatureMapPanel source = _sources[i];
                Vector3 position = IsOutput ? source.Root.TransformPoint(0,0,-.035f)
                    : source.Root.TransformPoint(source.CellPosition(shown));
                Transform plate = _filter.Plate(i);
                window.position = Vector3.Lerp(plate.position,position,fly);
                window.rotation = Quaternion.Slerp(plate.rotation,source.Root.rotation,fly);
                float width = IsOutput ? source.Width : source.Width*3/_size;
                window.localScale = Vector3.one*Mathf.Lerp(.8f,width,fly);
            }
        }

        private void Merge(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed/1.9f);
            for (int i = 0; i < _count; i++)
            {
                FeatureMapPanel image = _images[i];
                image.Root.localPosition = Vector3.Lerp(_mergePositions[i],_destination+Vector3.back*.058f,
                    Mathf.SmoothStep(0,1,t));
                image.Root.localRotation = Quaternion.Slerp(_sources[i].Root.localRotation,
                    _target == null ? Quaternion.identity : _target.Root.localRotation,t);
                image.Root.localScale = Vector3.one*(IsOutput ? Mathf.Lerp(1,.2f,t) : 1);
                image.Opacity(1-Mathf.InverseLerp(.75f,1,t));
                if (t == 1) { image.Root.gameObject.SetActive(false); }
            }
        }

        internal string Body()
        {
            switch (Stage)
            {
                case 0: return CorridorDialogue.FilterIntro(Layer,Map,_digit);
                case 1: return CorridorDialogue.FilterOpen(Layer,_count,_size);
                case 2: return CorridorDialogue.Scan(Layer);
                case 3: return CorridorDialogue.Explain(Layer,Map,_digit);
                case 4: return CorridorDialogue.Compare(Layer,_selectedTerms,_outputCell.pre[Index],_outputCell.dead);
                case 5: return CorridorDialogue.Merge(Layer,Map,_digit,_count,_output.Winner);
                case 6: return CorridorDialogue.MatchTemplates;
                case 7: return CorridorDialogue.ShareLight(_output.Winner,_output.WinnerPercent);
                default: return CorridorDialogue.FlyAnswer(_output.Winner);
            }
        }

        internal void Recap(HintBubbles hints, bool mandatory)
        {
            var names = new string[_count];
            for (int i = 0; i < _count; i++)
                names[i] = Layer == 1 ? CorridorDialogue.InputChannels[i] : CorridorDialogue.MapId(Layer-1,i);
            Color32[] colors = Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs;
            if (IsOutput) { hints.AddRecap(new Vector3(_destination.x,4.5f,_destination.z-.7f),names,CorridorDialogue.OutputScore(_digit),colors); return; }
            for (int i = 0; i < _maps[Layer-1].Length; i++)
                if (mandatory || i == Map)
                    hints.AddRecap(_maps[Layer-1][i].Position+Vector3.up*1.6f,names,CorridorDialogue.MapId(Layer,i),colors);
        }

        internal void Close()
        {
            _filter.Hide();
            _input.MarkChannels(false);
            _target?.ClearHighlight();
            _target?.Frame();
            if (_sources != null)
            foreach (FeatureMapPanel source in _sources)
            {
                source.ClearHighlight();
                if (Layer == 1) { continue; }
                source.Frame();
                source.Badge("",_visuals.Theme.Dim);
            }
            for (int i = 0; i < _images.Length; i++)
            {
                _images[i].Root.gameObject.SetActive(false);
                _scanWindows[i].gameObject.SetActive(false);
            }
        }
    }
}
