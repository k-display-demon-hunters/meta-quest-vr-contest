using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class OutputStageController
    {
        private CorridorRenderUtility _visuals;
        [SerializeField] private Transform[] _liquid = new Transform[3];
        [SerializeField] private TMP_Text[] _percent = new TMP_Text[3];
        [SerializeField] private Transform[] _templates = new Transform[3];
        [SerializeField] private LineRenderer[] _templateFrame = new LineRenderer[3];
        [SerializeField] private Transform[] _columns = new Transform[3];
        [SerializeField] private TMP_Text[] _scoreLabels = new TMP_Text[3];
        [SerializeField] private LineRenderer[] _streams = new LineRenderer[3];
        [SerializeField] private Renderer[] _liquidRenderers = new Renderer[3];
        [SerializeField] private Renderer _tankLiquid;
        [SerializeField] private TMP_Text _tankLabel;
        [SerializeField] private Transform _evidence;
        [SerializeField] private Renderer[] _evidencePlates;
        [SerializeField] private Transform _answer;
        [SerializeField] private Renderer _answerImage;
        [SerializeField] private TMP_Text _answerText;
        private InputPanelController _input;
        private FeatureMapPanelController[] _last;
        private int[] _displayPercent = new int[3];
        private int[] _targetPercent = new int[3];
        private float[] _fromFill = new float[3];
        private float[] _currentFill = new float[3];
        private float[] _fill = new float[3];
        private System.Collections.Generic.IReadOnlyList<float> _scores;
        private System.Collections.Generic.IReadOnlyList<float> _probabilities;
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
        [SerializeField] private Transform _root;
        [SerializeField] private Transform _staging;
        [SerializeField] private Transform[] _tankAnchors = new Transform[3];
        [SerializeField] private Transform[] _answerAnchors = new Transform[3];
        [SerializeField] private XRSimpleInteractable[] _tankHits = new XRSimpleInteractable[3];
        private Vector3[] _liquidPosition = new Vector3[3];
        private Vector3[] _liquidScale = new Vector3[3];
        private Vector3[] _columnPosition = new Vector3[3];
        private Vector3[] _columnScale = new Vector3[3];
        private Vector3 _tankPosition;
        private Vector3 _tankScale;
        internal void Bind(CorridorRenderUtility visuals, InteractionManager interaction, InputPanelController input, FeatureMapPanelController[] last, Action<int> chosen)
        {
            _visuals = visuals;
            _input = input;
            _last = last;
            for (int i = 0; i < 3; i++)
            {
                int digit = i + 1;
                Vector3 scale = _tankHits[i].transform.localScale;
                XRSimpleInteractable hit = _tankHits[i];
                interaction.Bind(hit, _ => chosen(digit), null, point => hit.transform.localScale = scale*(point.HasValue ? 1.04f : 1f));
                _liquidPosition[i] = _liquid[i].localPosition;
                _liquidScale[i] = _liquid[i].localScale;
                _columnPosition[i] = _columns[i].localPosition;
                _columnScale[i] = _columns[i].localScale;
            }
            _tankPosition = _tankLiquid.transform.localPosition;
            _tankScale = _tankLiquid.transform.localScale;
            for (int i = 0; i < 3; i++)
            {
                Vector3 start = _tankLiquid.transform.TransformPoint(Vector3.down);
                Vector3 end = _root.TransformPoint(Tin(i + 1));
                Transform line = _streams[i].transform;
                _streams[i].SetPosition(0, line.InverseTransformPoint(start));
                _streams[i].SetPosition(1, line.InverseTransformPoint(Vector3.Lerp(start, end, .55f)));
                _streams[i].SetPosition(2, line.InverseTransformPoint(end));
            }
            for (int i = 0; i < _evidencePlates.Length; i++)
            {
                visuals.Texture(_evidencePlates[i], last[i].Texture);
            }
            Reset();
        }

        private OutputStageController()
        {
        }

        internal Vector3 Tin(int digit) => _root.InverseTransformPoint(_tankAnchors[digit-1].TransformPoint(Vector3.up*2.5f));
        internal Vector3 AnswerDestination => _root.InverseTransformPoint(_answerAnchors[_winner].position);
        private Vector3 StagingPosition => _root.InverseTransformPoint(_staging.position);
        internal void SetData(FeatureMapSnapshot snapshot)
        {
            int oldWinner = _winner;
            _scores = snapshot.Scores;
            _probabilities = snapshot.Probabilities;
            _winner = 0;
            for (int i = 1; i < 3; i++)
            if (_probabilities[i] > _probabilities[_winner])
            {
                _winner = i;
            }
            PercentageFormatter.RoundPercentages(_probabilities, _targetPercent);
            if (!_completed)
            {
                return;
            }
            StartFill(false);
            if (oldWinner != _winner)
            {
                Fly(false);
            }
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
                SetFill(i, 0);
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
                SetScore(i, 0);
                _visuals.Paint(_templateFrame[i], new Color(.38f, .89f, .85f, .22f));
            }
        }

        internal void FinishMatch()
        {
            _matching = false;
            _evidence.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                SetScore(i, 1);
                Color color = Color.Lerp(_visuals.Theme.Cyan, _visuals.Theme.Amber, Mathf.Clamp01(_scores[i]));
                color.a = .4f;
                _visuals.Paint(_templateFrame[i], color);
            }
        }

        private void SetScore(int digit, float fraction)
        {
            float level = Mathf.Max(.04f, _scores[digit]) * fraction;
            Transform column = _columns[digit];
            Vector3 scale = _columnScale[digit];
            column.localPosition = _columnPosition[digit] + Vector3.down * (scale.y * (1 - level));
            column.localScale = new Vector3(scale.x, Mathf.Max(.001f, scale.y * level), scale.z);
            _scoreLabels[digit].transform.position = column.TransformPoint(Vector3.up) + _tankAnchors[digit].up * .18f;
        }

        private void Match(float elapsed)
        {
            if (elapsed >= 7.55f)
            {
                FinishMatch();
                return;
            }
            if (elapsed < 1.6f)
            {
                _evidence.localPosition = StagingPosition;
                float t = Mathf.SmoothStep(0, 1, elapsed/1.6f);
                for (int i = 0; i < _evidencePlates.Length; i++)
                {
                    Transform plate = _evidencePlates[i].transform;
                    plate.localPosition = Vector3.Lerp(_last[i].Position-StagingPosition, new Vector3(i*.06f, i*.06f, i*.16f), t);
                    plate.localRotation = Quaternion.Slerp(_last[i].Root.localRotation, Quaternion.identity, t);
                    plate.localScale = Vector3.one*Mathf.Lerp(_last[i].Width, 1, t);
                }
                return;
            }
            float phase = (elapsed-1.6f)/1.5f;
            int digit = Mathf.Min(3, Mathf.FloorToInt(phase));
            Vector3 from = digit == 0 ? StagingPosition : _root.InverseTransformPoint(_templates[digit-1].position);
            Vector3 to = digit == 3 ? StagingPosition : _root.InverseTransformPoint(_templates[digit].position);
            _evidence.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, Mathf.Clamp01((phase-digit)*2.5f)));
            _evidence.localPosition += Vector3.back*.12f;
            for (int i = 0; i < 3; i++)
            {
                float level = Mathf.Clamp01((phase-i-.4f)*1.6667f);
                SetScore(i, level);
                Color color = Color.Lerp(_visuals.Theme.Cyan, _visuals.Theme.Amber, Mathf.Clamp01(_scores[i])*level);
                color.a = .22f+level*.18f;
                _visuals.Paint(_templateFrame[i], color);
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
            if (_fillDuration > 0)
            {
                Fill(1);
            }
        }

        private void Fill(float t)
        {
            float total = 0;
            for (int i = 0; i < 3; i++)
            {
                SetFill(i, Mathf.Lerp(_fromFill[i], _fill[i], t));
                total += _currentFill[i];
            }
            if (_shareTank)
            {
                PercentageFormatter.RoundPercentages(_fill, _displayPercent, t);
            }
            else if (total > 0)
            {
                PercentageFormatter.RoundPercentages(_currentFill, _displayPercent);
            }
            for (int i = 0; i < 3; i++)
            {
                _percent[i].SetText("{0:0}%", _displayPercent[i]);
            }
            if (_shareTank)
            {
                UpdateTank(1-t);
            }
            if (t < 1)
            {
                return;
            }
            _filled = true;
            _fillDuration = 0;
            foreach (LineRenderer stream in _streams)
            {
                stream.enabled = false;
            }
        }

        private void SetFill(int digit, float value)
        {
            _currentFill[digit] = value;
            Transform fill = _liquid[digit];
            Vector3 scale = _liquidScale[digit];
            fill.gameObject.SetActive(value > 0);
            fill.localPosition = _liquidPosition[digit] + Vector3.down * (scale.y * (1 - value));
            fill.localScale = new Vector3(scale.x, Mathf.Max(.001f, scale.y * value), scale.z);
            _visuals.Paint(_liquidRenderers[digit], digit == _winner ? _visuals.Theme.Amber : new Color32(159, 233, 226, 255));
        }

        private void UpdateTank(float fraction)
        {
            _tankLiquid.enabled = fraction > 0;
            _tankLiquid.transform.localPosition = _tankPosition + Vector3.down * (_tankScale.y * (1 - fraction));
            _tankLiquid.transform.localScale = new Vector3(_tankScale.x, _tankScale.y * fraction, _tankScale.z);
            _tankLabel.SetText("{0:0}%", Mathf.RoundToInt(fraction*100));
        }

        internal void Fly(bool fromInput)
        {
            _answerFrom = fromInput ? _input.Picture.Position : _answer.localPosition;
            _answer.gameObject.SetActive(true);
            _visuals.Texture(_answerImage, _input.Picture.Texture);
            _answerText.text = FeatureMapLabels.Answer(Winner);
            _flightStarted = Time.unscaledTime;
            _flying = true;
        }

        internal void Complete()
        {
            FinishFill();
            if (!_filled)
            {
                StartFill(true);
                FinishFill();
            }
            _completed = true;
            _flying = false;
            _answer.localPosition = AnswerDestination;
        }

        internal void Tick(float now)
        {
            if (_matching)
            {
                Match(now-_matchStarted);
            }
            if (_fillDuration > 0)
            {
                Fill(Mathf.Clamp01((now-_fillStarted)/_fillDuration));
            }
            if (!_answer.gameObject.activeSelf)
            {
                return;
            }
            if (_flying)
            {
                float t = Mathf.Clamp01((now-_flightStarted)/2.6f);
                _answer.localPosition = Vector3.Lerp(_answerFrom, AnswerDestination, t)+Vector3.up*(12*t*(1-t));
                _flying = t < 1;
            }
            else
            {
                _answer.localPosition = AnswerDestination+Vector3.up*(.055f*Mathf.Sin(now*2));
            }
        }
    }
}
