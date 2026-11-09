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
        [SerializeField] private ReceptiveFieldTourController _tour;
        [SerializeField] private FeatureMapRoundController _round;
        private Camera _camera;
        private bool _tourActive, _roundActive;
        private bool _waitingForFilter, _filterPending;
        internal event Action<int> InputSelected;
        internal event Action<int> DigitSelected;
        internal event Action<int, int, int> MapSelected;
        internal event Action<int, int> SwitchSelected;
        internal event Action<int> OutputSelected;
        internal event Action<int> ZoneSelected;
        internal event Action RoundChanged;
        internal event Action<string> ButtonPressed;
        internal int RoundLayer => _round.Layer;
        internal int RoundMap => _round.Map;
        internal int RoundIndex => _round.Index;
        internal int RoundStage => _round.Stage;
        internal bool RoundIsOutput => _round.IsOutput;
        internal bool CanAdvanceRound => !_waitingForFilter && !_filterPending;
        internal string HoverName => _interaction.HoverName;
        internal string LastClick => _interaction.LastClick;
        internal string HoveredCell => _display.HoveredCell();
        internal Vector3 InputHint => _display.InputPosition+Vector3.up*1.1f;
        internal Vector3 WaitingHint(int layer, int digit) => _display.WaitingPosition(layer, digit);
        internal Vector3 SwitchHint(int layer, int map) => _display.SwitchPosition(layer, map);
        internal void Initialize(FeatureMapSnapshot snapshot)
        {
            _visuals.InitializeRuntime();
            _interaction.Initialize();
            _display.Bind(_visuals, _interaction, snapshot);
            _tour.Bind(_visuals, _display, snapshot.Size);
            _round.Bind(_visuals, _interaction, _display);
        }

        internal void Enable()
        {
            _display.InputSelected += OnInput;
            _display.DigitSelected += OnDigit;
            _display.MapSelected += OnMap;
            _display.SwitchSelected += OnSwitch;
            _display.OutputSelected += OnOutput;
            _display.ButtonPressed += OnButton;
            _interaction.ZoneSelected += OnZone;
            _round.FilterPressed += OnFilter;
            _interaction.Enable();
            _display.Enable();
            _round.EnableInput();
        }

        internal void Disable()
        {
            _interaction.Disable();
            _display.Disable();
            _round.DisableInput();
            _display.InputSelected -= OnInput;
            _display.DigitSelected -= OnDigit;
            _display.MapSelected -= OnMap;
            _display.SwitchSelected -= OnSwitch;
            _display.OutputSelected -= OnOutput;
            _display.ButtonPressed -= OnButton;
            _interaction.ZoneSelected -= OnZone;
            _round.FilterPressed -= OnFilter;
        }

        private void OnInput(int index) => InputSelected?.Invoke(index);
        private void OnDigit(int digit) => DigitSelected?.Invoke(digit);
        private void OnMap(int layer, int map, int index) => MapSelected?.Invoke(layer, map, index);
        private void OnSwitch(int layer, int map) => SwitchSelected?.Invoke(layer, map);
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
            if (visible)
            {
                _camera = Camera.main;
                _display.SetCamera(_camera);
            }
            else
            {
                _interaction.Suspend();
            }
        }

        internal void Tick(float now, float delta)
        {
            _interaction.Tick(_camera);
            _display.Tick(now, delta);
            _visuals.UpdateBillboards(_camera);
            if (_tourActive)
            {
                _tour.Tick(now);
            }
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
        internal void ShowSwitches(bool visible) => _display.ShowSwitches(visible);
        internal void Celebrate(int layer) => _display.Celebrate(layer);
        internal void ShowTour(int stage, int index)
        {
            _roundActive = false;
            _tourActive = true;
            _tour.Show(stage, index);
        }

        internal void FinishTour() => _tour.Finish();
        internal void ClearTour()
        {
            _tourActive = false;
            _tour.Clear();
        }

        internal void StartRound(RoundCalculationResult data)
        {
            ClearTour();
            _roundActive = true;
            _waitingForFilter = true;
            _filterPending = false;
            _display.DimLinks(true);
            _round.Start(data);
        }

        internal void RestartRound(RoundCalculationResult data)
        {
            _roundActive = true;
            _display.DimLinks(true);
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

        internal void CompleteOutputForRecap()
        {
            _display.StartMatch();
            _display.FinishMatch();
            _display.StartFill();
            _display.FinishFill();
            _display.Fly();
        }

        internal void CloseRound()
        {
            _roundActive = false;
            _round.Close();
            _display.DimLinks(false);
        }

        internal System.Collections.Generic.IReadOnlyList<RecapInfo> Recap(bool mandatory)
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
            for (int i = 0;i<_display.MapCount(data.Layer);i++)
            if (mandatory || i == data.Map)
            {
                recaps.Add(new RecapInfo(_display.Map(data.Layer, i).Position+Vector3.up*1.6f, data.Layer, i, data.Digit, data.Count));
            }
            CloseRound();
            return recaps.AsReadOnly();
        }

        internal LessonContext DescribeRound(bool mandatory)
        {
            return new LessonContext
            {
                Layer = _round.Layer, Map = _round.Map, Stage = _round.Stage, MapCount = _round.IsOutput ? 0 : _display.MapCount(_round.Layer), Mandatory = mandatory, WaitingForFilter = _waitingForFilter, FilterPending = _filterPending, HintPosition = _round.FilterPosition+Vector3.up*.8f, Calculation = _round.Data, Winner = _display.Winner, WinnerPercent = _display.WinnerPercent
            };
        }

        internal void Dispose()
        {
            _interaction.Dispose();
            _display.Dispose();
            _round.Dispose();
            _tour.Dispose();
        }
    }
}
