using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class InputPanel
    {
        private static readonly Vector3 ImagePosition = new Vector3(0,3.1f,-7.6f);
        [SerializeField] private FeatureMapPanel[] _channels = new FeatureMapPanel[3];
        private Vector3[] _starts = new Vector3[3];
        private Vector3[] _ends = new Vector3[3];
        private float _started;
        private float _pressed = float.NegativeInfinity;
        private bool _expanded;
        private bool _moving;
        [SerializeField] private FeatureMapPanel _picture;
        internal FeatureMapPanel Picture => _picture;
        internal FeatureMapPanel Channel(int index) => _channels[index];

        [SerializeField] private UI.WorldUIButtonController[] _digitButtons = new UI.WorldUIButtonController[3];
        [SerializeField] private Transform[] _expandedAnchors = new Transform[3];

        internal void Bind(CorridorVisuals visuals, Action<int> pixel, Action<int> digit)
        {
            Action<int> select = index => { SetExpanded(true); pixel(index); };
            Picture.Bind(visuals, select, () => SetExpanded(false));
            for (int i = 0; i < 3; i++)
            {
                int number = i + 1;
                _channels[i].Bind(visuals, select, () => SetExpanded(false));
                _ends[i] = _expandedAnchors[i].localPosition;
                visuals.Listen(_digitButtons[i].GetComponent<UnityEngine.UI.Button>(), () => digit(number));
            }
        }

        private InputPanel() { }

#if UNITY_EDITOR
        internal InputPanel(CorridorVisuals visuals, Transform root, int size, Action<int> pixel, Action<int> digit)
        {
            Color frame = new Color32(30,58,72,255);
            for (int side = -1; side <= 1; side += 2)
                visuals.Shape(root,"Gate pillar",new Vector3(side*1.15f,2.15f,ImagePosition.z),
                    new Vector3(.12f,4.3f,.12f),frame);
            visuals.Shape(root,"Gate lintel",new Vector3(0,4.25f,ImagePosition.z),new Vector3(2.42f,.12f,.12f),frame);
            visuals.Label(root,CorridorDialogue.InputLabel,new Vector3(0,4.62f,ImagePosition.z),.26f,3,visuals.Theme.Ink);
            _picture = new FeatureMapPanel(visuals,root,ImagePosition,0,size,"","",index =>
            {
                float now = Time.unscaledTime;
                if (now-_pressed <= .4f)
                {
                    _pressed = float.NegativeInfinity;
                    SetExpanded(false);
                    return;
                }
                _pressed = now;
                SetExpanded(true);
                pixel(index);
            },16);
            Picture.Root.name = "Input image";
            for (int i = 0; i < 3; i++)
            {
                int number = i+1;
                _ends[i] = new Vector3((i-1)*2.25f,1.05f,-7.75f);
                _expandedAnchors[i] = CorridorVisuals.Group(root, "RGB expanded " + number, _ends[i]);
                _channels[i] = new FeatureMapPanel(visuals,root,ImagePosition,0,size,"","",pixel,16);
                _channels[i].Root.name = "Input channel " + CorridorDialogue.InputChannels[i];
                _channels[i].Root.gameObject.SetActive(false);
                _channels[i].Frame(CorridorTheme.Rgb[i]);
                _channels[i].Badge(CorridorDialogue.InputChannels[i],CorridorTheme.Rgb[i]);
                var button = visuals.Button(root,number.ToString(),new Vector3(1.67f,2.46f+i*.43f,-7.68f),
                    new Vector2(370,340),() => digit(number));
                _digitButtons[i] = button;
                button.GetComponent<UnityEngine.UI.Image>().color = new Color32(20,53,63,255);
                var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
                label.color = visuals.Theme.Ink;
                label.resizeTextMaxSize = 112;
                label.fontStyle = FontStyle.Bold;
                button.ApplyPalette(Color.white,Color.white,new Color(.8f,.95f,.94f),Color.white,visuals.Theme.Cyan);
            }
        }
#endif

        internal void Draw(float[][] values)
        {
            Picture.DrawInput(values,-1);
            for (int i = 0; i < 3; i++) { _channels[i].DrawInput(values,i); }
        }

        internal void MarkChannels(bool marked)
        {
            for (int i = 0; i < 3; i++)
                _channels[i].Badge((marked ? CorridorDialogue.Marks[i]+" " : "")+CorridorDialogue.InputChannels[i],CorridorTheme.Rgb[i]);
        }

        internal void SetExpanded(bool expanded)
        {
            if (_expanded == expanded) { return; }
            _expanded = expanded;
            _moving = true;
            _started = Time.unscaledTime;
            Picture.Opacity(expanded ? .45f : 1);
            for (int i = 0; i < 3; i++)
            {
                _starts[i] = _channels[i].Position;
                _channels[i].Root.gameObject.SetActive(true);
            }
        }

        internal void Tick(float now)
        {
            if (!_moving) { return; }
            float time = Mathf.Clamp01((now-_started)/.4f);
            float smooth = Mathf.SmoothStep(0,1,time);
            for (int i = 0; i < 3; i++)
            {
                _channels[i].Root.localPosition = Vector3.Lerp(_starts[i],_expanded ? _ends[i] : Picture.Position,smooth);
                if (time == 1 && !_expanded) { _channels[i].Root.gameObject.SetActive(false); }
            }
            _moving = time < 1;
        }
    }
}
