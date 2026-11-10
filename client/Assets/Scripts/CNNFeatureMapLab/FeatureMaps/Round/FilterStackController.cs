using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class FilterStackController : IXRSelectFilter
    {
        private const int Capacity = 5;
        private CorridorRenderUtility _visuals;
        [SerializeField] private Transform _root;
        [SerializeField] private Renderer[] _frames = new Renderer[Capacity];
        [SerializeField] private Transform[] _card = new Transform[Capacity];
        [SerializeField] private Transform[] _weights = new Transform[Capacity];
        [SerializeField] private TMP_Text[] _badges = new TMP_Text[Capacity];
        [SerializeField] private FeatureMapPanelController[] _templates = new FeatureMapPanelController[Capacity];
        [SerializeField] private LineRenderer[] _links = new LineRenderer[4];
        [SerializeField] private LineRenderer _resultLink;
        [SerializeField] private TMP_Text _cover;
        [SerializeField] private TMP_Text _title;
        private Vector3[] _starts = new Vector3[Capacity];
        private FeatureMapPanelController[] _sources;
        private FeatureMapPanelController _result;
        private Color32[] _colors;
        private int _count;
        private int _layer;
        private float _motionStart;
        private bool _expanded;
        private bool _animating;
        private bool _connected;
        internal event Action<bool> ExpansionChanged;
        internal Transform Root => _root;
        internal Transform Plate(int index) => _card[index];
        internal Texture2D TemplateTexture(int index) => _templates[index].Texture;
        [SerializeField] private XRSimpleInteractable[] _cardHits = new XRSimpleInteractable[Capacity];
        [SerializeField] private float _spreadSpacing = .95f;
        private Vector3[] _foldedPositions = new Vector3[Capacity];
        internal void Bind(CorridorRenderUtility visuals)
        {
            _visuals = visuals;
            for (int i = 0; i < Capacity; i++)
            {
                _foldedPositions[i] = _card[i].localPosition;
                _templates[i].Bind(visuals);
                _inputScales[i] = _card[i].localScale;
            }
            Hide();
        }

        private Vector3[] _inputScales = new Vector3[Capacity];
        private XRSimpleInteractable _lastTarget;
        private float _lastClickTime;

        private bool _inputActive;
        private XRSimpleInteractable _hoveredTarget;
        internal event Action<string> InputClicked;
        internal string HoverName => _hoveredTarget == null ? null : _hoveredTarget.name;
        public bool canProcess => true;
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => false;

        internal void SetInputActive(bool active)
        {
            if (_inputActive == active) return;
            _inputActive = active;
            foreach (var target in _cardHits)
            {
                if (active)
                {
                    target.selectFilters.Add(this);
                    target.activated.AddListener(HandleActivated);
                    target.hoverEntered.AddListener(HandleHoverEntered);
                    target.hoverExited.AddListener(HandleHoverExited);
                    target.enabled = true;
                }
                else
                {
                    target.activated.RemoveListener(HandleActivated);
                    target.hoverEntered.RemoveListener(HandleHoverEntered);
                    target.hoverExited.RemoveListener(HandleHoverExited);
                    target.enabled = false;
                    target.selectFilters.Remove(this);
                }
            }
            if (!active)
            {
                for (int i = 0; i < _cardHits.Length; i++)
                    _card[i].localScale = _inputScales[i];
                _hoveredTarget = null;
                _lastTarget = null;
            }
        }

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {
            if (!(args.interactorObject is XRRayInteractor ray) ||
                ray.handedness != InteractorHandedness.Right) return;
            var target = (XRSimpleInteractable)args.interactableObject;
            _hoveredTarget = target;
            int index = Array.IndexOf(_cardHits, target);
            _card[index].localScale = _inputScales[index] * 1.04f;
        }

        private void HandleHoverExited(HoverExitEventArgs args)
        {
            if (!(args.interactorObject is XRRayInteractor ray) ||
                ray.handedness != InteractorHandedness.Right) return;
            var target = (XRSimpleInteractable)args.interactableObject;
            int index = Array.IndexOf(_cardHits, target);
            _card[index].localScale = _inputScales[index];
            if (_hoveredTarget == target) _hoveredTarget = null;
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            if (!(args.interactorObject is XRRayInteractor ray) ||
                ray.handedness != InteractorHandedness.Right || ray.hasSelection) return;
            var target = (XRSimpleInteractable)args.interactableObject;
            if (!ray.TryGetCurrentRaycast(out var hit, out _, out _, out _, out bool uiClosest) ||
                uiClosest || !hit.HasValue || !target.colliders.Contains(hit.Value.collider)) return;
            InputClicked?.Invoke(target.name);
            float now = Time.unscaledTime;
            bool twice = target == _lastTarget && now - _lastClickTime <= .4f;
            _lastTarget = twice ? null : target;
            _lastClickTime = now;
            if (twice) SetExpanded(false, false);
            else Expand();
        }

        internal void Dispose()
        {
            foreach (var template in _templates)
            {
                template.Dispose();
            }
        }

        private FilterStackController()
        {
        }

        internal void Configure(RoundCalculationResult calculation, FeatureMapPanelController[] sources, FeatureMapPanelController result, Vector3 destination)
        {
            int layer = calculation.Layer;
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
                _badges[i].text = FeatureMapLabels.Marks[i]+" "+(layer == 1 ? FeatureMapLabels.Channels[i] : $"{layer-1}-{i+1}");
                if (layer == 4)
                {
                    _templates[i].Frame(_colors[i]);
                    _templates[i].SetGridSize(calculation.Size);
                    _templates[i].UpdateMap(calculation.Template(i), 1);
                }
            }
            for (int i = _count; i < Capacity; i++)
            {
                _card[i].gameObject.SetActive(false);
            }
            float sourceDepth = layer == 1 ? -7.75f : sourceZ/_count;
            _root.localPosition = new Vector3(destination.x*.3f, layer == 4 ? 1.6f : destination.y+1.7f, (destination.z+sourceDepth)*.5f);
            _title.text = FeatureMapLabels.FilterTitle(layer, _count, layer == 4 ? calculation.Size : 3);
            _root.gameObject.SetActive(true);
            _connected = true;
            SetExpanded(false, true);
        }

        private void Expand()
        {
            if (_expanded)
            {
                return;
            }
            SetExpanded(true, false);
            ExpansionChanged?.Invoke(true);
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
                _visuals.Paint(_frames[i], expanded ? _colors[i] : i == 0 ? _visuals.Theme.Amber : new Color32(58, 74, 90, 255));
                if (instant)
                {
                    _card[i].localPosition = expanded ? Spread(i) : Folded(i);
                }
            }
            SetLinks(expanded && _connected);
        }

        private Vector3 Folded(int i) => _foldedPositions[i];
        private Vector3 Spread(int i) => Vector3.right*(i-(_count-1)*.5f)*_spreadSpacing;
        private void SetLinks(bool enabled)
        {
            for (int i = 0; i < _links.Length; i++)
            {
                _links[i].enabled = enabled && i < _count;
            }
            _resultLink.enabled = enabled && _result != null;
        }

        internal void ShowCards(bool visible) => _root.gameObject.SetActive(visible);
        internal void DimDuringScan(bool dim)
        {
            _connected = !dim;
            SetLinks(!dim && _expanded);
            if (!_expanded)
            {
                return;
            }
            for (int i = 0; i < _count; i++)
            {
                // Use the same physical card at reduced brightness while its plate is in flight.
                _visuals.Paint(_frames[i], (Color)_colors[i]*(dim ? .25f : 1));
                if (_layer == 4)
                {
                    _templates[i].Opacity(dim ? .25f : 1);
                }
            }
        }

        internal void Hide()
        {
            _root.gameObject.SetActive(false);
            SetLinks(false);
        }

        internal void Tick(float now)
        {
            if (!_root.gameObject.activeSelf)
            {
                return;
            }
            if (_animating)
            {
                float t = Mathf.Clamp01((now-_motionStart)/.5f);
                for (int i = 0; i < _count; i++)
                _card[i].localPosition = Vector3.Lerp(_starts[i], _expanded ? Spread(i) : Folded(i), Mathf.SmoothStep(0, 1, t));
                _animating = t < 1;
            }
            if (!_expanded || !_connected)
            {
                return;
            }
            for (int i = 0; i < _count; i++)
            {
                _visuals.Paint(_links[i], _colors[i]);
                _links[i].SetPosition(0, _sources[i].Root.position);
                _links[i].SetPosition(1, _card[i].position);
            }
            if (_result == null)
            {
                return;
            }
            _resultLink.SetPosition(0, _root.position);
            _resultLink.SetPosition(1, _result.Root.position);
        }
    }
}
