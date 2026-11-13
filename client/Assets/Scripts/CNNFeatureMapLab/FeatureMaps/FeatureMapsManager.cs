using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private OperationDeviceController[] _firstFilters;
        [SerializeField] private OperationDeviceController[] _poolingDevices;
        [SerializeField] private OperationDeviceController[] _secondFilters;
        private OperationDeviceController[][] _devices;
        private Camera _camera;
        private bool _visible;
        private MapId _availableRound;
        private bool _additionalFiltersVisible;
        private IReadOnlyCollection<MapId> _revealed;
        internal bool RevealingOutputInputs => _display.RevealingOutputInputs;
        private bool _roundActive;
        internal event Action<int> DigitSelected;
        internal event Action InputPictureSelected;
        internal void ShowInputChannels(bool expanded) => _display.ShowInputChannels(expanded);
        internal event Action<MapId> OperationSelected;
        internal event Action FilterExpanded;
        internal event Action PresentationReady;
        internal event Action<string> ButtonPressed;
        internal string HoverName => _display.HoverName ?? _round.HoverName ?? "—";
        internal string LastClick { get; private set; } = "—";
        private void OnInputClicked(string name) => LastClick = name;
        internal Pose AdditionalFiltersPanelPose => new Pose(
            (_secondFilters[0].CalculationAnchor.position + _secondFilters[1].CalculationAnchor.position) * .5f
            + Vector3.up * .25f, _secondFilters[0].CalculationAnchor.rotation);

        internal void SetAdditionalFiltersVisible(bool visible)
        {
            _additionalFiltersVisible = visible;
            for (int map = 2; map < _secondFilters.Length; map++)
            {
                _secondFilters[map].SetVisible(visible);
                _display.SetResultVisible(3, map, visible);
            }
            UpdateDevices();
        }

        internal Pose GetLessonPanelPose(LessonPhase phase, int layer, int map, bool showingOutputDetails)
        {
            if (layer == 0)
            {
                return _display.InputLessonPanelPose;
            }
            if (layer == 4)
            {
                Pose pose = _display.OutputLessonPanelPose;
                if (phase == LessonPhase.Round && showingOutputDetails)
                {
                    pose.position = _round.FilterLessonPanelPosition;
                }
                return pose;
            }

            if (layer == 2 && phase == LessonPhase.Round)
            {
                return _pooling.LessonPanelPose(_poolingDevices[map].CalculationAnchor);
            }
            return layer == 3 && map >= 2
                ? _secondFilters[map].GetLessonPanelPose(phase == LessonPhase.Round)
                : _display.ResultLessonPanelPose(layer, map);
        }

        internal void ShowOperations(MapId round, IReadOnlyCollection<MapId> revealed)
        {
            _availableRound = round;
            _revealed = revealed;
            for (int layer = 1; layer <= _devices.Length; layer++)
            {
                bool secondMapUnlocked = revealed.Contains(new MapId(layer, 0));
                _devices[layer - 1][1].SetVisible(secondMapUnlocked);
                _display.SetResultVisible(layer, 1, secondMapUnlocked);
            }
            UpdateDevices();
        }

        private void UpdateDevices()
        {
            for (int layer = 1; layer <= _devices.Length; layer++)
            for (int map = 0; map < _devices[layer - 1].Length; map++)
            {
                bool completed = _revealed.Contains(new MapId(layer, map));
                bool current = layer == _availableRound.Layer &&
                    (map == _availableRound.Map || (layer == 3 && map >= 2 && _additionalFiltersVisible));
                bool available = (layer != 3 || map < 2 || _additionalFiltersVisible) &&
                    (current || (layer <= _availableRound.Layer && completed));
                OperationDeviceController device = _devices[layer - 1][map];
                device.SetHighlight(current && map == _availableRound.Map);
                device.SetInputActive(_visible && available);
            }
        }
        internal void Initialize(FeatureMapSnapshot snapshot)
        {
            _visuals.InitializeRuntime();
            _display.Bind(_visuals, snapshot);
            _devices = new[] { _firstFilters, _poolingDevices, _secondFilters };
            for (int layer = 1; layer <= _devices.Length; layer++)
            for (int map = 0; map < _devices[layer - 1].Length; map++)
            {
                var operation = new MapId(layer, map);
                _devices[layer - 1][map].Bind(_visuals, () =>
                {
                    LastClick = FeatureMapLabels.Operation(operation.Layer, operation.Map);
                    OperationSelected?.Invoke(operation);
                });
            }
            _pooling.Bind(_visuals, _display);
            _round.Bind(_visuals, _display);
        }

        internal void ShowFirstFilterKernels(RoundCalculationResult first, RoundCalculationResult second)
        {
            _firstFilters[0].ShowKernel(first);
            _firstFilters[1].ShowKernel(second);
        }

        internal void Enable()
        {
            _display.DigitSelected += OnDigit;
            _display.InputPictureSelected += OnInputPicture;
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
            _display.InputPictureSelected -= OnInputPicture;
            _display.ButtonPressed -= OnButton;
            _round.FilterPressed -= OnFilter;
            _round.PresentationReady -= OnPresentationReady;
        }

        private void OnInputPicture() => InputPictureSelected?.Invoke();
        private void OnDigit(int digit) => DigitSelected?.Invoke(digit);
        private void OnButton(string caption) => ButtonPressed?.Invoke(caption);
        private void OnPresentationReady() => PresentationReady?.Invoke();
        private void OnFilter(bool expanded)
        {
            if (_roundActive && expanded)
            {
                FilterExpanded?.Invoke();
            }
        }

        private static RoundStepSequence RulesFor(MapId round, bool outputDetails = false) => round.Layer switch
        {
            2 => PoolingRoundRules.Sequence,
            4 => outputDetails ? OutputRoundRules.Details : OutputRoundRules.Sequence,
            _ => ConvRoundRules.ForMap(round)
        };

        internal LessonStep FirstStep(MapId round, bool outputDetails = false) => RulesFor(round, outputDetails).First;
        internal LessonStep LastStep(MapId round, bool outputDetails = false) => RulesFor(round, outputDetails).Last;
        internal RoundTransition EvaluateTransition(MapId round, LessonStep step, RoundRequest request, bool outputDetails) =>
            RulesFor(round, outputDetails).Evaluate(step, request, _round.PresentationPending);

        private void SetInputActive(bool active)
        {
            _visible = active;
            _display.SetInputActive(active);
            UpdateFilterInput();
            UpdateDevices();
        }

        private void UpdateFilterInput() =>
            _round.SetInputActive(_visible && _roundActive && _round.IsOutput);

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
            if (_availableRound.Layer is >= 1 and <= 3)
            {
                _devices[_availableRound.Layer - 1][_availableRound.Map].TickHighlight(now);
            }
            if (_availableRound.Layer == 1)
            {
                _display.FollowInputChannels(_firstFilters[_availableRound.Map].CalculationAnchor,
                    _camera.transform.position);
            }
            _display.Tick(now);
            _visuals.UpdateBillboards(_camera);
            _pooling.Tick(now);
            if (!_roundActive)
            {
                return;
            }
            _round.Tick(now, _camera);
        }

        internal void ShowSnapshot(FeatureMapSnapshot snapshot, IReadOnlyCollection<MapId> revealed)
        {
            _display.ShowSnapshot(snapshot);
            RestoreResults(revealed);
        }

        internal void RestoreResults(IReadOnlyCollection<MapId> revealed) => _display.RestoreResults(revealed);
        internal void RevealOutputInput(int map) => _display.RevealOutputInput(map);
        internal void Highlight(int layer)
        {
            _display.ArrangePoolingSources(_secondFilters[0].CalculationAnchor, layer == 3);
            _display.HighlightLayer(layer);
        }
        internal void ShowPooling(FeatureMapSnapshot snapshot, int channel, PoolingCalculationResult data,
            LessonStep step, IReadOnlyCollection<MapId> revealed) =>
            _pooling.Show(snapshot, channel, data, step, revealed, _poolingDevices[channel].CalculationAnchor);

        internal void PrepareRound(RoundCalculationResult data)
        {
            _roundActive = true;
            if (data.IsOutput)
            {
                _round.Prepare(data, null);
            }
            else
            {
                OperationDeviceController device = _devices[data.Layer - 1][data.Map];
                _round.Prepare(data, device.CalculationAnchor, device.FilterSplitOrigin);
            }
            UpdateFilterInput();
        }

        internal void ShowRoundStep(LessonStep step, bool animateFilterOpening) =>
            _round.Show(step, animateFilterOpening);

        internal void FinishRoundStep(LessonStep step) => _round.FinishStep(step);

        internal void CloseRound(MapId round)
        {
            if (round.Layer == 2)
            {
                _pooling.Close();
                return;
            }

            ClosePresentation();
            if (round.Layer == 4)
            {
                _display.ResetOutput();
            }
        }

        private void ClosePresentation()
        {
            _roundActive = false;
            UpdateFilterInput();
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

        internal LessonContext DescribeRound(MapId round, LessonStep step, bool outputDetails)
        {
            return new LessonContext
            {
                WaitingForFilter = RoundStepSequence.WaitsForFilter(step),
                FilterPending = _round.PresentationPending,
                IsLastStep = step == LastStep(round, outputDetails),
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
