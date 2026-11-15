using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class TrainingFlowController : MonoBehaviour
    {
        private const int ParticleLimit = 28;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Material _flowMaterial;
        [SerializeField] private Font _labelFont;
        [SerializeField, ColorUsage(false, true)] private Color _forward = new Color(1f, .72f, .36f);
        [SerializeField, ColorUsage(false, true)] private Color _backward = new Color(1f, .478f, .522f);
        [SerializeField, Min(.05f)] private float _speed = .8f;
        [SerializeField, Min(.01f)] private float _trackGap = .06f;
        [SerializeField, Min(0f)] private float _brightness = .85f;

        private readonly List<(Vector3 Center, Vector2 Extents, bool Trainable,
            ModelBlockController Block, int Slot)> _layers = new();
        private readonly List<Vector3> _path = new();
        private readonly List<float> _distances = new();
        private readonly List<float> _blockDistances = new();
        private MaterialPropertyBlock _properties;
        private readonly LineRenderer[] _forwardTrails = new LineRenderer[ParticleLimit];
        private readonly LineRenderer[] _backwardTrails = new LineRenderer[ParticleLimit];
        private readonly List<LineRenderer> _outlines = new();
        private readonly List<LineRenderer> _ripples = new();

        private GameObject _visuals;
        private Text _forwardLabel;
        private Text _backwardLabel;
        private LineRenderer _epochTrail;
        private Func<int, ModelBlockController, bool> _isInSlot;
        private bool _training;
        private bool _reversing;
        private float _readyAt, _travelSpeed;
        private bool _started;
        private float _forwardTravel;
        private float _backwardTravel;
        private float _fade = -1f;
        private float _epochTime;
        private int _pendingEpochs;
        private int _lastEpoch;

        private void Awake() => _properties = new MaterialPropertyBlock();

        internal void Play((Vector3 Center, Vector2 Extents, bool Trainable)[] frames,
            IReadOnlyList<(ModelBlockController Block, int Slot)> occupied,
            Func<int, ModelBlockController, bool> isInSlot, bool training, float readyAt)
        {
            Cancel();
            _training = training;
            _readyAt = readyAt;
            for (int index = 0; index < frames.Length; index++)
            {
                var frame = frames[index];
                var source = training && index > 0 && index < frames.Length - 1
                    ? occupied[index - 1] : (Block: (ModelBlockController)null, Slot: -1);
                _layers.Add((frame.Center, frame.Extents, frame.Trainable, source.Block, source.Slot));
            }
            _isInSlot = isInSlot;
            _visuals = new GameObject("Model flow visuals");
            _visuals.transform.SetParent(transform, false);
            _forwardLabel = MakeLabel(training ? "순전파 · 예측 계산" : "순전파 · 예측");
            if (training)
            {
                _backwardLabel = MakeLabel("역전파 → 가중치 수정");
                _epochTrail = MakeLine(5, false);
            }
            _visuals.SetActive(false);
        }

        internal void QueueEpoch(int epoch)
        {
            if (!_training || _visuals == null || _fade >= 0f || epoch <= _lastEpoch) { return; }
            _pendingEpochs += epoch - _lastEpoch;
            _lastEpoch = epoch;
        }

        internal void FinishTraining()
        {
            if (_training) { Fade(); }
        }

        internal void Fade()
        {
            if (_visuals != null && _fade < 0f) { _fade = 0f; }
        }

        internal void Cancel()
        {
            if (_visuals != null)
            {
                _visuals.SetActive(false);
                Destroy(_visuals);
            }
            _visuals = null;
            _isInSlot = null;
            _layers.Clear();
            _started = false;
            _outlines.Clear();
            _ripples.Clear();
            Array.Clear(_forwardTrails, 0, _forwardTrails.Length);
            Array.Clear(_backwardTrails, 0, _backwardTrails.Length);
            _forwardTravel = _backwardTravel = _epochTime = 0f;
            _pendingEpochs = _lastEpoch = 0;
            _fade = -1f;
            _reversing = false;
        }

        private void OnDisable() => Cancel();

        private void LateUpdate()
        {
            if (_visuals == null) { return; }
            if (!_started)
            {
                if (_fade >= 0f) { Cancel(); return; }
                if (Time.unscaledTime < _readyAt) { return; }
                _visuals.SetActive(true);
                _started = true;
            }
            BuildPath();
            float length = _distances[_distances.Count - 1];
            float reverseLength = Mathf.Max(0f, length - _layers[0].Extents.x - 0.08f);
            _travelSpeed = Mathf.Max(_speed, length / (_training ? 2.0f : 1.25f));
            float delta = Time.unscaledDeltaTime;
            if (_fade >= 0f)
            {
                _fade += delta;
                if (_fade >= .5f) { Cancel(); return; }
            }
            else
            {
                _forwardTravel += delta * _travelSpeed;
                if (_training && _forwardTravel >= length)
                {
                    if (!_reversing) { _backwardTravel = _forwardTravel - length; }
                    else { _backwardTravel += delta * _travelSpeed; }
                    _reversing = true;
                }
            }

            float opacity = _fade < 0f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, _fade / .5f);
            float spacing = _training ? Mathf.Max(.28f, length / (ParticleLimit - 1)) : .15f;
            DrawTrain(_forwardTrails, _forwardTravel, length, spacing, _forward, opacity, false);
            if (_training)
            {
                DrawTrain(_backwardTrails, _reversing ? _backwardTravel : -1f,
                    reverseLength, spacing, _backward, opacity, true);
                DrawEpoch(length, delta, opacity);
            }
            DrawBlockEffects(length, spacing, opacity);
            PositionLabel(_forwardLabel, _layers[0].Center + transform.up * _layers[0].Extents.y,
                _forward, opacity);
            if (_training)
            {
                var last = _layers[_layers.Count - 1];
                PositionLabel(_backwardLabel, last.Center + transform.up * last.Extents.y,
                    _backward, _reversing ? opacity : 0f);
            }
            if (!_training && _forwardTravel > length + .8f) { Cancel(); }
        }

        private void BuildPath()
        {
            for (int i = _layers.Count - 2; i > 0; i--)
            {
                var entry = _layers[i];
                if (entry.Slot < 0 || (entry.Block != null && !entry.Block.IsGrabbed
                    && _isInSlot(entry.Slot, entry.Block))) { continue; }
                _layers.RemoveAt(i);
            }

            _path.Clear();
            _distances.Clear();
            _blockDistances.Clear();
            foreach (var layer in _layers)
            {
                AddPoint(layer.Center);
                _blockDistances.Add(_distances[_distances.Count - 1]);
            }
        }

        private void AddPoint(Vector3 point)
        {
            float distance = _path.Count == 0 ? 0f :
                _distances[_distances.Count - 1] + Vector3.Distance(_path[_path.Count - 1], point);
            _path.Add(point);
            _distances.Add(distance);
        }

        private Vector3 Sample(float distance, bool reverse)
        {
            int last = _path.Count - 1;
            distance = Mathf.Clamp(distance, 0f, _distances[last]);
            int index = 1;
            while (index < last && _distances[index] < distance) { index++; }
            Vector3 point = Vector3.Lerp(_path[index - 1], _path[index],
                Mathf.InverseLerp(_distances[index - 1], _distances[index], distance));
            return point + transform.up * (_trackGap * (reverse ? -.5f : .5f));
        }

        private void DrawTrain(LineRenderer[] trails, float travel, float length, float spacing,
            Color color, float opacity, bool reverse)
        {
            int count = _training ? trails.Length : 5;
            float lead = travel;
            if (_training && travel >= length) { lead = length + Mathf.Repeat(travel - length, spacing); }
            for (int i = 0; i < count; i++)
            {
                float distance = lead - i * spacing;
                if (_fade >= 0f && distance > 0f && distance < length) { distance += _fade * _travelSpeed; }
                if (distance <= 0f || distance >= length)
                {
                    if (trails[i] != null) { trails[i].enabled = false; }
                    continue;
                }
                LineRenderer trail = trails[i] ??= MakeLine(5, false);
                float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(distance, length - distance) / .09f);
                DrawTrail(trail, distance, .19f, reverse, color, opacity * envelope, .021f);
            }
        }

        private void DrawTrail(LineRenderer trail, float distance, float tailLength, bool reverse,
            Color color, float opacity, float width)
        {
            float length = _distances[_distances.Count - 1];
            for (int i = 0; i < trail.positionCount; i++)
            {
                float point = Mathf.Max(0f, distance - tailLength * (1f - i / (float)(trail.positionCount - 1)));
                trail.SetPosition(i, Sample(reverse ? length - point : point, reverse));
            }
            SetLine(trail, color, opacity, width);
        }

        private void DrawEpoch(float length, float delta, float opacity)
        {
            _epochTrail.enabled = false;
            if (_pendingEpochs == 0) { return; }
            _epochTime += delta;
            float t = Mathf.Clamp01(_epochTime / .8f);
            DrawTrail(_epochTrail, length * t, length * .18f, false,
                new Color(.89f, .95f, 1f), opacity * .72f * Mathf.Sin(t * Mathf.PI), .029f);
            if (t >= 1f) { _pendingEpochs--; _epochTime = 0f; }
        }

        private void DrawBlockEffects(float length, float spacing, float opacity)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                var layer = _layers[i];
                float distance = _blockDistances[i];
                float ahead = PassageAge(_forwardTravel - distance, spacing);
                float behind = _reversing && i > 0
                    ? PassageAge(_backwardTravel - (length - distance), spacing) : -1f;
                float amber = Pulse(ahead);
                float coral = Pulse(behind);
                float power = Mathf.Max(amber, coral);
                LineRenderer outline = GetEffect(_outlines, i);
                outline.enabled = power > 0f;
                if (outline.enabled)
                {
                    Frame(outline, layer.Center, layer.Extents, 1f, .013f);
                    SetLine(outline, Color.Lerp(_forward, _backward, coral / (amber + coral)),
                        opacity * power * .72f, .015f);
                }

                if (!_training) { continue; }
                LineRenderer ripple = GetEffect(_ripples, i);
                ripple.enabled = behind >= 0f && behind < .6f && layer.Trainable;
                if (ripple.enabled)
                {
                    float t = behind / .6f;
                    Frame(ripple, layer.Center, layer.Extents, Mathf.Lerp(.94f, .28f, Mathf.SmoothStep(0f, 1f, t)), .018f);
                    SetLine(ripple, _backward, opacity * .44f * Mathf.Sin(t * Mathf.PI), .009f);
                }
            }
            for (int i = _layers.Count; i < _outlines.Count; i++) { _outlines[i].enabled = false; }
            for (int i = _layers.Count; i < _ripples.Count; i++) { _ripples[i].enabled = false; }
        }

        private float PassageAge(float distance, float spacing)
        {
            // Accent the leading group once per second or slower, while the particles keep moving.
            if (distance < 0f) { return -1f; }
            if (!_training) { return distance / _travelSpeed; }
            float groupSpacing = spacing * Mathf.Ceil(Mathf.Max(1f, _travelSpeed / spacing));
            return Mathf.Repeat(distance, groupSpacing) / _travelSpeed;
        }

        private static float Pulse(float age)
        {
            if (age < 0f || age >= .7f) { return 0f; }
            return age < .2f ? Mathf.SmoothStep(0f, 1f, age / .2f) :
                1f - Mathf.SmoothStep(0f, 1f, (age - .2f) / .5f);
        }

        private void Frame(LineRenderer line, Vector3 center, Vector2 extents, float scale, float offset)
        {
            center -= transform.forward * offset;
            Vector3 right = transform.right * (extents.x * scale);
            Vector3 up = transform.up * (extents.y * scale);
            line.SetPosition(0, center - right - up);
            line.SetPosition(1, center + right - up);
            line.SetPosition(2, center + right + up);
            line.SetPosition(3, center - right + up);
        }

        private LineRenderer GetEffect(List<LineRenderer> pool, int index)
        {
            while (pool.Count <= index) { pool.Add(MakeLine(4, true)); }
            return pool[index];
        }

        private LineRenderer MakeLine(int points, bool loop)
        {
            GameObject visual = new GameObject(loop ? "Flow outline" : "Flow trail");
            visual.transform.SetParent(_visuals.transform, false);
            LineRenderer line = visual.AddComponent<LineRenderer>();
            line.sharedMaterial = _flowMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = points;
            line.loop = loop;
            if (!loop) { line.widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); }
            else { line.numCornerVertices = 2; }
            line.enabled = false;
            return line;
        }

        private void SetLine(LineRenderer line, Color color, float opacity, float width)
        {
            float alpha = Mathf.Clamp01(opacity);
            if (alpha <= .001f) { line.enabled = false; return; }
            Color tint = color * _brightness;
            tint.a = alpha;
            _properties.SetColor(BaseColor, tint);
            line.SetPropertyBlock(_properties);
            line.widthMultiplier = width;
            line.enabled = true;
        }

        private Text MakeLabel(string label)
        {
            GameObject panel = new GameObject("Flow label", typeof(RectTransform), typeof(Canvas));
            panel.transform.SetParent(_visuals.transform, false);
            panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)panel.transform;
            rect.sizeDelta = new Vector2(380f, 64f);
            rect.localScale = Vector3.one * .0016f;
            GameObject child = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform textRect = (RectTransform)child.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            Text text = child.GetComponent<Text>();
            text.font = _labelFont;
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = label;
            return text;
        }

        private void PositionLabel(Text text, Vector3 point, Color color, float opacity)
        {
            // Keep the labels in front of the connector and readable from the assembly aisle.
            text.transform.parent.SetPositionAndRotation(point + transform.up * .105f - transform.forward * .09f,
                transform.rotation);
            color.a = opacity;
            text.color = color;
        }
    }
}
