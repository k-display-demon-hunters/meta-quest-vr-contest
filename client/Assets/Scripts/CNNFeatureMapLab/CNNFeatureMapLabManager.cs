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
        private static readonly MapId[] RequiredRounds =
        {
            new MapId(1, 0), new MapId(1, 1), new MapId(2, 0),
            new MapId(3, 0), new MapId(3, 1), new MapId(4, 0)
        };
        private int _requiredRound;
        private bool _mandatoryRound;
        private int _poolingStage;
        private int _poolingChannel;
        private PoolingCalculationResult _poolingCalculation;
        private MapId _switch;
        private MapId RequiredRound => RequiredRounds[_requiredRound];
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
            AwaitRound();
            _lessonUI.SetVisible(false);
        }

        private void OnEnable()
        {
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
            _lessonUI.ShowDebug(_phase+" / "+(_phase == LessonPhase.Pooling ? _poolingStage : _featureMaps.RoundStage), _featureMaps.HoverName, "", _featureMaps.LastClick);
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
                _featureMaps.RestartRound(_execution.CalculateRound(_featureMaps.RoundLayer, _featureMaps.RoundMap));
                RenderRound();
            }
            else if (_phase == LessonPhase.Pooling)
            {
                _poolingCalculation = _execution.CalculatePooling(_poolingChannel);
                ShowPooling();
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
            if (value > Progress)
            {
                Progress = value;
                ProgressChanged?.Invoke(value);
            }
        }

        private void AwaitRound()
        {
            Highlight(RequiredRound.Layer);
            if (RequiredRound.Layer == 2)
            {
                BeginPooling(0, 0);
                return;
            }
            _phase = LessonPhase.AwaitRound;
            RenderLesson();
        }

        private void BeginPooling(int channel, int stage)
        {
            _phase = LessonPhase.Pooling;
            _poolingChannel = channel;
            _poolingStage = stage;
            _poolingCalculation = _execution.CalculatePooling(channel);
            ShowPooling();
        }

        private void ShowPooling()
        {
            _featureMaps.ShowPooling(_execution.Snapshot, _poolingChannel, _poolingCalculation, _poolingStage);
            RenderLesson();
        }

        private void MapSelected(int layer, int map)
        {
            if (_phase == LessonPhase.Pooling)
            {
                if (layer == 2 && map != _poolingChannel)
                {
                    BeginPooling(map, _poolingStage);
                }
                return;
            }
            if (layer == 2 || _phase == LessonPhase.Round || _phase == LessonPhase.Recap || CanExit)
            {
                return;
            }
            bool required = _phase == LessonPhase.AwaitRound && layer == RequiredRound.Layer && map == RequiredRound.Map;
            _optionalReturn = _phase;
            BeginRound(layer, map, required);
        }

        private void TinSelected(int digit)
        {
            if (_phase == LessonPhase.AwaitRound && RequiredRound.Layer == 4 && digit == _execution.Digit)
            {
                BeginRound(4, 0, true);
            }
        }

        private void BeginRound(int layer, int map, bool mandatory)
        {
            _phase = LessonPhase.Round;
            _mandatoryRound = mandatory;
            Highlight(layer);
            _featureMaps.StartRound(_execution.CalculateRound(layer, map));
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
            _lessonUI.AddRecaps(_featureMaps.Recap());
            if (_mandatoryRound)
            {
                CompleteRequiredRound();
            }
            RenderLesson();
        }

        private void CompleteRequiredRound() => SetProgress((_requiredRound + 1f) / (RequiredRounds.Length + 1));

        private void ShowPreviousRequiredRound()
        {
            _requiredRound--;
            MapId round = RequiredRound;
            if (round.Layer == 2)
            {
                Highlight(2);
                BeginPooling(0, 3);
                return;
            }
            BeginRound(round.Layer, round.Map, true);
            ShowRecap();
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
                case LessonPhase.Pooling:
                if (_poolingStage < 3)
                {
                    _poolingStage++;
                    ShowPooling();
                    break;
                }
                CompleteRequiredRound();
                _featureMaps.ClosePooling(_execution.Snapshot);
                _requiredRound++;
                AwaitRound();
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
                    _requiredRound++;
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
                case LessonPhase.Pooling:
                if (_poolingStage > 0)
                {
                    _poolingStage--;
                    ShowPooling();
                }
                else
                {
                    _featureMaps.ClosePooling(_execution.Snapshot);
                    ShowPreviousRequiredRound();
                }
                break;
                case LessonPhase.AwaitRound:
                if (_requiredRound > 0)
                {
                    ShowPreviousRequiredRound();
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
                BeginRound(_featureMaps.RoundLayer, _featureMaps.RoundMap, _mandatoryRound);
                _featureMaps.ReplayLastStep();
                RenderLesson();
                break;
                case LessonPhase.AwaitOff:
                _featureMaps.ShowSwitches(false);
                BeginRound(4, 0, true);
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
            context.Switch = _switch;
            if (_phase == LessonPhase.Pooling)
            {
                context.Stage = _poolingStage;
                context.Map = _poolingChannel;
                context.Pooling = _poolingCalculation;
            }
            if (_phase == LessonPhase.AwaitRound)
            {
                context.Layer = RequiredRound.Layer;
                context.Map = RequiredRound.Map;
                context.CanGoBack = _requiredRound > 0;
                context.HintPosition = _featureMaps.WaitingHint(context.Layer, context.Map, _execution.Digit);
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
