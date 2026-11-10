using System;
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
        internal event Action<int, int> MapSelected;
        internal event Action<int> OutputSelected;
        internal event Action<string> ButtonPressed;
        internal event Action<string> InputClicked;
        internal string HoverName
        {
            get
            {
                foreach (var layer in _maps) foreach (var panel in layer)
                    if (panel.HoverName != null) return panel.HoverName;
                return _output.HoverName;
            }
        }
        internal void SetInputActive(bool active)
        {
            foreach (var layer in _maps) foreach (var panel in layer) panel.SetInputActive(active);
            _output.SetInputActive(active);
        }
        private void OnInputClicked(string name) => InputClicked?.Invoke(name);
        internal int Winner => _output.Winner;
        internal int WinnerPercent => _output.WinnerPercent;
        internal void Bind(CorridorRenderUtility visuals, FeatureMapSnapshot snapshot)
        {
            _visuals = visuals;
            _input.Bind(visuals, digit => DigitSelected?.Invoke(digit));
            _maps = new FeatureMapPanelController[_layers.Length][];
            for (int l = 0;l<_layers.Length;l++)
            {
                int layer = l+1;
                _maps[l] = _layers[l].Panels;
                for (int m = 0;m<_maps[l].Length;m++)
                {
                    int map = m;
                    _maps[l][m].Bind(visuals, () => MapSelected?.Invoke(layer, map));
                    _maps[l][m].SetGridSize(snapshot.Size(layer));
                }
            }
            _output.Bind(visuals, _input, _maps[2], digit => OutputSelected?.Invoke(digit));
            ShowSnapshot(snapshot);
        }

        internal void Enable()
        {
            _input.ButtonPressed += OnButton;
            _input.Enable();
            foreach (var layer in _maps) foreach (var panel in layer) panel.InputClicked += OnInputClicked;
            _output.InputClicked += OnInputClicked;
        }

        internal void Disable()
        {
            _input.ButtonPressed -= OnButton;
            _input.Disable();
            foreach (var layer in _maps) foreach (var panel in layer) panel.InputClicked -= OnInputClicked;
            _output.InputClicked -= OnInputClicked;
        }

        private void OnButton(string text) => ButtonPressed?.Invoke(text);
        internal void SetCamera(Camera camera) => _input.SetCamera(camera);
        internal void ShowSnapshot(FeatureMapSnapshot snapshot)
        {
            _input.Draw(snapshot.Input);
            for (int l = 0;l<_maps.Length;l++)
            for (int m = 0;m<_maps[l].Length;m++)
            {
                _maps[l][m].UpdateMap(snapshot.Values(l+1, m), snapshot.Scale(l+1, m));
            }
            _output.SetData(snapshot);
        }

        internal void Tick(float now)
        {
            _input.Tick(now);
            _output.Tick(now);
        }

        internal FeatureMapPanelController Map(int layer, int map) => _maps[layer-1][map];
        internal FeatureMapPanelController[] Sources(int layer) => layer == 1 ? new[]
        {
            _input.Channel(0), _input.Channel(1), _input.Channel(2)
        }
        : _maps[layer-2];
        internal Vector3 Destination(int layer, int map, int digit) => layer == 4 ? _output.Tin(digit)+Vector3.up*.4f : Map(layer, map).Position;
        internal Vector3 WaitingPosition(int layer, int map, int digit) => layer == 4 ? _output.Tin(digit)+Vector3.up*.9f : Map(layer, map).Position+Vector3.up*1.75f;
        internal void HighlightLayer(int zone)
        {
            for (int l = 0;l<_maps.Length;l++) foreach (var panel in _maps[l])
            {
                panel.Frame(l+1 == zone ? _visuals.Theme.Cyan : (Color?)null);
            }
        }

        internal void ShowRound(RoundCalculationResult data, int stage)
        {
            _input.MarkChannels(data.Layer == 1 && stage >= 1);
            if (data.Layer == 1 && stage >= 1)
            {
                _input.SetExpanded(true);
            }
            if (!data.IsOutput)
            {
                Map(data.Layer, data.Map).Frame(_visuals.Theme.Amber);
                Map(data.Layer, data.Map).Cell(data.Index, _visuals.Theme.Yellow);
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
                if (!data.IsOutput)
                {
                    int row = data.Index/data.Size;
                    int col = data.Index%data.Size;
                    sources[i].Highlight(row-1, col-1, row+1, col+1, Color.white);
                }
            }
        }

        internal void CloseRound(RoundCalculationResult data)
        {
            _input.MarkChannels(false);
            if (data.Layer == 1)
            {
                _input.SetExpanded(false);
            }
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
