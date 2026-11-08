using System;
using TMPro;
using UnityEngine;

namespace FeatureMapCorridor
{
    internal sealed class OutputStage
    {
        private const float BaseY = -2.4f;
        private const float CenterZ = 18.5f;
        private static readonly Vector3 Staging = new Vector3(0,3.2f,17.6f);
        private readonly CorridorVisuals _visuals;
        private readonly Transform[] _liquid = new Transform[3];
        private readonly TMP_Text[] _percent = new TMP_Text[3];
        private readonly Transform[] _templates = new Transform[3];

        private readonly LineRenderer[] _templateFrame = new LineRenderer[3];
        private readonly Transform[] _columns = new Transform[3];
        private readonly TMP_Text[] _scoreLabels = new TMP_Text[3];
        private readonly LineRenderer[] _streams = new LineRenderer[3];
        private readonly Renderer[] _liquidRenderers = new Renderer[3];
        private readonly Renderer _tankLiquid;
        private readonly TMP_Text _tankLabel;
        private readonly Transform _evidence;
        private readonly Renderer[] _evidencePlates;
        private readonly Transform _answer;
        private readonly Renderer _answerImage;
        private readonly TMP_Text _answerText;
        private readonly ICnnModel _model;
        private readonly InputPanel _input;
        private readonly FeatureMapPanel[] _last;
        private readonly int[] _displayPercent = new int[3];
        private readonly int[] _targetPercent = new int[3];
        private readonly float[] _fromFill = new float[3];
        private readonly float[] _currentFill = new float[3];
        private readonly float[] _fill = new float[3];
        private float[] _scores;
        private float[] _probabilities;
        private Vector3 _answerFrom;
        private float _fillStarted;
        private float _fillDuration;
        private float _flightStarted;
        private float _matchStarted;
        private bool _shareTank;
        private bool _flying;
        private bool _matching;
        private bool _filled;
        private bool _completed;
        private int _winner;
        internal int Winner => _winner+1;
        internal int WinnerPercent => _targetPercent[_winner];

        internal OutputStage(CorridorVisuals visuals, Transform root, ICnnModel model, InputPanel input, FeatureMapPanel[] last,
            Action<int> chosen)
        {
            _visuals = visuals;
            _model = model;
            _input = input;
            _last = last;

            _evidencePlates = new Renderer[last.Length];
            visuals.Label(root,CorridorDialogue.OutputLabel,new Vector3(0,5.9f,CenterZ),.28f,6,visuals.Theme.Ink);
            Color glass = new Color(159/255f,220/255f,1,.16f);
            for (int i = 0; i < 3; i++)
            {
                int digit = i+1;
                Vector3 tin = Tin(digit);
                var cylinder = visuals.Shape(root,"Glass tank",new Vector3(tin.x,BaseY+1.25f,CenterZ),
                    new Vector3(1,1.25f,1),glass,PrimitiveType.Cylinder,true,true);
                CorridorVisuals.Click(cylinder.GetComponent<Collider>(),_ => chosen(digit));
                visuals.Shape(root,"Tank support",new Vector3(tin.x,BaseY+.04f,CenterZ),
                    new Vector3(1.17f,.08f,1.17f),new Color32(30,58,72,255),PrimitiveType.Cylinder);
                Ring(root,tin, .505f);
                _liquidRenderers[i] = visuals.Shape(root,"Liquid",new Vector3(tin.x,BaseY,CenterZ),
                    new Vector3(.95f,0,.95f),visuals.Theme.Amber,PrimitiveType.Cylinder);
                _liquid[i] = _liquidRenderers[i].transform;
                visuals.Label(root,digit.ToString(),new Vector3(tin.x,BaseY+.4f,CenterZ-.58f),.48f,.6f,visuals.Theme.Ink,mono:true);
                _percent[i] = visuals.Label(root,"",tin+Vector3.up*.23f,.29f,.7f,visuals.Theme.Ink,mono:true);
                _templates[i] = CorridorVisuals.Group(root,"Template",new Vector3(tin.x,1.35f,CenterZ));
                _templateFrame[i] = TemplateWire(_templates[i]);
                for (int map = 0; map < last.Length; map++)
                {
                    var panel = new FeatureMapPanel(visuals,_templates[i],new Vector3(map*.06f,map*.06f,map*.16f),
                        0,model.Size,"","",null,width:1);
                    panel.Opacity(.55f);
                    panel.UpdateMap(model.OutputTemplate(digit,map),1,false);

                }
                visuals.Label(_templates[i],CorridorDialogue.Template(digit),new Vector3(.1f,.82f,0),.2f,1.6f,visuals.Theme.Cyan);
                _columns[i] = visuals.Shape(root,"Score pillar",new Vector3(tin.x+.78f,BaseY,CenterZ),
                    new Vector3(.12f,0,.12f),new Color(1.3f,1.2f,.85f),PrimitiveType.Cylinder).transform;
                _scoreLabels[i] = visuals.Label(root,CorridorDialogue.Score,new Vector3(tin.x+.78f,BaseY,CenterZ),.15f,.6f,visuals.Theme.Ink);
                _streams[i] = CorridorVisuals.Group(root,"Light stream",Vector3.zero).gameObject.AddComponent<LineRenderer>();
                _streams[i].useWorldSpace = false;
                _streams[i].positionCount = 3;
                _streams[i].SetPosition(0,new Vector3(0,4.59f,CenterZ));
                _streams[i].SetPosition(1,new Vector3(tin.x*.55f,2.5f,CenterZ-.12f));
                _streams[i].SetPosition(2,tin);
                _visuals.Line(_streams[i],visuals.Theme.Amber,.05f);
            }
            var tank = new Vector3(0,5.1f,CenterZ);
            _tankLiquid = visuals.Shape(root,"Light tank liquid",tank,new Vector3(1.18f,.5f,1.18f),
                (Color)visuals.Theme.Amber*1.2f,PrimitiveType.Cylinder);
            visuals.Shape(root,"Light tank glass",tank,new Vector3(1.2f,.5f,1.2f),glass,PrimitiveType.Cylinder,transparent:true);
            _tankLabel = visuals.Label(root,CorridorDialogue.FullTank,tank+Vector3.up*.65f,.28f,.8f,visuals.Theme.Amber,mono:true);
            _evidence = CorridorVisuals.Group(root,"Evidence stack",Staging);
            for (int i = 0; i < last.Length; i++)
            {
                _evidencePlates[i] = visuals.Shape(_evidence,"Evidence plate",Vector3.zero,Vector3.one,Color.white,PrimitiveType.Quad);
                visuals.Texture(_evidencePlates[i],last[i].Texture);
            }
            _answer = CorridorVisuals.Group(root,"Answer",Vector3.zero);
            visuals.Shape(_answer,"Answer frame",new Vector3(0,0,.025f),new Vector3(1.37f,1.37f,.045f),visuals.Theme.Amber);
            _answerImage = visuals.Shape(_answer,"Answer image",Vector3.zero,new Vector3(1.25f,1.25f,1),Color.white,PrimitiveType.Quad);
            _answerText = visuals.Label(_answer,"",new Vector3(0,.9f,-.02f),.29f,2.7f,visuals.Theme.Amber);
            Reset();
        }

        private LineRenderer TemplateWire(Transform parent)
        {
            var line = CorridorVisuals.Group(parent,"Template wire",new Vector3(.11f,.11f,.35f)).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            int[] corners = { 0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4 };
            line.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++)
            {
                int corner = corners[i];
                line.SetPosition(i,new Vector3((corner&1) == 0 ? -.67f : .67f,
                    (corner&2) == 0 ? -.67f : .67f,(corner&4) == 0 ? -.44f : .44f));
            }
            _visuals.Line(line,_visuals.Theme.Cyan,.021f);
            return line;
        }

        private void Ring(Transform root, Vector3 center, float radius)
        {
            var ring = CorridorVisuals.Group(root,"Tank rim",center).gameObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 40;
            for (int i = 0; i < 40; i++)
            {
                float angle = i*Mathf.PI*2/40;
                ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
            }
            _visuals.Line(ring,_visuals.Theme.Cyan,.032f);
        }

        internal Vector3 Tin(int digit) => new Vector3((digit-2)*1.7f,BaseY+2.5f,CenterZ);
        internal Vector3 AnswerDestination => new Vector3(Tin(Winner).x,3.05f,CenterZ);

        internal void SetData(LayerOutput[][] forward)
        {
            int oldWinner = _winner;
            _scores = _model.Scores(forward);
            _probabilities = _model.Probabilities(forward);
            _winner = 0;
            for (int i = 1; i < 3; i++)
                if (_probabilities[i] > _probabilities[_winner]) { _winner = i; }
            CorridorMath.RoundPercentages(_probabilities,_targetPercent);
            if (!_completed) { return; }
            StartFill(false);
            if (oldWinner != _winner) { Fly(false); }
        }

        internal void Reset()
        {
            _completed = false;
            _filled = false;
            _matching = false;
            _flying = false;
            _fillDuration = 0;
            _answer.gameObject.SetActive(false);
            _evidence.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                _templates[i].gameObject.SetActive(false);
                _columns[i].gameObject.SetActive(false);
                _scoreLabels[i].gameObject.SetActive(false);
                _streams[i].enabled = false;
                SetFill(i,0);
                _percent[i].text = "";
            }
            UpdateTank(1);
        }

        internal void StartMatch()
        {
            _matching = true;
            _matchStarted = Time.unscaledTime;
            _evidence.gameObject.SetActive(true);
            for (int i = 0; i < 3; i++)
            {
                _templates[i].gameObject.SetActive(true);
                _columns[i].gameObject.SetActive(true);
                _scoreLabels[i].gameObject.SetActive(true);
                SetScore(i,0);
                _visuals.Paint(_templateFrame[i],new Color(.38f,.89f,.85f,.22f));
            }
        }

        internal void FinishMatch()
        {
            _matching = false;
            _evidence.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                SetScore(i,1);
                Color color = Color.Lerp(_visuals.Theme.Cyan,_visuals.Theme.Amber,Mathf.Clamp01(_scores[i]));
                color.a = .4f;
                _visuals.Paint(_templateFrame[i],color);
            }
        }

        private void SetScore(int digit, float fraction)
        {
            float height = Mathf.Max(.04f,_scores[digit])*2.3f*fraction;
            Vector3 position = _columns[digit].localPosition;
            position.y = BaseY+height*.5f;
            _columns[digit].localPosition = position;
            _columns[digit].localScale = new Vector3(.12f,Mathf.Max(.001f,height*.5f),.12f);
            _scoreLabels[digit].transform.localPosition = new Vector3(position.x,BaseY+height+.18f,position.z);
        }

        private void Match(float elapsed)
        {
            if (elapsed >= 7.55f) { FinishMatch(); return; }
            if (elapsed < 1.6f)
            {
                _evidence.localPosition = Staging;
                float t = Mathf.SmoothStep(0,1,elapsed/1.6f);
                for (int i = 0; i < _evidencePlates.Length; i++)
                {
                    Transform plate = _evidencePlates[i].transform;
                    plate.localPosition = Vector3.Lerp(_last[i].Position-Staging,new Vector3(i*.06f,i*.06f,i*.16f),t);
                    plate.localRotation = Quaternion.Slerp(_last[i].Root.localRotation,Quaternion.identity,t);
                    plate.localScale = Vector3.one*Mathf.Lerp(_last[i].Width,1,t);
                }
                return;
            }
            float phase = (elapsed-1.6f)/1.5f;
            int digit = Mathf.Min(3,Mathf.FloorToInt(phase));
            Vector3 from = digit == 0 ? Staging : _templates[digit-1].localPosition;
            Vector3 to = digit == 3 ? Staging : _templates[digit].localPosition;
            _evidence.localPosition = Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-digit)*2.5f)));
            _evidence.localPosition += Vector3.back*.12f;
            for (int i = 0; i < 3; i++)
            {
                float level = Mathf.Clamp01((phase-i-.4f)*1.6667f);
                SetScore(i,level);
                Color color = Color.Lerp(_visuals.Theme.Cyan,_visuals.Theme.Amber,Mathf.Clamp01(_scores[i])*level);
                color.a = .22f+level*.18f;
                _visuals.Paint(_templateFrame[i],color);
            }
        }

        internal void StartFill(bool shareTank)
        {
            _shareTank = shareTank;
            _fillStarted = Time.unscaledTime;
            _fillDuration = shareTank ? 3 : .8f;
            for (int i = 0; i < 3; i++)
            {
                _fromFill[i] = shareTank ? 0 : _currentFill[i];
                _fill[i] = _probabilities[i];
                _streams[i].enabled = shareTank;
                _streams[i].widthMultiplier = .025f+.105f*_fill[i];
            }
            Fill(0);
        }

        internal void FinishFill()
        {
            if (_fillDuration > 0) { Fill(1); }
        }

        private void Fill(float t)
        {
            float total = 0;
            for (int i = 0; i < 3; i++)
            {
                SetFill(i,Mathf.Lerp(_fromFill[i],_fill[i],t));
                total += _currentFill[i];
            }
            if (_shareTank) { CorridorMath.RoundPercentages(_fill,_displayPercent,t); }
            else if (total > 0) { CorridorMath.RoundPercentages(_currentFill,_displayPercent); }
            for (int i = 0; i < 3; i++) { _percent[i].SetText("{0:0}%",_displayPercent[i]); }
            if (_shareTank) { UpdateTank(1-t); }
            if (t < 1) { return; }
            _filled = true;
            _fillDuration = 0;
            foreach (LineRenderer stream in _streams) { stream.enabled = false; }
        }

        private void SetFill(int digit, float value)
        {
            _currentFill[digit] = value;
            float height = value*2.25f;
            Transform fill = _liquid[digit];
            fill.gameObject.SetActive(height > 0);
            fill.localPosition = new Vector3(Tin(digit+1).x,BaseY+height*.5f,CenterZ);
            fill.localScale = new Vector3(.94f,Mathf.Max(.001f,height*.5f),.94f);
            _visuals.Paint(_liquidRenderers[digit], digit == _winner ? _visuals.Theme.Amber : new Color32(159,233,226,255));
        }

        private void UpdateTank(float fraction)
        {
            _tankLiquid.enabled = fraction > 0;
            _tankLiquid.transform.localPosition = new Vector3(0,4.6f+.5f*fraction,CenterZ);
            _tankLiquid.transform.localScale = new Vector3(1.18f,.5f*fraction,1.18f);
            _tankLabel.SetText("{0:0}%",Mathf.RoundToInt(fraction*100));
        }

        internal void Fly(bool fromInput)
        {
            _answerFrom = fromInput ? _input.Picture.Position : _answer.localPosition;
            _answer.gameObject.SetActive(true);
            _visuals.Texture(_answerImage,_input.Picture.Texture);
            _answerText.text = CorridorDialogue.Answer(Winner);
            _flightStarted = Time.unscaledTime;
            _flying = true;
        }

        internal void Complete()
        {
            FinishFill();
            if (!_filled) { StartFill(true); FinishFill(); }
            _completed = true;
            _flying = false;
            _answer.localPosition = AnswerDestination;
        }

        internal void Tick(float now)
        {
            if (_matching) { Match(now-_matchStarted); }
            if (_fillDuration > 0) { Fill(Mathf.Clamp01((now-_fillStarted)/_fillDuration)); }
            if (!_answer.gameObject.activeSelf) { return; }
            if (_flying)
            {
                float t = Mathf.Clamp01((now-_flightStarted)/2.6f);
                _answer.localPosition = Vector3.Lerp(_answerFrom,AnswerDestination,t)+Vector3.up*(12*t*(1-t));
                _flying = t < 1;
            }
            else { _answer.localPosition = AnswerDestination+Vector3.up*(.055f*Mathf.Sin(now*2)); }
        }
    }
}
