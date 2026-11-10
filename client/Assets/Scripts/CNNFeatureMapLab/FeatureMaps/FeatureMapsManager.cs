using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class FeatureMapsManager
    {
        [SerializeField] private CorridorRenderUtility _visuals;
        [SerializeField] private FeatureMapDisplayController _display;
        [SerializeField] private InteractionManager _interaction;
        [SerializeField] private PoolingRoundController _pooling;
        [SerializeField] private FeatureMapRoundController _round;
        private Camera _camera;
        private bool _roundActive;
        private bool _waitingForFilter, _filterPending;
        internal event Action<int> DigitSelected;
        internal event Action<int, int> MapSelected;
        internal event Action<int> OutputSelected;
        internal event Action<int> ZoneSelected;
        internal event Action RoundChanged;
        internal event Action<string> ButtonPressed;
        internal int RoundLayer => _round.Layer;
        internal int RoundMap => _round.Map;
        internal int RoundStage => _round.Stage;
        internal bool RoundIsOutput => _round.IsOutput;
        internal bool CanAdvanceRound => !_waitingForFilter && !_filterPending;
        internal string HoverName => _interaction.HoverName;
        internal string LastClick => _interaction.LastClick;
        internal Vector3 WaitingHint(int layer, int map, int digit) => _display.WaitingPosition(layer, map, digit);
        internal void Initialize(FeatureMapSnapshot snapshot)
        {
            _visuals.InitializeRuntime();
            _interaction.Initialize();
            _display.Bind(_visuals, _interaction, snapshot);
            _pooling.Bind(_visuals, _display);
            _round.Bind(_visuals, _interaction, _display);
        }

        internal void Enable()
        {
            _display.DigitSelected += OnDigit;
            _display.MapSelected += OnMap;
            _display.OutputSelected += OnOutput;
            _display.ButtonPressed += OnButton;
            _interaction.ZoneSelected += OnZone;
            _round.FilterPressed += OnFilter;
            _display.Enable();
            _round.EnableInput();
        }

        internal void Disable()
        {
            _interaction.SetActive(false);
            _display.Disable();
            _round.DisableInput();
            _display.DigitSelected -= OnDigit;
            _display.MapSelected -= OnMap;
            _display.OutputSelected -= OnOutput;
            _display.ButtonPressed -= OnButton;
            _interaction.ZoneSelected -= OnZone;
            _round.FilterPressed -= OnFilter;
        }

        private void OnDigit(int digit) => DigitSelected?.Invoke(digit);
        private void OnMap(int layer, int map) => MapSelected?.Invoke(layer, map);
        private void OnOutput(int digit) => OutputSelected?.Invoke(digit);
        private void OnZone(int zone) => ZoneSelected?.Invoke(zone);
        private void OnButton(string caption) => ButtonPressed?.Invoke(caption);
        private void OnFilter(bool expanded)
        {
            if (!_roundActive || _round.Stage != 0 || !expanded)
            {
                return;
            }
            _waitingForFilter = false;
            _filterPending = true;
            _round.Show(1, true);
            RoundChanged?.Invoke();
        }

        internal void SetVisible(bool visible)
        {
            _interaction.SetActive(visible);
            if (visible)
            {
                _camera = Camera.main;
                _display.SetCamera(_camera);
            }
        }

        internal void Tick(float now)
        {
            _interaction.UpdateHover();
            _display.Tick(now);
            _visuals.UpdateBillboards(_camera);
            if (!_roundActive)
            {
                return;
            }
            _round.Tick(now);
            if (_filterPending && now >= _round.ButtonReadyAt)
            {
                _filterPending = false;
                RoundChanged?.Invoke();
            }
        }

        internal void ShowSnapshot(FeatureMapSnapshot snapshot) => _display.ShowSnapshot(snapshot);
        internal void Highlight(int layer) => _display.HighlightLayer(layer);
        internal void ShowPooling(FeatureMapSnapshot snapshot, int channel, PoolingCalculationResult data, int stage) =>
            _pooling.Show(snapshot, channel, data, stage);

        internal void ClosePooling(FeatureMapSnapshot snapshot) => _pooling.Close(snapshot);

        internal void StartRound(RoundCalculationResult data)
        {
            _roundActive = true;
            _waitingForFilter = true;
            _filterPending = false;
            _round.Start(data);
        }

        internal void RestartRound(RoundCalculationResult data)
        {
            _roundActive = true;
            int stage = _round.Stage;
            _round.Start(data);
            if (data.IsOutput && stage >= 7)
            {
                _display.StartMatch();
                _display.FinishMatch();
            }
            if (data.IsOutput && stage >= 8)
            {
                _display.StartFill();
                _display.FinishFill();
            }
            _waitingForFilter = stage == 0;
            _filterPending = false;
            _round.Show(stage);
            _round.FoldFilters();
        }

        internal bool AdvanceRound() => _round.Advance();
        internal bool PreviousRound()
        {
            if (!_round.Previous())
            {
                return false;
            }
            _waitingForFilter = _round.Stage == 0;
            _filterPending = false;
            return true;
        }

        internal void ReplayLastStep()
        {
            if (_round.IsOutput)
            {
                _display.StartMatch();
                _display.FinishMatch();
                _display.StartFill();
            }
            _waitingForFilter = false;
            _filterPending = false;
            _round.Show(_round.LastStage);
        }

        internal void CloseRound()
        {
            _roundActive = false;
            _round.Close();
        }

        internal System.Collections.Generic.IReadOnlyList<RecapInfo> Recap()
        {
            if (_round.IsOutput)
            {
                _display.CompleteOutput();
            }
            var data = _round.Data;
            var recaps = new System.Collections.Generic.List<RecapInfo>();
            if (data.IsOutput)
            {
                Vector3 destination = _round.Destination;
                recaps.Add(new RecapInfo(new Vector3(destination.x, 4.5f, destination.z-.7f), 4, 0, data.Digit, data.Count));
            }
            else
            {
                var position = _display.Map(data.Layer, data.Map).Position + Vector3.up * 1.6f;
                recaps.Add(new RecapInfo(position, data.Layer, data.Map, data.Digit, data.Count));
            }
            CloseRound();
            return recaps.AsReadOnly();
        }

        internal LessonContext DescribeRound(bool mandatory)
        {
            return new LessonContext
            {
                Layer = _round.Layer, Map = _round.Map, Stage = _round.Stage, Mandatory = mandatory, WaitingForFilter = _waitingForFilter, FilterPending = _filterPending, HintPosition = _round.FilterPosition+Vector3.up*.8f, Calculation = _round.Data, Winner = _display.Winner, WinnerPercent = _display.WinnerPercent
            };
        }

        internal void Dispose()
        {
            _display.Dispose();
            _round.Dispose();
            _pooling.Dispose();
        }
    }
}
