using System.Collections.Generic;
using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class InputPanelController
    {
        private const float CalculationDepthOffset = -1.25f;
        [SerializeField] private FeatureMapPanelController[] _channels = new FeatureMapPanelController[3];
        private readonly Vector3[] _starts = new Vector3[3];
        private readonly Vector3[] _ends = new Vector3[3];
        private float _started;
        private float _moveDuration;
        private bool _expanded;
        private bool _moving;
        private bool _atCalculation;
        private float? _followStartDepth;
        private float _forwardTravel;
        [SerializeField] private FeatureMapPanelController _picture;
        internal FeatureMapPanelController Picture => _picture;
        internal FeatureMapPanelController Channel(int index) => _channels[index];
        [SerializeField] private UI.WorldUIButtonController[] _digitButtons = new UI.WorldUIButtonController[3];
        [SerializeField] private Transform[] _expandedAnchors = new Transform[3];
        internal float ChannelSpacing => _expandedAnchors[1].localPosition.x - _expandedAnchors[0].localPosition.x;
        internal void Bind(CorridorRenderUtility visuals, Action<int> digit, Action pictureSelected)
        {
            Picture.Bind(visuals, pictureSelected);
            for (int i = 0; i < 3; i++)
            {
                int number = i + 1;
                _channels[i].Bind(visuals);
                _buttons.Add(_digitButtons[i].GetComponent<UnityEngine.UI.Button>(), () => digit(number));
            }
        }

        private readonly CorridorButtonBindings _buttons = new CorridorButtonBindings();
        internal event Action<string> ButtonPressed
        {
            add => _buttons.Pressed += value;
            remove => _buttons.Pressed -= value;
        }

        internal void Enable() => _buttons.Enable();
        internal void Disable() => _buttons.Disable();
        internal void SetCamera(Camera camera)
        {
            foreach (var button in _digitButtons)
            foreach (var canvas in button.GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = camera;
            }
        }

        internal void Dispose()
        {
            _picture.Dispose();
            foreach (var panel in _channels)
            {
                panel.Dispose();
            }
        }

        private InputPanelController()
        {
        }

        internal void Draw(IReadOnlyList<IReadOnlyList<float>> values)
        {
            Picture.DrawInput(values, -1);
            for (int i = 0; i < 3; i++)
            {
                _channels[i].DrawInput(values, i);
            }
        }

        internal void MarkChannels(bool marked)
        {
            for (int i = 0; i < 3; i++)
            _channels[i].Badge((marked ? FeatureMapLabels.Marks[i]+" " : "")+FeatureMapLabels.InputChannels[i], CorridorTheme.Rgb[i]);
        }

        internal void SetExpanded(bool expanded)
        {
            if (_expanded == expanded)
            {
                return;
            }
            _expanded = expanded;
            _atCalculation = false;
            _followStartDepth = null;
            _forwardTravel = 0f;
            Picture.Opacity(expanded ? .45f : 1);
            for (int i = 0; i < _channels.Length; i++)
            {
                _ends[i] = expanded ? _expandedAnchors[i].localPosition : Picture.Position;
                _channels[i].Root.gameObject.SetActive(true);
            }
            StartMove(.4f);
        }

        internal void FollowTowardsCalculation(Transform filterAnchor, Vector3 viewerPosition)
        {
            if (!_expanded || _atCalculation)
            {
                return;
            }

            Transform parent = Picture.Root.parent;
            float viewerDepth = parent.InverseTransformPoint(viewerPosition).z;
            _followStartDepth ??= viewerDepth;
            _forwardTravel = Mathf.Max(_forwardTravel, viewerDepth - _followStartDepth.Value);

            Vector3 start = _expandedAnchors[1].localPosition;
            Vector3 destination = parent.InverseTransformPoint(
                filterAnchor.TransformPoint(0, -1.2f, CalculationDepthOffset));
            float progress = Mathf.InverseLerp(start.z, destination.z, start.z + _forwardTravel);
            Vector3 travel = (destination - start) * progress;
            for (int i = 0; i < _channels.Length; i++)
            {
                _ends[i] = _expandedAnchors[i].localPosition + travel;
            }
            if (!_moving)
            {
                UpdateChannelPositions(1f);
            }
        }

        internal void MoveToCalculation(Transform filterAnchor, float duration, bool animate)
        {
            if (!_atCalculation)
            {
                _atCalculation = true;
                Transform parent = Picture.Root.parent;
                for (int i = 0; i < _channels.Length; i++)
                {
                    float offset = (i - (_channels.Length - 1) * .5f) * ChannelSpacing;
                    _ends[i] = parent.InverseTransformPoint(
                        filterAnchor.TransformPoint(offset, -1.2f, CalculationDepthOffset));
                }
                StartMove(duration);
            }

            if (!animate && _moving)
            {
                UpdateChannelPositions(1f);
            }
        }

        private void StartMove(float duration)
        {
            _started = Time.unscaledTime;
            _moveDuration = duration;
            _moving = true;
            for (int i = 0; i < _channels.Length; i++)
            {
                _starts[i] = _channels[i].Position;
            }
        }

        internal void Tick(float now)
        {
            if (!_moving)
            {
                return;
            }
            float time = Mathf.Clamp01((now - _started) / _moveDuration);
            UpdateChannelPositions(time);
        }

        private void UpdateChannelPositions(float time)
        {
            float smooth = Mathf.SmoothStep(0, 1, time);
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i].Root.localPosition = Vector3.Lerp(_starts[i], _ends[i], smooth);
                if (time == 1 && !_expanded)
                {
                    _channels[i].Root.gameObject.SetActive(false);
                }
            }
            _moving = time < 1;
        }
    }
}
