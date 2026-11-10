using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class FeatureMapsManager
    {
        [SerializeField] private CorridorRenderUtility _visuals;
        [SerializeField] private FeatureMapDisplayController _display;
        [SerializeField] private PoolingRoundController _pooling;
        [SerializeField] private FeatureMapRoundController _round;
        private Camera _camera;
        private bool _roundActive;
        internal event Action<int> DigitSelected;
        internal event Action<int, int> MapSelected;
        internal event Action<int> OutputSelected;
        internal event Action FilterExpanded;
        internal event Action PresentationReady;
        internal event Action<string> ButtonPressed;
        internal string HoverName => _display.HoverName ?? _round.HoverName ?? "—";
        internal string LastClick { get; private set; } = "—";
        private void OnInputClicked(string name) => LastClick = name;
        internal Vector3 WaitingHint(int layer, int map, int digit) => _display.WaitingPosition(layer, map, digit);
        internal void Initialize(FeatureMapSnapshot snapshot)
        {
            _visuals.InitializeRuntime();
            _display.Bind(_visuals, snapshot);
            _pooling.Bind(_visuals, _display);
            _round.Bind(_visuals, _display);
        }

        internal void Enable()
        {
            _display.DigitSelected += OnDigit;
            _display.MapSelected += OnMap;
            _display.OutputSelected += OnOutput;
            _display.ButtonPressed += OnButton;
            _round.FilterPressed += OnFilter;
            _round.PresentationReady += OnPresentationReady;
            _display.Enable();
            _round.EnableInput();
            _display.InputClicked += OnInputClicked;
            _round.InputClicked += OnInputClicked;
        }

        internal void Disable()
        {
            SetInputActive(false);
            _display.Disable();
            _round.DisableInput();
            _display.InputClicked -= OnInputClicked;
            _round.InputClicked -= OnInputClicked;
            _display.DigitSelected -= OnDigit;
            _display.MapSelected -= OnMap;
            _display.OutputSelected -= OnOutput;
            _display.ButtonPressed -= OnButton;
            _round.FilterPressed -= OnFilter;
            _round.PresentationReady -= OnPresentationReady;
        }

        private void OnDigit(int digit) => DigitSelected?.Invoke(digit);
        private void OnMap(int layer, int map) => MapSelected?.Invoke(layer, map);
        private void OnOutput(int digit) => OutputSelected?.Invoke(digit);
        private void OnButton(string caption) => ButtonPressed?.Invoke(caption);
        private void OnPresentationReady() => PresentationReady?.Invoke();
        private void OnFilter(bool expanded)
        {
            if (_roundActive && expanded)
            {
                FilterExpanded?.Invoke();
            }
        }

        private static RoundStepSequence RulesFor(MapId round) => round.Layer switch
        {
            2 => PoolingRoundRules.Sequence,
            4 => OutputRoundRules.Sequence,
            _ => ConvRoundRules.ForMap(round.Map)
        };

        internal LessonStep FirstStep(MapId round) => RulesFor(round).First;
        internal LessonStep LastStep(MapId round) => RulesFor(round).Last;
        internal RoundTransition EvaluateTransition(MapId round, LessonStep step, RoundRequest request) =>
            RulesFor(round).Evaluate(step, request, _round.PresentationPending);

        private void SetInputActive(bool active)
        {
            _display.SetInputActive(active);
            _round.SetInputActive(active);
        }

        internal void SetVisible(bool visible)
        {
            SetInputActive(visible);
            if (visible)
            {
                _camera = Camera.main;
                _display.SetCamera(_camera);
            }
        }

        internal void Tick(float now)
        {
            _display.Tick(now);
            _visuals.UpdateBillboards(_camera);
            if (!_roundActive)
            {
                return;
            }
            _round.Tick(now);
        }

        internal void ShowSnapshot(FeatureMapSnapshot snapshot) => _display.ShowSnapshot(snapshot);
        internal void Highlight(int layer) => _display.HighlightLayer(layer);
        internal void ShowPooling(FeatureMapSnapshot snapshot, int channel, PoolingCalculationResult data, LessonStep step) =>
            _pooling.Show(snapshot, channel, data, step);

        internal void PrepareRound(RoundCalculationResult data)
        {
            _roundActive = true;
            _round.Prepare(data);
        }

        internal void ShowRoundStep(LessonStep step, bool filterInput) => _round.Show(step, filterInput);

        internal void FinishRoundStep(LessonStep step) => _round.FinishStep(step);

        internal void CloseRound(MapId round, FeatureMapSnapshot snapshot)
        {
            if (round.Layer == 2)
            {
                _pooling.Close(snapshot);
                return;
            }

            ClosePresentation();
        }

        private void ClosePresentation()
        {
            _roundActive = false;
            _round.Close();
        }

        internal RecapInfo FinishRoundForRecap()
        {
            if (_round.IsOutput)
            {
                _display.CompleteOutput();
            }
            var data = _round.Data;
            RecapInfo recap;
            if (data.IsOutput)
            {
                Vector3 destination = _round.Destination;
                recap = new RecapInfo(new Vector3(destination.x, 4.5f, destination.z-.7f), 4, 0, data.Digit, data.Count);
            }
            else
            {
                var position = _display.Map(data.Layer, data.Map).Position + Vector3.up * 1.6f;
                recap = new RecapInfo(position, data.Layer, data.Map, data.Digit, data.Count);
            }
            ClosePresentation();
            return recap;
        }

        internal LessonContext DescribeRound(MapId round, LessonStep step)
        {
            return new LessonContext
            {
                WaitingForFilter = RoundStepSequence.WaitsForFilter(step),
                FilterPending = _round.PresentationPending,
                IsLastStep = step == LastStep(round),
                HintPosition = _round.FilterPosition + Vector3.up * .8f,
                Calculation = _round.Data,
                Winner = _display.Winner,
                WinnerPercent = _display.WinnerPercent
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
