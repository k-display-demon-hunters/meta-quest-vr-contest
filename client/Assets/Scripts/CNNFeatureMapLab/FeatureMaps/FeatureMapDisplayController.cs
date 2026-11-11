using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class FeatureMapDisplayController
    {
        [Serializable] private sealed class MapLayerView
        {
            public FeatureMapPanelController[] Panels;
        }
        [SerializeField] private InputPanelController _input;
        [SerializeField] private MapLayerView[] _layers;
        [SerializeField] private OutputStageController _output;
        private FeatureMapPanelController[][] _maps;
        private CorridorRenderUtility _visuals;
        internal event Action<int> DigitSelected;
        internal event Action InputPictureSelected;
        internal Vector3 InputPictureHint => _input.Picture.Position + Vector3.up * 1.2f;
        internal void ShowInputChannels(bool expanded) => _input.SetExpanded(expanded);
        private FeatureMapSnapshot _snapshot;
        private readonly List<int> _outputInputs = new List<int>();
        private float _outputRevealStarted;
        private int _outputCellsShown;
        internal bool RevealingOutputInputs => _outputInputs.Count != 0;
        internal event Action OutputStarted;
        internal event Action<string> ButtonPressed;
        internal event Action<string> InputClicked;
        internal string HoverName
        {
            get
            {
                return _input.Picture.HoverName;
            }
        }
        internal void SetInputActive(bool active) => _input.Picture.SetInputActive(active);
        private void OnInputClicked(string name) => InputClicked?.Invoke(name);
        internal int Winner => _output.Winner;
        internal int WinnerPercent => _output.WinnerPercent;
        internal void Bind(CorridorRenderUtility visuals, FeatureMapSnapshot snapshot)
        {
            _visuals = visuals;
            _input.Bind(visuals, digit => DigitSelected?.Invoke(digit), () => InputPictureSelected?.Invoke());
            _maps = new FeatureMapPanelController[_layers.Length][];
            for (int l = 0;l<_layers.Length;l++)
            {
                int layer = l+1;
                _maps[l] = _layers[l].Panels;
                for (int m = 0;m<_maps[l].Length;m++)
                {
                    _maps[l][m].SetGridSize(snapshot.Size(layer));
                    _maps[l][m].Bind(visuals, showEmptyGrid: true);
                }
            }
            _output.Bind(visuals, _input, _maps[2], () => OutputStarted?.Invoke());
            ShowSnapshot(snapshot);
            RestoreResults(new HashSet<MapId>());
        }

        internal void Enable()
        {
            _input.ButtonPressed += OnButton;
            _input.Enable();
            _input.Picture.InputClicked += OnInputClicked;
            _output.Enable();
        }

        internal void Disable()
        {
            _input.ButtonPressed -= OnButton;
            _input.Disable();
            _input.Picture.InputClicked -= OnInputClicked;
            _output.Disable();
        }

        private void OnButton(string text) => ButtonPressed?.Invoke(text);
        internal void SetCamera(Camera camera) => _input.SetCamera(camera);
        internal void ShowSnapshot(FeatureMapSnapshot snapshot)
        {
            _snapshot = snapshot;
            _outputInputs.Clear();
            _input.Draw(snapshot.Input);
            _output.SetData(snapshot);
        }

        internal void RestoreResults(IReadOnlyCollection<MapId> revealed)
        {
            _outputInputs.Clear();
            for (int layer = 1; layer <= _maps.Length; layer++)
            for (int map = 0; map < _snapshot.MapCount(layer); map++)
            {
                bool complete = revealed.Contains(new MapId(layer, map));
                DrawResult(layer, map, complete ? int.MaxValue : 0);
            }
        }

        internal void DrawResult(int layer, int map, int shown, int only = -1, int always = -1) =>
            Map(layer, map).UpdateMap(_snapshot.Values(layer, map), _snapshot.Scale(layer, map), shown, only, always);

        internal void RevealOutputInput(int map)
        {
            _outputInputs.Add(map);
            _outputRevealStarted = Time.unscaledTime;
            _outputCellsShown = -1;
        }

        private void UpdateOutputInputs(float now)
        {
            if (!RevealingOutputInputs) return;
            int count = _snapshot.Size(3) * _snapshot.Size(3);
            int shown = Mathf.FloorToInt(Mathf.Clamp01(now - _outputRevealStarted) * count);
            if (shown == _outputCellsShown) return;
            _outputCellsShown = shown;
            foreach (int map in _outputInputs)
            {
                DrawResult(3, map, shown);
            }
            if (shown == count) _outputInputs.Clear();
        }

        internal void Tick(float now)
        {
            _input.Tick(now);
            _output.Tick(now);
            UpdateOutputInputs(now);
        }

        internal FeatureMapPanelController Map(int layer, int map) => _maps[layer-1][map];
        internal FeatureMapPanelController[] Sources(int layer) => layer == 1 ? new[]
        {
            _input.Channel(0), _input.Channel(1), _input.Channel(2)
        }
        : _maps[layer-2];
        internal Vector3 Destination(int layer, int map, int digit) => layer == 4 ? _output.Tin(digit)+Vector3.up*.4f : Map(layer, map).Position;
        internal void SetOutputStartAvailable(bool available) => _output.SetStartAvailable(available);
        internal void HighlightLayer(int zone)
        {
            for (int l = 0;l<_maps.Length;l++) foreach (var panel in _maps[l])
            {
                panel.Frame(l+1 == zone ? _visuals.Theme.Cyan : (Color?)null);
            }
        }

        internal void ShowRound(RoundCalculationResult data, LessonStep step)
        {
            _input.MarkChannels(data.Layer == 1 && step != LessonStep.FilterIntro);

            if (!data.IsOutput)
            {
                Map(data.Layer, data.Map).Frame(_visuals.Theme.Amber);
                if (step is LessonStep.Merge)
                {
                    Map(data.Layer, data.Map).Cell(data.Index, _visuals.Theme.Yellow);
                }
                else
                {
                    Map(data.Layer, data.Map).ClearHighlight();
                }
            }
            var sources = Sources(data.Layer);
            var colors = data.Layer == 1 ? CorridorTheme.Rgb : CorridorTheme.Pairs;
            for (int i = 0;i<sources.Length;i++)
            {
                sources[i].Frame(colors[i]);
                if (data.Layer != 1)
                {
                    sources[i].Badge(FeatureMapLabels.Marks[i]+" "+FeatureMapLabels.MapId(data.Layer-1, i), colors[i]);
                }
                if (!data.IsOutput && step != LessonStep.Scan)
                {
                    int row = data.Index/data.Size;
                    int col = data.Index%data.Size;
                    sources[i].Highlight(row - 1, col - 1, row + 1, col + 1, colors[i]);
                }
                else if (!data.IsOutput)
                {
                    sources[i].ClearHighlight();
                }
            }
        }

        internal void CloseRound(RoundCalculationResult data)
        {
            _input.MarkChannels(false);

            if (!data.IsOutput)
            {
                Map(data.Layer, data.Map).ClearHighlight();
                Map(data.Layer, data.Map).Frame();
            }
            foreach (var source in Sources(data.Layer))
            {
                source.ClearHighlight();
                if (data.Layer != 1)
                {
                    source.Frame();
                    source.Badge("", _visuals.Theme.Dim);
                }
            }
        }

        internal void ResetOutput() => _output.Reset();
        internal void StartMatch() => _output.StartMatch();
        internal void FinishMatch() => _output.FinishMatch();
        internal void StartFill() => _output.StartFill(true);
        internal void FinishFill() => _output.FinishFill();
        internal void Fly() => _output.Fly(true);
        internal void CompleteOutput() => _output.Complete();
        internal void Dispose()
        {
            _input.Dispose();
            foreach (var layer in _maps) foreach (var panel in layer)
            {
                panel.Dispose();
            }
        }
    }
}
