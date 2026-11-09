using System;
using System.Collections;
using Learning;
using UnityEngine;

namespace FeatureMapCorridor
{
    public sealed class CNNFeatureMapLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private FeatureMapExecutionManager _execution;
        [SerializeField] private FeatureMapsManager _featureMaps;
        [SerializeField] private LessonUIManager _lessonUI;
        [SerializeField] private CorridorEnvironmentManager _environment;
        private LessonPhase _phase;
        private LessonPhase _optionalReturn;
        private int _tourStage;
        private int _tourIndex;
        private int _mandatoryLayer = 1;
        private bool _mandatoryRound;
        private readonly int[] _savedRoundIndices = new int[4];
        private MapId _switch;
        private bool _tourIntroduced;
        private bool _tourIntroThisVisit;
        private int _navigationFrame = -1;
        public LearningStageId StageId => LearningStageId.FeatureMap;
        public float Progress
        {
            get;
            private set;
        }

        public bool CanExit => _phase == LessonPhase.Complete;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;
        private void Awake()
        {
            _execution.Initialize();
            _featureMaps.Initialize(_execution.Snapshot);
            _lessonUI.Initialize();
            _environment.Initialize();
            ShowStart();
            _lessonUI.SetVisible(false);
        }

        private void OnEnable()
        {
            _featureMaps.InputSelected += InputSelected;
            _featureMaps.DigitSelected += DigitSelected;
            _featureMaps.MapSelected += MapSelected;
            _featureMaps.SwitchSelected += Toggle;
            _featureMaps.OutputSelected += TinSelected;
            _featureMaps.ZoneSelected += ToggleZone;
            _featureMaps.RoundChanged += RenderRound;
            _featureMaps.ButtonPressed += _lessonUI.RecordButton;
            _lessonUI.PreviousRequested += Previous;
            _lessonUI.NextRequested += Next;
            _environment.VisibilityChanged += VisibilityChanged;
            _featureMaps.Enable();
            _lessonUI.Enable();
            _environment.Enable();
        }

        private void Start() => _environment.Start(gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            float delta = Time.unscaledDeltaTime;
            _environment.Tick(now, delta);
            if (!_environment.Visible)
            {
                return;
            }
            _featureMaps.Tick(now, delta);
            _lessonUI.Tick(now, delta);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lessonUI.ShowDebug(_phase+" / "+(_phase == LessonPhase.Tour ? _tourStage : _featureMaps.RoundStage), _featureMaps.HoverName, _featureMaps.HoveredCell, _featureMaps.LastClick);
#endif
        }

        private void VisibilityChanged(bool visible)
        {
            _featureMaps.SetVisible(visible);
            _lessonUI.SetVisible(visible);
        }

        private void DigitSelected(int digit)
        {
            if (_execution.Digit == digit)
            {
                return;
            }
            bool inRound = _phase == LessonPhase.Round;
            if (inRound || _phase == LessonPhase.Recap)
            {
                _featureMaps.CloseRound();
            }
            _execution.SelectDigit(digit);
            _featureMaps.ShowSnapshot(_execution.Snapshot);
            if (inRound)
            {
                _featureMaps.RestartRound(_execution.CalculateRound(_featureMaps.RoundLayer, _featureMaps.RoundMap, _featureMaps.RoundIndex));
                RenderRound();
            }
            else if (_phase == LessonPhase.AwaitRound)
            {
                AwaitRound();
            }
        }

        private void ToggleZone(int zone) => Highlight(_environment.HighlightedZone == zone ? -1 : zone);
        private void Highlight(int zone)
        {
            _environment.Highlight(zone);
            _featureMaps.Highlight(zone);
        }

        private void SetProgress(float value)
        {
            Progress = value;
            ProgressChanged?.Invoke(value);
        }

        private void ShowStart()
        {
            _phase = LessonPhase.Start;
            Highlight(-1);
            RenderLesson();
        }

        private void InputSelected(int index)
        {
            if (_phase != LessonPhase.Start && _phase != LessonPhase.Tour)
            {
                return;
            }
            _tourIndex = index;
            _tourIntroThisVisit = !_tourIntroduced;
            _tourIntroduced = true;
            ShowTour(0);
        }

        private void ShowTour(int stage)
        {
            _phase = LessonPhase.Tour;
            _tourStage = stage;
            Highlight(stage);
            _featureMaps.ShowTour(stage, _tourIndex);
            RenderLesson();
        }

        private void AwaitRound()
        {
            _phase = LessonPhase.AwaitRound;
            Highlight(_mandatoryLayer);
            RenderLesson();
        }

        private void MapSelected(int layer, int map, int index)
        {
            if (_phase == LessonPhase.Round || _phase == LessonPhase.Recap || CanExit)
            {
                return;
            }
            if (_phase == LessonPhase.AwaitRound && layer == _mandatoryLayer && map == 0)
            {
                BeginRound(layer, map, index, true);
                return;
            }
            _optionalReturn = _phase;
            BeginRound(layer, map, index, false);
        }

        private void TinSelected(int digit)
        {
            if (_phase == LessonPhase.AwaitRound && _mandatoryLayer == 4 && digit == _execution.Digit)
            {
                BeginRound(4, 0, 0, true);
            }
        }

        private void BeginRound(int layer, int map, int index, bool mandatory)
        {
            _phase = LessonPhase.Round;
            _mandatoryRound = mandatory;
            Highlight(layer);
            _featureMaps.StartRound(_execution.CalculateRound(layer, map, index));
            RenderLesson();
        }

        private void RenderRound()
        {
            if (_phase == LessonPhase.Round)
            {
                RenderLesson();
            }
        }

        private void ShowRecap()
        {
            _phase = LessonPhase.Recap;
            _lessonUI.AddRecaps(_featureMaps.Recap(_mandatoryRound));
            if (_mandatoryRound)
            {
                int layer = _featureMaps.RoundLayer;
                _savedRoundIndices[layer-1] = _featureMaps.RoundIndex;
                _featureMaps.Celebrate(layer);
                SetProgress((layer+1)/6f);
            }
            RenderLesson();
        }

        private void AwaitOff()
        {
            _phase = LessonPhase.AwaitOff;
            Highlight(-1);
            _featureMaps.ShowSwitches(true);
            RenderLesson();
        }

        private void Toggle(int layer, int map)
        {
            if (_phase != LessonPhase.AwaitOff && _phase != LessonPhase.OffResult && _phase != LessonPhase.AwaitOn && _phase != LessonPhase.OnResult && !CanExit)
            {
                return;
            }
            bool turningOn = _execution.Toggle(new MapId(layer, map));
            _featureMaps.ShowSnapshot(_execution.Snapshot);
            if (!turningOn && (_phase == LessonPhase.AwaitOff || _phase == LessonPhase.OnResult))
            {
                _switch = new MapId(layer, map);
                ShowOffResult();
            }
            else if (turningOn && layer == _switch.Layer && map == _switch.Map)
            {
                if (_phase == LessonPhase.AwaitOn)
                {
                    ShowOnResult();
                }
                else if (_phase == LessonPhase.OffResult)
                {
                    AwaitOff();
                }
            }
        }

        private void ShowOffResult()
        {
            _phase = LessonPhase.OffResult;
            RenderLesson();
        }

        private void AwaitOn()
        {
            _phase = LessonPhase.AwaitOn;
            RenderLesson();
        }

        private void ShowOnResult()
        {
            _phase = LessonPhase.OnResult;
            RenderLesson();
        }

        private void Finish()
        {
            _phase = LessonPhase.Complete;
            _environment.OpenDoor();
            RenderLesson();
            _lessonUI.OpenCompletion();
            SetProgress(1);
            ExitAvailabilityChanged?.Invoke(true);
        }

        private void Next()
        {
            if (_navigationFrame == Time.frameCount)
            {
                return;
            }
            _navigationFrame = Time.frameCount;
            switch (_phase)
            {
                case LessonPhase.Tour:
                _featureMaps.FinishTour();
                if (_tourStage<3)
                {
                    ShowTour(_tourStage+1);
                }
                else
                {
                    _featureMaps.ClearTour();
                    SetProgress(1/6f);
                    AwaitRound();
                }
                break;
                case LessonPhase.Round:
                if (!_featureMaps.CanAdvanceRound)
                {
                    return;
                }
                if (_featureMaps.AdvanceRound())
                {
                    RenderLesson();
                }
                else
                {
                    ShowRecap();
                }
                break;
                case LessonPhase.Recap:
                if (!_mandatoryRound)
                {
                    RestoreOptional();
                }
                else if (_featureMaps.RoundIsOutput)
                {
                    AwaitOff();
                }
                else
                {
                    _mandatoryLayer++;
                    AwaitRound();
                }
                break;
                case LessonPhase.OffResult: AwaitOn();
                break;
                case LessonPhase.OnResult: Finish();
                break;
            }
        }

        private void Previous()
        {
            if (_navigationFrame == Time.frameCount)
            {
                return;
            }
            _navigationFrame = Time.frameCount;
            switch (_phase)
            {
                case LessonPhase.Tour:
                if (_tourStage>0)
                {
                    ShowTour(_tourStage-1);
                }
                else
                {
                    _featureMaps.ClearTour();
                    ShowStart();
                }
                break;
                case LessonPhase.AwaitRound:
                if (_mandatoryLayer == 1)
                {
                    ShowTour(3);
                }
                else
                {
                    _mandatoryLayer--;
                    BeginRound(_mandatoryLayer, 0, _savedRoundIndices[_mandatoryLayer-1], true);
                    ShowRecap();
                }
                break;
                case LessonPhase.Round:
                if (_featureMaps.PreviousRound())
                {
                    RenderLesson();
                }
                else
                {
                    _featureMaps.CloseRound();
                    if (_mandatoryRound)
                    {
                        AwaitRound();
                    }
                    else
                    {
                        RestoreOptional();
                    }
                }
                break;
                case LessonPhase.Recap:
                BeginRound(_featureMaps.RoundLayer, _featureMaps.RoundMap, _featureMaps.RoundIndex, _mandatoryRound);
                _featureMaps.ReplayLastStep();
                RenderLesson();
                break;
                case LessonPhase.AwaitOff:
                _featureMaps.ShowSwitches(false);
                BeginRound(4, 0, 0, true);
                _featureMaps.CompleteOutputForRecap();
                ShowRecap();
                break;
                case LessonPhase.OffResult:
                _execution.SetEnabled(_switch, true);
                _featureMaps.ShowSnapshot(_execution.Snapshot);
                AwaitOff();
                break;
                case LessonPhase.AwaitOn: ShowOffResult();
                break;
                case LessonPhase.OnResult:
                _execution.SetEnabled(_switch, false);
                _featureMaps.ShowSnapshot(_execution.Snapshot);
                AwaitOn();
                break;
            }
        }

        private void RestoreOptional()
        {
            switch (_optionalReturn)
            {
                case LessonPhase.Start: ShowStart();
                break;
                case LessonPhase.Tour: ShowTour(_tourStage);
                break;
                case LessonPhase.AwaitOff: AwaitOff();
                break;
                case LessonPhase.OffResult: ShowOffResult();
                break;
                case LessonPhase.AwaitOn: AwaitOn();
                break;
                case LessonPhase.OnResult: ShowOnResult();
                break;
                default: AwaitRound();
                break;
            }
        }

        private void RenderLesson()
        {
            LessonContext context = (_phase == LessonPhase.Round || _phase == LessonPhase.Recap)
            ? _featureMaps.DescribeRound(_mandatoryRound) : new LessonContext();
            context.Phase = _phase;
            context.Digit = _execution.Digit;
            context.TourStage = _tourStage;
            context.FirstTour = _tourIntroThisVisit;
            context.Switch = _switch;
            if (_phase == LessonPhase.Start)
            {
                context.HintPosition = _featureMaps.InputHint;
            }
            if (_phase == LessonPhase.AwaitRound)
            {
                context.Layer = _mandatoryLayer;
                context.HintPosition = _featureMaps.WaitingHint(_mandatoryLayer, _execution.Digit);
            }
            if (_phase == LessonPhase.AwaitOff)
            {
                context.HintPosition = _featureMaps.SwitchHint(1, 0);
            }
            if (_phase == LessonPhase.AwaitOn)
            {
                context.HintPosition = _featureMaps.SwitchHint(_switch.Layer, _switch.Map);
            }
            if (_phase == LessonPhase.Complete)
            {
                context.HintPosition = _environment.ExitHint;
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
            _featureMaps.InputSelected -= InputSelected;
            _featureMaps.DigitSelected -= DigitSelected;
            _featureMaps.MapSelected -= MapSelected;
            _featureMaps.SwitchSelected -= Toggle;
            _featureMaps.OutputSelected -= TinSelected;
            _featureMaps.ZoneSelected -= ToggleZone;
            _featureMaps.RoundChanged -= RenderRound;
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
