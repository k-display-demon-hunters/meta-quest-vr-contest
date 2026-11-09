using System.Collections.Generic;
using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class InputPanelController
    {
        [SerializeField] private FeatureMapPanelController[] _channels = new FeatureMapPanelController[3];
        private Vector3[] _starts = new Vector3[3];
        private Vector3[] _ends = new Vector3[3];
        private float _started;
        private bool _expanded;
        private bool _moving;
        [SerializeField] private FeatureMapPanelController _picture;
        internal FeatureMapPanelController Picture => _picture;
        internal FeatureMapPanelController Channel(int index) => _channels[index];
        [SerializeField] private UI.WorldUIButtonController[] _digitButtons = new UI.WorldUIButtonController[3];
        [SerializeField] private Transform[] _expandedAnchors = new Transform[3];
        internal void Bind(CorridorRenderUtility visuals, InteractionManager interaction, Action<int> pixel, Action<int> digit)
        {
            Action<int> select = index =>
            {
                SetExpanded(true);
                pixel(index);
            };
            Picture.Bind(visuals, interaction, select, () => SetExpanded(false));
            for (int i = 0; i < 3; i++)
            {
                int number = i + 1;
                _channels[i].Bind(visuals, interaction, select, () => SetExpanded(false));
                _ends[i] = _expandedAnchors[i].localPosition;
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
            if (!_moving)
            {
                return;
            }
            float time = Mathf.Clamp01((now-_started)/.4f);
            float smooth = Mathf.SmoothStep(0, 1, time);
            for (int i = 0; i < 3; i++)
            {
                _channels[i].Root.localPosition = Vector3.Lerp(_starts[i], _expanded ? _ends[i] : Picture.Position, smooth);
                if (time == 1 && !_expanded)
                {
                    _channels[i].Root.gameObject.SetActive(false);
                }
            }
            _moving = time < 1;
        }
    }
}
