using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class FeatureMapRoundController
    {
        [SerializeField] private FilterStackController _filter;
        [SerializeField] private RoundResultsController _results;
        private FeatureMapDisplayController _display;
        private RoundCalculationResult _data;
        private float _started;
        private Vector3 _destination;
        internal Vector3 Destination => _destination;
        internal int Layer => _data.Layer;
        internal int Map => _data.Map;
        internal int Stage
        {
            get;
            private set;
        }

        internal bool IsOutput => _data.IsOutput;
        internal int LastStage => IsOutput ? 8 : 5;
        private bool SkipCellBreakdown => IsOutput || Map == 1;
        internal RoundCalculationResult Data => _data;
        internal Vector3 FilterPosition => _filter.Root.localPosition;
        internal float ButtonReadyAt
        {
            get;
            private set;
        }

        internal event Action<bool> FilterPressed;
        internal void Bind(CorridorRenderUtility visuals, FeatureMapDisplayController display)
        {
            _display = display;
            _filter.Bind(visuals);
            _results.Bind(visuals, _filter);
        }

        internal string HoverName => _filter.HoverName;
        internal event Action<string> InputClicked;
        internal void SetInputActive(bool active) => _filter.SetInputActive(active);
        private void OnInputClicked(string name) => InputClicked?.Invoke(name);
        internal void EnableInput()
        {
            _filter.ExpansionChanged += OnFilter;
            _filter.InputClicked += OnInputClicked;
        }
        internal void DisableInput()
        {
            _filter.ExpansionChanged -= OnFilter;
            _filter.InputClicked -= OnInputClicked;
        }
        private void OnFilter(bool expanded) => FilterPressed?.Invoke(expanded);
        internal void Start(RoundCalculationResult data)
        {
            _data = data;
            _destination = _display.Destination(Layer, Map, data.Digit);
            _filter.Configure(data, _display.Sources(Layer), IsOutput ? null : _display.Map(Layer, Map), _destination);
            _results.Configure(data, _display);
            Show(0);
        }

        internal void Show(int stage, bool filterInput = false)
        {
            Stage = stage;
            _started = Time.unscaledTime;
            ButtonReadyAt = stage == 1 && filterInput ? _started+.7f : _started;
            if (IsOutput && stage <= 6)
            {
                _display.ResetOutput();
            }
            _filter.ShowCards(stage <= 4);
            if (!filterInput)
            {
                _filter.SetExpanded(stage>0, true);
            }
            _filter.DimDuringScan(stage == 2);
            _display.ShowRound(_data, stage);
            _results.Show(stage);
            if (stage == 6)
            {
                _display.StartMatch();
            }
            if (stage == 7)
            {
                _display.FinishMatch();
                _display.StartFill();
            }
            if (stage == 8)
            {
                _display.FinishFill();
                _display.Fly();
            }
        }

        internal void Tick(float now)
        {
            _filter.Tick(now);
            _results.Tick(Stage, now-_started);
        }

        internal void FoldFilters() => _filter.SetExpanded(false, true);
        internal void FinishStage()
        {
            _results.Finish(Stage);
            if (Stage == 6)
            {
                _display.FinishMatch();
            }
            if (Stage == 7)
            {
                _display.FinishFill();
            }
            if (Stage == 8)
            {
                _display.CompleteOutput();
            }
        }

        internal bool Advance()
        {
            FinishStage();
            if (Stage == LastStage)
            {
                return false;
            }
            Show(SkipCellBreakdown && Stage == 3 ? 5 : Stage+1);
            return true;
        }

        internal bool Previous()
        {
            if (Stage == 0)
            {
                return false;
            }
            Show(SkipCellBreakdown && Stage == 5 ? 3 : Stage-1);
            return true;
        }

        internal void Close()
        {
            _filter.Hide();
            _display.CloseRound(_data);
            _results.Hide();
        }

        internal void Dispose()
        {
            _filter.Dispose();
            _results.Dispose();
        }
    }
}
