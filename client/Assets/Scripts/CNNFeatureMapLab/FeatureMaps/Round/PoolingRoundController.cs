using System;
using System.Collections.Generic;
using System.Linq;
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

        private float _applyStarted;
        private int _shown;
        private int _size;
        private bool _applying;
        private FeatureMapPanelController _transferTarget;
        private Vector3 _transferOrigin;
        private int _transferCell;
        private int _transferChannel;
        private float _transferStarted;
        private bool _transferring;

        internal void Bind(CorridorRenderUtility visuals, FeatureMapDisplayController display)
        {
            _visuals = visuals;
            _display = display;
            _detail.Bind(visuals);
            _detail.Root.gameObject.SetActive(false);
        }

        internal void Show(FeatureMapSnapshot snapshot, int channel, PoolingCalculationResult data, LessonStep step, IReadOnlyCollection<MapId> revealed, Transform anchor)
        {
            _detail.Root.SetPositionAndRotation(anchor.position, anchor.rotation);
            _detail.Root.gameObject.SetActive(true);
            _applying = step == LessonStep.PoolingApply;
            _transferring = step == LessonStep.PoolingMaximum;
            _applyStarted = Time.unscaledTime;
            _size = snapshot.Size(2);
            _transferChannel = channel;
            _transferCell = data.OutputIndex;
            _shown = 0;
            FeatureMapPanelController source = _display.Map(1, channel);
            source.Highlight(data.InputIndex / snapshot.Size(1), data.InputIndex % snapshot.Size(1),
                data.InputIndex / snapshot.Size(1) + 1, data.InputIndex % snapshot.Size(1) + 1, _visuals.Theme.Amber);
            _detail.Badge($"계산 보기 · 채널 {channel + 1}", _visuals.Theme.Ink);
            _detail.ClearHighlight();
            float scale = snapshot.Scale(1, channel);
            if (step == LessonStep.PoolingWindow || step == LessonStep.PoolingApply)
            {
                int size = snapshot.Size(1);
                _detail.SetGridSize(size);
                _detail.UpdateMap(snapshot.Values(1, channel), scale);
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
                _detail.UpdateMap(_window, scale);
                if (step == LessonStep.PoolingMaximum)
                {
                    _detail.Cell(data.MaximumCell, _visuals.Theme.Yellow);
                }
            }
            for (int i = 0; i < snapshot.MapCount(2); i++)
            {
                FeatureMapPanelController panel = _display.Map(2, i);
                panel.ClearHighlight();
                panel.Frame(i == channel ? _visuals.Theme.Amber : _visuals.Theme.Cyan);
                bool showRepresentative = i == channel && (step is LessonStep.PoolingMaximum or LessonStep.PoolingApply);
                int shown = showRepresentative || (i != channel && !_applying && revealed.Contains(new MapId(2, i)))
                    ? int.MaxValue : 0;
                int only = showRepresentative ? data.OutputIndex : -1;
                panel.UpdateMap(snapshot.Values(2, i), snapshot.Scale(2, i), shown, only);
                if (i == channel && (step == LessonStep.PoolingMaximum || step == LessonStep.PoolingApply))
                {
                    panel.Cell(data.OutputIndex, _visuals.Theme.Yellow);
                }
            }
            if (_transferring)
            {
                _transferTarget = _display.Map(2, channel);
                _transferStarted = Time.unscaledTime;
                _transferOrigin = _detail.Root.TransformPoint(_detail.CellPosition(data.MaximumCell));
                _transferTarget.MoveCellHighlight(_transferCell, _transferOrigin, 0);
                _display.DrawResult(2, channel, 0);
            }
        }

        internal void Tick(float now)
        {
            if (_transferring)
            {
                float progress = Mathf.Clamp01((now - _transferStarted) / .65f);
                _transferTarget.MoveCellHighlight(_transferCell, _transferOrigin, Mathf.SmoothStep(0, 1, progress));
                if (progress == 1)
                {
                    _display.DrawResult(2, _transferChannel, int.MaxValue, _transferCell);
                    _transferring = false;
                }
            }
            if (!_applying) return;
            int count = _size * _size;
            int shown = Mathf.FloorToInt(Mathf.Clamp01((now - _applyStarted) / 2f) * count);
            if (shown == _shown) return;
            _shown = shown;
            for (int channel = 0; channel < 2; channel++)
            {
                _display.DrawResult(2, channel, shown, always: channel == _transferChannel ? _transferCell : -1);
            }
            _applying = shown < count;
        }

        internal void Close()
        {
            _applying = false;
            _transferring = false;
            _detail.Root.gameObject.SetActive(false);
            for (int channel = 0; channel < 2; channel++)
            {
                _display.Map(1, channel).ClearHighlight();
                FeatureMapPanelController panel = _display.Map(2, channel);
                panel.ClearHighlight();
                panel.Frame();
            }
        }

        internal void Dispose() => _detail.Dispose();
    }
}
