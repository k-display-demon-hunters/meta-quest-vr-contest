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
        private LessonStep _displayedStep;
        private float? _presentationReadyAt;

        internal bool IsOutput => _data.IsOutput;
        internal RoundCalculationResult Data => _data;
        internal Vector3 FilterPosition => _filter.Root.localPosition;
        internal bool PresentationPending => _presentationReadyAt.HasValue;

        internal event Action<bool> FilterPressed;
        internal event Action PresentationReady;
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
        internal void Prepare(RoundCalculationResult data)
        {
            _presentationReadyAt = null;
            _data = data;
            _destination = _display.Destination(Layer, Map, data.Digit);
            _filter.Configure(data, _display.Sources(Layer), IsOutput ? null : _display.Map(Layer, Map), _destination);
            _results.Configure(data, _display);
        }

        internal void Show(LessonStep step, bool filterInput)
        {
            _displayedStep = step;
            _started = Time.unscaledTime;
            _presentationReadyAt = filterInput ? _started + .7f : null;

            _filter.ShowCards(step is LessonStep.FilterIntro or LessonStep.FilterOpened
                or LessonStep.Scan or LessonStep.Explain or LessonStep.CellBreakdown);
            if (!filterInput)
            {
                _filter.SetExpanded(step != LessonStep.FilterIntro, true);
            }
            _filter.DimDuringScan(step == LessonStep.Scan);
            _display.ShowRound(_data, step);
            _results.Show(step);

            if (IsOutput)
            {
                ShowOutput(step);
            }
        }

        private void ShowOutput(LessonStep step)
        {
            // Rebuild the prerequisite visuals for revisits and input changes as well as forward navigation.
            _display.ResetOutput();
            if (step is LessonStep.CompareTemplates or LessonStep.FillProbabilities or LessonStep.RevealAnswer)
            {
                _display.StartMatch();
            }
            if (step is LessonStep.FillProbabilities or LessonStep.RevealAnswer)
            {
                _display.FinishMatch();
                _display.StartFill();
            }
            if (step == LessonStep.RevealAnswer)
            {
                _display.FinishFill();
                _display.Fly();
            }
        }

        internal void Tick(float now)
        {
            _filter.Tick(now);
            _results.Tick(_displayedStep, now - _started);
            if (_presentationReadyAt is float readyAt && now >= readyAt)
            {
                _presentationReadyAt = null;
                PresentationReady?.Invoke();
            }
        }

        internal void FinishStep(LessonStep step)
        {
            _results.Finish(step);
            switch (step)
            {
                case LessonStep.CompareTemplates:
                    _display.FinishMatch();
                    break;
                case LessonStep.FillProbabilities:
                    _display.FinishFill();
                    break;
                case LessonStep.RevealAnswer:
                    _display.CompleteOutput();
                    break;
            }
        }

        internal void Close()
        {
            _presentationReadyAt = null;
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
