using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class PoolingRoundController
    {
        [SerializeField] private FeatureMapPanelController _detail;
        private FeatureMapDisplayController _display;
        private CorridorRenderUtility _visuals;
        private readonly float[] _window = new float[4];

        internal void Bind(CorridorRenderUtility visuals, FeatureMapDisplayController display)
        {
            _visuals = visuals;
            _display = display;
            _detail.Bind(visuals);
            _detail.Root.gameObject.SetActive(false);
        }

        internal void Show(FeatureMapSnapshot snapshot, int channel, PoolingCalculationResult data, int stage)
        {
            _detail.Root.gameObject.SetActive(true);
            _detail.Badge($"계산 보기 · 채널 {channel + 1}", _visuals.Theme.Ink);
            _detail.ClearHighlight();
            float scale = snapshot.Scale(1, channel);
            if (stage == 0 || stage == 3)
            {
                int size = snapshot.Size(1);
                _detail.SetGridSize(size);
                _detail.UpdateMap(snapshot.Values(1, channel), scale, snapshot.IsOff(1, channel));
                int row = data.InputIndex / size;
                int column = data.InputIndex % size;
                _detail.Highlight(row, column, row + 1, column + 1, _visuals.Theme.Amber);
            }
            else
            {
                _window[0] = data.TopLeft;
                _window[1] = data.TopRight;
                _window[2] = data.BottomLeft;
                _window[3] = data.BottomRight;
                _detail.SetGridSize(2);
                _detail.UpdateMap(_window, scale, false);
                if (stage == 2)
                {
                    _detail.Cell(data.MaximumCell, _visuals.Theme.Yellow);
                }
            }
            for (int i = 0; i < snapshot.MapCount(2); i++)
            {
                FeatureMapPanelController panel = _display.Map(2, i);
                panel.ClearHighlight();
                panel.Frame(i == channel ? _visuals.Theme.Amber : _visuals.Theme.Cyan);
                int shown = stage == 3 || (stage == 2 && i == channel) ? int.MaxValue : 0;
                int only = stage == 2 ? data.OutputIndex : -1;
                panel.UpdateMap(snapshot.Values(2, i), snapshot.Scale(2, i), false, shown, only);
                if (i == channel && stage >= 2)
                {
                    panel.Cell(data.OutputIndex, _visuals.Theme.Yellow);
                }
            }
        }

        internal void Close(FeatureMapSnapshot snapshot)
        {
            _detail.Root.gameObject.SetActive(false);
            for (int channel = 0; channel < snapshot.MapCount(2); channel++)
            {
                FeatureMapPanelController panel = _display.Map(2, channel);
                panel.ClearHighlight();
                panel.Frame();
                panel.UpdateMap(snapshot.Values(2, channel), snapshot.Scale(2, channel), snapshot.IsOff(2, channel));
            }
        }

        internal void Dispose() => _detail.Dispose();
    }
}
