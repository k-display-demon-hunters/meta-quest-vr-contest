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
        [SerializeField] private Transform _root;
        [SerializeField] private InputPanelController _input;
        [SerializeField] private MapLayerView[] _layers;
        [SerializeField] private MapLinksController _links;
        [SerializeField] private OutputStageController _output;
        private FeatureMapPanelController[][] _maps;
        private CorridorRenderUtility _visuals;
        private FeatureMapSnapshot _snapshot;
        internal event Action<int> InputSelected;
        internal event Action<int> DigitSelected;
        internal event Action<int, int, int> MapSelected;
        internal event Action<int, int> SwitchSelected;
        internal event Action<int> OutputSelected;
        internal event Action<string> ButtonPressed;
        internal Vector3 InputPosition => _input.Picture.Position;
        internal int Winner => _output.Winner;
        internal int WinnerPercent => _output.WinnerPercent;
        internal void Bind(CorridorRenderUtility visuals, InteractionManager interaction, FeatureMapSnapshot snapshot)
        {
            _visuals = visuals;
            _input.Bind(visuals, interaction, index => InputSelected?.Invoke(index), digit => DigitSelected?.Invoke(digit));
            _maps = new FeatureMapPanelController[_layers.Length][];
            for (int l = 0;l<_layers.Length;l++)
            {
                int layer = l+1;
                _maps[l] = _layers[l].Panels;
                for (int m = 0;m<_maps[l].Length;m++)
                {
                    int map = m;
                    _maps[l][m].Bind(visuals, interaction, index => MapSelected?.Invoke(layer, map, index));
                    _maps[l][m].BindSwitch(interaction, () => SwitchSelected?.Invoke(layer, map));
                }
            }
            _output.Bind(visuals, interaction, _input, _maps[2], digit => OutputSelected?.Invoke(digit));
            var starts = new System.Collections.Generic.List<Vector3>();
            var ends = new System.Collections.Generic.List<Vector3>();
            for (int layer = 1;layer <= _maps.Length;layer++)
            {
                int count = layer == 1 ? 1 : _maps[layer-2].Length;
                for (int source = 0;source<count;source++)
                foreach (var target in _maps[layer-1])
                {
                    starts.Add(layer == 1 ? _input.Picture.Root.position : _maps[layer-2][source].Root.position);
                    ends.Add(target.Root.position);
                }
            }
            foreach (var source in _maps[2])
            for (int digit = 1;digit <= 3;digit++)
            {
                starts.Add(source.Root.position);
                ends.Add(_root.TransformPoint(_output.Tin(digit)));
            }
            _links.Bind(visuals, starts.ToArray(), ends.ToArray(), _root.up*.33f);
            ShowSnapshot(snapshot);
        }

        internal void Enable()
        {
            _input.ButtonPressed += OnButton;
            _input.Enable();
        }

        internal void Disable()
        {
            _input.ButtonPressed -= OnButton;
            _input.Disable();
        }

        private void OnButton(string text) => ButtonPressed?.Invoke(text);
        internal void SetCamera(Camera camera) => _input.SetCamera(camera);
        internal void ShowSnapshot(FeatureMapSnapshot snapshot)
        {
            _snapshot = snapshot;
            _input.Draw(snapshot.Input);
            for (int l = 0;l<_maps.Length;l++)
            for (int m = 0;m<_maps[l].Length;m++)
            {
                _maps[l][m].UpdateMap(snapshot.Values(l+1, m), snapshot.Scale(l+1, m), snapshot.IsOff(l+1, m));
            }
            _output.SetData(snapshot);
        }

        internal void Tick(float now, float delta)
        {
            _input.Tick(now);
            _links.Tick(now, _snapshot);
            _output.Tick(now);
            for (int l = 0;l<_maps.Length;l++)
            for (int m = 0;m<_maps[l].Length;m++)
            {
                _maps[l][m].AnimateSwitch(delta, !_snapshot.IsOff(l+1, m));
            }
        }

        internal FeatureMapPanelController Input => _input.Picture;
        internal FeatureMapPanelController Map(int layer, int map) => _maps[layer-1][map];
        internal FeatureMapPanelController[] Sources(int layer) => layer == 1 ? new[]
        {
            _input.Channel(0), _input.Channel(1), _input.Channel(2)
        }
        : _maps[layer-2];
        internal int MapCount(int layer) => _maps[layer-1].Length;
        internal Vector3 Destination(int layer, int map, int digit) => layer == 4 ? _output.Tin(digit)+Vector3.up*.4f : Map(layer, map).Position;
        internal Vector3 WaitingPosition(int layer, int digit) => layer == 4 ? _output.Tin(digit)+Vector3.up*.9f : Map(layer, 0).Position+Vector3.up*1.75f;
        internal Vector3 SwitchPosition(int layer, int map) => _root.InverseTransformPoint(Map(layer, map).Switch.position)+new Vector3(0, -.34f, -.2f);
        internal void HighlightLayer(int zone)
        {
            for (int l = 0;l<_maps.Length;l++) foreach (var panel in _maps[l])
            {
                panel.Frame(l+1 == zone ? _visuals.Theme.Cyan : (Color?)null);
            }
        }

        internal void ShowSwitches(bool visible)
        {
            foreach (var layer in _maps) foreach (var panel in layer)
            {
                panel.ShowSwitch(visible);
            }
        }

        internal void DimLinks(bool dimmed) => _links.Dimmed = dimmed;
        internal void Celebrate(int layer) => _links.Celebrate(layer);
        internal void ClearHighlights()
        {
            _input.Picture.ClearHighlight();
            foreach (var layer in _maps) foreach (var panel in layer)
            {
                panel.ClearHighlight();
            }
        }

        internal void ShowTourRange(int layer, int index, int size)
        {
            if (layer == 0)
            {
                _input.Picture.Cell(index, Color.white);
                return;
            }
            int row = index/size;
            int col = index%size;
            foreach (var panel in _maps[layer-1])
            {
                panel.Highlight(row-layer, col-layer, row+layer, col+layer, Color.white);
            }
        }

        internal void ShowTourStep(int stage, int row, int column, int size)
        {
            var source = stage == 1 ? _input.Picture : _maps[stage-2][0];
            source.Highlight(row-1, column-1, row+1, column+1, _visuals.Theme.Amber);
            Map(stage, 0).Cell(row*size+column, _visuals.Theme.Amber);
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
        internal string HoveredCell()
        {
            string cell = _input.Picture.HoverCell;
            foreach (var layer in _maps) foreach (var panel in layer) if (panel.HoverCell.Length>0)
            {
                cell = panel.HoverCell;
            }
            for (int i = 0;i<3;i++) if (_input.Channel(i).HoverCell.Length>0)
            {
                cell = _input.Channel(i).HoverCell;
            }
            return cell;
        }

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
