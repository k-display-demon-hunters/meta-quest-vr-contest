using System;
using System.Collections;
using Learning;
using UnityEngine;

namespace FeatureMapCorridor
{
    public sealed class CNNFeatureMapLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private FeatureMapExecutionDataManager _executionData;
        [SerializeField] private FeatureMapsManager _featureMaps;
        [SerializeField] private LessonUIManager _lessonUI;
        [SerializeField] private CorridorEnvironmentManager _environment;
        private readonly LessonState _state = new LessonState();
        private FeatureMapSnapshot _snapshot;
        private static readonly MapId[] RequiredRounds =
        {
            new MapId(1, 0), new MapId(1, 1), new MapId(2, 0),
            new MapId(3, 0), new MapId(3, 1), new MapId(4, 0)
        };
        private PoolingCalculationResult _poolingCalculation;
        private bool IsPooling => _state.ActiveRound.Layer == 2;
        private MapId RequiredRound => RequiredRounds[_state.RequiredRoundIndex];
        private int _navigationFrame = -1;
        public LearningStageId StageId => LearningStageId.FeatureMap;
        public float Progress => _state.Progress;

        public bool CanExit => _state.Phase == LessonPhase.Complete;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;
        private void Awake()
        {
            _snapshot = _executionData.GetSnapshot(_state.Digit);
            _featureMaps.Initialize(_snapshot);
            _lessonUI.Initialize();
            _environment.Initialize();
            AwaitRound();
            _lessonUI.SetVisible(false);
        }

        private void OnEnable()
        {
            _featureMaps.DigitSelected += DigitSelected;
            _featureMaps.MapSelected += MapSelected;
            _featureMaps.OutputSelected += TinSelected;
            _featureMaps.FilterExpanded += OnFilterExpanded;
            _featureMaps.PresentationReady += RenderLesson;
            _featureMaps.ButtonPressed += _lessonUI.RecordButton;
            _lessonUI.PreviousRequested += Previous;
            _lessonUI.NextRequested += Next;
            _environment.VisibilityChanged += VisibilityChanged;
            _featureMaps.Enable();
            _lessonUI.Enable();
            _environment.Enable();
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            float delta = Time.unscaledDeltaTime;
            _environment.Tick(now);
            if (!_environment.Visible)
            {
                return;
            }
            _featureMaps.Tick(now);
            _lessonUI.Tick(now, delta);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lessonUI.ShowDebug(_state.Phase+" / "+_state.Step, _featureMaps.HoverName, "", _featureMaps.LastClick);
#endif
        }

        private void VisibilityChanged(bool visible)
        {
            _featureMaps.SetVisible(visible);
            _lessonUI.SetVisible(visible);
        }

        private void DigitSelected(int digit)
        {
            if (_state.Digit == digit)
            {
                return;
            }
            bool inRound = _state.Phase == LessonPhase.Round;
            if (inRound)
            {
                CloseRound();
            }
            _state.Digit = digit;
            _snapshot = _executionData.GetSnapshot(_state.Digit);
            _featureMaps.ShowSnapshot(_snapshot);
            if (inRound)
            {
                PrepareActiveRound();
                ShowCurrentStep();
            }
            else if (_state.Phase == LessonPhase.AwaitRound)
            {
                AwaitRound();
            }
            else if (_state.Phase == LessonPhase.Recap)
            {
                RenderLesson();
            }
        }

        private void Highlight(int zone)
        {
            _environment.Highlight(zone);
            _featureMaps.Highlight(zone);
        }

        private void SetProgress(float value)
        {
            if (value > Progress)
            {
                _state.Progress = value;
                ProgressChanged?.Invoke(value);
            }
        }

        private void AwaitRound()
        {
            MapId round = RequiredRound;
            if (round.Layer == 2)
            {
                BeginRound(round, true, _featureMaps.FirstStep(round));
                return;
            }

            Highlight(round.Layer);
            _state.Phase = LessonPhase.AwaitRound;
            RenderLesson();
        }

        private void MapSelected(int layer, int map)
        {
            if (_state.Phase == LessonPhase.Round && IsPooling)
            {
                if (layer == 2 && map != _state.ActiveRound.Map)
                {
                    _state.ActiveRound = new MapId(layer, map);
                    PrepareActiveRound();
                    ShowCurrentStep();
                }
                return;
            }
            if (layer == 2 || _state.Phase != LessonPhase.AwaitRound)
            {
                return;
            }

            var round = new MapId(layer, map);
            bool mandatory = layer == RequiredRound.Layer && map == RequiredRound.Map;
            BeginRound(round, mandatory, _featureMaps.FirstStep(round));
        }

        private void TinSelected(int digit)
        {
            if (_state.Phase == LessonPhase.AwaitRound && RequiredRound.Layer == 4 && digit == _state.Digit)
            {
                MapId round = RequiredRound;
                BeginRound(round, true, _featureMaps.FirstStep(round));
            }
        }

        private void BeginRound(MapId round, bool mandatory, LessonStep step)
        {
            _state.Phase = LessonPhase.Round;
            _state.ActiveRound = round;
            _state.MandatoryRound = mandatory;
            _state.Step = step;
            Highlight(round.Layer);
            PrepareActiveRound();
            ShowCurrentStep();
        }

        private void PrepareActiveRound()
        {
            MapId round = _state.ActiveRound;
            if (IsPooling)
            {
                _poolingCalculation = _executionData.GetPooling(_state.Digit, round.Map);
                return;
            }

            _featureMaps.PrepareRound(_executionData.GetRound(_state.Digit, round.Layer, round.Map));
        }

        private void ShowCurrentStep(bool filterInput = false)
        {
            if (IsPooling)
            {
                _featureMaps.ShowPooling(_snapshot, _state.ActiveRound.Map, _poolingCalculation, _state.Step);
            }
            else
            {
                _featureMaps.ShowRoundStep(_state.Step, filterInput);
            }
            RenderLesson();
        }

        private void CloseRound() => _featureMaps.CloseRound(_state.ActiveRound, _snapshot);

        private void ShowRecap()
        {
            _state.Phase = LessonPhase.Recap;
            _lessonUI.AddRecap(_featureMaps.FinishRoundForRecap());
            if (_state.MandatoryRound && _state.ActiveRound.Layer != 4)
            {
                CompleteRequiredRound();
            }
            RenderLesson();
        }

        private void CompleteRequiredRound() => SetProgress((_state.RequiredRoundIndex + 1f) / RequiredRounds.Length);

        private void ShowPreviousRequiredRound()
        {
            _state.RequiredRoundIndex--;
            MapId round = RequiredRound;
            BeginRound(round, true, _featureMaps.LastStep(round));
            if (!IsPooling)
            {
                ShowRecap();
            }
        }

        private void Finish()
        {
            _state.Phase = LessonPhase.Complete;
            Highlight(-1);
            RenderLesson();
            _lessonUI.OpenCompletion();
            SetProgress(1);
            ExitAvailabilityChanged?.Invoke(true);
        }

        private void Next() => Navigate(RoundRequest.Next);
        private void Previous() => Navigate(RoundRequest.Previous);

        private void Navigate(RoundRequest request)
        {
            if (_navigationFrame == Time.frameCount)
            {
                return;
            }
            _navigationFrame = Time.frameCount;
            switch (_state.Phase)
            {
                case LessonPhase.Round:
                    ApplyTransition(request);
                    break;
                case LessonPhase.AwaitRound when request == RoundRequest.Previous && _state.RequiredRoundIndex > 0:
                    ShowPreviousRequiredRound();
                    break;
                case LessonPhase.Recap when request == RoundRequest.Previous:
                    MapId round = _state.ActiveRound;
                    BeginRound(round, _state.MandatoryRound, _featureMaps.LastStep(round));
                    break;
                case LessonPhase.Recap:
                    ContinueAfterRecap();
                    break;
            }
        }

        private void OnFilterExpanded()
        {
            if (_state.Phase == LessonPhase.Round)
            {
                ApplyTransition(RoundRequest.FilterExpanded);
            }
        }

        private void ApplyTransition(RoundRequest request)
        {
            RoundTransition transition = _featureMaps.EvaluateTransition(_state.ActiveRound, _state.Step, request);
            if (!IsPooling && request == RoundRequest.Next && transition.Outcome != RoundOutcome.Stay)
            {
                _featureMaps.FinishRoundStep(_state.Step);
            }

            switch (transition.Outcome)
            {
                case RoundOutcome.Move:
                    _state.Step = transition.Step;
                    ShowCurrentStep(request == RoundRequest.FilterExpanded);
                    break;
                case RoundOutcome.BeforeFirstStep:
                    CloseRound();
                    if (IsPooling)
                    {
                        ShowPreviousRequiredRound();
                    }
                    else
                    {
                        AwaitRound();
                    }
                    break;
                case RoundOutcome.ShowRecap:
                    ShowRecap();
                    break;
                case RoundOutcome.Complete:
                    CloseRound();
                    CompleteRequiredRound();
                    _state.RequiredRoundIndex++;
                    AwaitRound();
                    break;
            }
        }

        private void ContinueAfterRecap()
        {
            if (_state.MandatoryRound && _state.ActiveRound.Layer == 4)
            {
                Finish();
                return;
            }
            if (_state.MandatoryRound)
            {
                _state.RequiredRoundIndex++;
            }
            AwaitRound();
        }

        private void RenderLesson()
        {
            LessonContext context = _state.Phase == LessonPhase.Round && !IsPooling
                ? _featureMaps.DescribeRound(_state.ActiveRound, _state.Step)
                : new LessonContext();
            context.Phase = _state.Phase;
            context.Digit = _state.Digit;
            context.Mandatory = _state.MandatoryRound;
            context.Step = _state.Step;
            if (_state.Phase == LessonPhase.Round || _state.Phase == LessonPhase.Recap)
            {
                context.Layer = _state.ActiveRound.Layer;
                context.Map = _state.ActiveRound.Map;
                context.Pooling = _poolingCalculation;
            }
            else if (_state.Phase == LessonPhase.AwaitRound)
            {
                context.Layer = RequiredRound.Layer;
                context.Map = RequiredRound.Map;
                context.CanGoBack = _state.RequiredRoundIndex > 0;
                context.HintPosition = _featureMaps.WaitingHint(context.Layer, context.Map, _state.Digit);
            }
            _lessonUI.Show(context);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI() => _lessonUI.DrawDebug();
#endif
        public IEnumerator EndSession()
        {
            yield break;
        }

        private void OnDisable()
        {
            _featureMaps.Disable();
            _lessonUI.Disable();
            _environment.Disable();
            _featureMaps.DigitSelected -= DigitSelected;
            _featureMaps.MapSelected -= MapSelected;
            _featureMaps.OutputSelected -= TinSelected;
            _featureMaps.FilterExpanded -= OnFilterExpanded;
            _featureMaps.PresentationReady -= RenderLesson;
            _featureMaps.ButtonPressed -= _lessonUI.RecordButton;
            _lessonUI.PreviousRequested -= Previous;
            _lessonUI.NextRequested -= Next;
            _environment.VisibilityChanged -= VisibilityChanged;
        }

        private void OnDestroy()
        {
            _featureMaps.Dispose();
            _lessonUI.Dispose();
        }
    }
}
