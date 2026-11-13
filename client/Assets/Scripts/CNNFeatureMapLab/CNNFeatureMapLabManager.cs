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
            new MapId(1, 0), new MapId(1, 1), new MapId(2, 0), new MapId(2, 1),
            new MapId(3, 0), new MapId(3, 1), new MapId(4, 0)
        };
        private PoolingCalculationResult _poolingCalculation;
        private bool IsPooling => _state.ActiveRound.Layer == 2;
        private bool SkipsRecap => (_state.ActiveRound.Layer is 1 or 3) && _state.ActiveRound.Map == 0;
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
            _featureMaps.ShowFirstFilterKernels(
                _executionData.GetRound(_state.Digit, 1, 0),
                _executionData.GetRound(_state.Digit, 1, 1));
            _lessonUI.Initialize();
            _environment.Initialize();
            _featureMaps.ShowOperations(default, _state.RevealedMaps);
            _featureMaps.SetAdditionalFiltersVisible(false);
            Highlight(0);
            RenderLesson();
            _lessonUI.SetVisible(false);
        }

        private void OnEnable()
        {
            _featureMaps.DigitSelected += DigitSelected;
            _featureMaps.InputPictureSelected += InputPictureSelected;
            _featureMaps.OperationSelected += OperationSelected;
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
            _environment.Tick();
            if (!_environment.Visible || CanExit)
            {
                return;
            }
            _featureMaps.Tick(now);
            StartOutput();
            _lessonUI.Tick(now);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lessonUI.ShowDebug(_state.Phase+" / "+_state.Step, _featureMaps.HoverName, "", _featureMaps.LastClick);
#endif
        }

        private void VisibilityChanged(bool visible)
        {
            _featureMaps.SetVisible(visible && !CanExit);
            _lessonUI.SetVisible(visible && !AwaitingOutput && !CanExit);
            if (visible)
            {
                RenderLesson();
            }
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
            _featureMaps.ShowSnapshot(_snapshot, _state.RevealedMaps);
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

        private void InputPictureSelected()
        {
            if (_state.Phase == LessonPhase.AwaitInput)
            {
                _featureMaps.ShowInputChannels(true);
                AwaitRound();
            }
        }

        private void AwaitRound()
        {
            MapId round = RequiredRound;
            if (round.Layer == 4)
            {
                for (int map = 2; map < _snapshot.MapCount(3); map++)
                {
                    var result = new MapId(3, map);
                    if (_state.RevealedMaps.Add(result))
                    {
                        _featureMaps.RevealOutputInput(map);
                    }
                }
            }
            _featureMaps.ShowOperations(round.Layer == 4 ? default : round, _state.RevealedMaps);

            Highlight(round.Layer);
            _state.Phase = LessonPhase.AwaitRound;
            if (AwaitingOutput)
            {
                SetProgress(.9f);
                _lessonUI.SetVisible(false);
                StartOutput();
                return;
            }
            RenderLesson();
        }

        private void OperationSelected(MapId round)
        {
            if (_state.Phase != LessonPhase.AwaitRound)
            {
                return;
            }

            MapId required = RequiredRound;
            if (round.Layer == 3 && round.Map >= 2 && !_state.AdditionalFiltersAccepted)
            {
                return;
            }
            bool mandatory = !_state.ExploringAdditionalFilters &&
                round.Layer == required.Layer && round.Map == required.Map;
            bool availableInLayer = mandatory || (_state.ExploringAdditionalFilters && round.Layer == 3 && round.Map >= 2);
            bool canReplay = round.Layer <= required.Layer && _state.RevealedMaps.Contains(round);
            if (!availableInLayer && !canReplay)
            {
                return;
            }

            BeginRound(round, mandatory, _featureMaps.FirstStep(round),
                animateFilterOpening: round.Layer == 1 || (round.Layer == 3 && round.Map < 2));
        }

        private bool AwaitingOutput => _state.Phase == LessonPhase.AwaitRound && RequiredRound.Layer == 4;

        private void StartOutput()
        {
            if (AwaitingOutput && !_featureMaps.RevealingOutputInputs)
            {
                MapId round = RequiredRound;
                BeginRound(round, true, _featureMaps.FirstStep(round));
                _lessonUI.SetVisible(_environment.Visible);
            }
        }

        private void BeginRound(MapId round, bool mandatory, LessonStep step, bool animateFilterOpening = false)
        {
            _state.Phase = LessonPhase.Round;
            _featureMaps.ShowOperations(default, _state.RevealedMaps);
            _state.ActiveRound = round;
            _state.MandatoryRound = mandatory;
            _state.Step = step;
            Highlight(round.Layer);
            PrepareActiveRound();
            ShowCurrentStep(animateFilterOpening);
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

        private void ShowCurrentStep(bool animateFilterOpening = false)
        {
            if (IsPooling)
            {
                _featureMaps.ShowPooling(_snapshot, _state.ActiveRound.Map, _poolingCalculation, _state.Step, _state.RevealedMaps);
            }
            else
            {
                _featureMaps.ShowRoundStep(_state.Step, animateFilterOpening);
            }
            RenderLesson();
        }

        private void CloseRound()
        {
            _featureMaps.CloseRound(_state.ActiveRound);
            _featureMaps.RestoreResults(_state.RevealedMaps);
        }

        private void ShowRecap()
        {
            _state.Phase = LessonPhase.Recap;
            if (_state.ActiveRound.Layer != 4)
            {
                _state.RevealedMaps.Add(_state.ActiveRound);
            }
            if (_state.ActiveRound.Layer == 3)
            {
                _state.Filter33Completed |= _state.ActiveRound.Map == 2;
                _state.Filter34Completed |= _state.ActiveRound.Map == 3;
            }
            RecapInfo recap = _featureMaps.FinishRoundForRecap();
            if (recap.Layer == 3 && recap.Map >= 2)
            {
                _lessonUI.AddRecap(recap);
            }
            _featureMaps.RestoreResults(_state.RevealedMaps);
            if (_state.MandatoryRound && _state.ActiveRound.Layer != 4)
            {
                CompleteRequiredRound();
            }
            if (SkipsRecap)
            {
                ContinueAfterRecap();
                return;
            }
            RenderLesson();
        }

        private void CompleteRequiredRound() => SetProgress((_state.RequiredRoundIndex + 1f) / RequiredRounds.Length);

        private void ShowPreviousRequiredRound()
        {
            _state.RequiredRoundIndex--;
            MapId round = RequiredRound;
            BeginRound(round, true, _featureMaps.LastStep(round));
            if (!IsPooling && !SkipsRecap)
            {
                ShowRecap();
            }
        }

        private void Finish()
        {
            _state.Phase = LessonPhase.Complete;
            Highlight(-1);
            RenderLesson();
            _featureMaps.SetVisible(false);
            _lessonUI.SetVisible(false);
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
                case LessonPhase.Recap when OffersAdditionalFilters:
                    if (request == RoundRequest.Previous)
                    {
                        _state.AdditionalFiltersAccepted = true;
                        _state.ExploringAdditionalFilters = true;
                        _featureMaps.SetAdditionalFiltersVisible(true);
                        AwaitRound();
                    }
                    else
                    {
                        ContinueAfterRecap();
                    }
                    break;
                case LessonPhase.AwaitRound when _state.ExploringAdditionalFilters && request == RoundRequest.Next:
                    FinishAdditionalFilters();
                    break;
                case LessonPhase.Round:
                    ApplyTransition(request);
                    break;
                case LessonPhase.AwaitRound when request == RoundRequest.Previous && _state.RequiredRoundIndex > 0:
                    ShowPreviousRequiredRound();
                    break;
                case LessonPhase.Recap when request == RoundRequest.Previous && _state.ActiveRound.Layer == 4:
                    _state.ShowingOutputDetails = true;
                    BeginRound(_state.ActiveRound, true, _featureMaps.FirstStep(_state.ActiveRound, outputDetails: true));
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
            RoundTransition transition = _featureMaps.EvaluateTransition(_state.ActiveRound, _state.Step, request, _state.ShowingOutputDetails);
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
                case RoundOutcome.BeforeFirstStep when _state.ShowingOutputDetails:
                case RoundOutcome.ShowRecap when _state.ShowingOutputDetails:
                    _state.ShowingOutputDetails = false;
                    _state.Step = LessonStep.RevealAnswer;
                    _featureMaps.ShowRoundStep(_state.Step, false);
                    ShowRecap();
                    break;
                case RoundOutcome.BeforeFirstStep:
                    CloseRound();
                    if (_state.ActiveRound.Layer == 4)
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
                    _state.RevealedMaps.Add(_state.ActiveRound);
                    CloseRound();
                    if (_state.MandatoryRound)
                    {
                        CompleteRequiredRound();
                        _state.RequiredRoundIndex++;
                    }
                    AwaitRound();
                    break;
            }
        }

        private void ContinueAfterRecap()
        {
            if (_state.ExploringAdditionalFilters)
            {
                if (_state.Filter33Completed && _state.Filter34Completed)
                {
                    FinishAdditionalFilters();
                }
                else
                {
                    AwaitRound();
                }
                return;
            }
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

        private bool OffersAdditionalFilters => _state.MandatoryRound &&
            _state.ActiveRound.Layer == 3 && _state.ActiveRound.Map == 1;

        private void FinishAdditionalFilters()
        {
            _state.ExploringAdditionalFilters = false;
            _state.RequiredRoundIndex++;
            AwaitRound();
        }

        private void RenderLesson()
        {
            LessonContext context = _state.Phase == LessonPhase.Round && !IsPooling
                ? _featureMaps.DescribeRound(_state.ActiveRound, _state.Step, _state.ShowingOutputDetails)
                : new LessonContext();
            context.Phase = _state.Phase;
            context.Digit = _state.Digit;
            context.Mandatory = _state.MandatoryRound;
            context.ShowingOutputDetails = _state.ShowingOutputDetails;
            context.Step = _state.Step;
            context.OfferAdditionalFilters = context.Phase == LessonPhase.Recap && OffersAdditionalFilters;
            context.ExploringAdditionalFilters = _state.ExploringAdditionalFilters;
            if (_state.Phase == LessonPhase.Round || _state.Phase == LessonPhase.Recap)
            {
                context.Layer = _state.ActiveRound.Layer;
                context.Map = _state.ActiveRound.Map;
            }
            else if (_state.Phase == LessonPhase.AwaitRound)
            {
                context.Layer = RequiredRound.Layer;
                context.Map = RequiredRound.Map;
                context.CanGoBack = _state.RequiredRoundIndex > 0;
            }
            if (context.Phase != LessonPhase.Complete)
            {
                context.LessonPanelPose = context.OfferAdditionalFilters ||
                    (context.Phase == LessonPhase.AwaitRound && context.ExploringAdditionalFilters)
                    ? _featureMaps.AdditionalFiltersPanelPose
                    : _featureMaps.GetLessonPanelPose(
                        context.Phase, context.Layer, context.Map, context.ShowingOutputDetails);
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
            _featureMaps.InputPictureSelected -= InputPictureSelected;
            _featureMaps.OperationSelected -= OperationSelected;
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
