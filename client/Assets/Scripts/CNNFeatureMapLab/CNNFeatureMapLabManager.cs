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
            _environment.Tick(now);
            if (!_environment.Visible)
            {
                return;
            }
            _featureMaps.Tick(now);
            _lessonUI.Tick(now, delta);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lessonUI.ShowDebug(_state.Phase+" / "+(_state.Phase == LessonPhase.Pooling ? _state.PoolingStage : _featureMaps.RoundStage), _featureMaps.HoverName, "", _featureMaps.LastClick);
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
            if (inRound || _state.Phase == LessonPhase.Recap)
            {
                _featureMaps.CloseRound();
            }
            _state.Digit = digit;
            _snapshot = _executionData.GetSnapshot(_state.Digit);
            _featureMaps.ShowSnapshot(_snapshot);
            if (inRound)
            {
                _featureMaps.RestartRound(_executionData.GetRound(_state.Digit, _featureMaps.RoundLayer, _featureMaps.RoundMap));
                RenderRound();
            }
            else if (_state.Phase == LessonPhase.Pooling)
            {
                _poolingCalculation = _executionData.GetPooling(_state.Digit, _state.PoolingChannel);
                ShowPooling();
            }
            else if (_state.Phase == LessonPhase.AwaitRound)
            {
                AwaitRound();
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
            Highlight(RequiredRound.Layer);
            if (RequiredRound.Layer == 2)
            {
                BeginPooling(0, 0);
                return;
            }
            _state.Phase = LessonPhase.AwaitRound;
            RenderLesson();
        }

        private void BeginPooling(int channel, int stage)
        {
            _state.Phase = LessonPhase.Pooling;
            _state.PoolingChannel = channel;
            _state.PoolingStage = stage;
            _poolingCalculation = _executionData.GetPooling(_state.Digit, channel);
            ShowPooling();
        }

        private void ShowPooling()
        {
            _featureMaps.ShowPooling(_snapshot, _state.PoolingChannel, _poolingCalculation, _state.PoolingStage);
            RenderLesson();
        }

        private void MapSelected(int layer, int map)
        {
            if (_state.Phase == LessonPhase.Pooling)
            {
                if (layer == 2 && map != _state.PoolingChannel)
                {
                    BeginPooling(map, _state.PoolingStage);
                }
                return;
            }
            if (layer == 2 || _state.Phase == LessonPhase.Round || _state.Phase == LessonPhase.Recap || CanExit)
            {
                return;
            }
            bool required = _state.Phase == LessonPhase.AwaitRound && layer == RequiredRound.Layer && map == RequiredRound.Map;
            BeginRound(layer, map, required);
        }

        private void TinSelected(int digit)
        {
            if (_state.Phase == LessonPhase.AwaitRound && RequiredRound.Layer == 4 && digit == _state.Digit)
            {
                BeginRound(4, 0, true);
            }
        }

        private void BeginRound(int layer, int map, bool mandatory)
        {
            _state.Phase = LessonPhase.Round;
            _state.MandatoryRound = mandatory;
            Highlight(layer);
            _featureMaps.StartRound(_executionData.GetRound(_state.Digit, layer, map));
            RenderLesson();
        }

        private void RenderRound()
        {
            if (_state.Phase == LessonPhase.Round)
            {
                RenderLesson();
            }
        }

        private void ShowRecap()
        {
            _state.Phase = LessonPhase.Recap;
            _lessonUI.AddRecaps(_featureMaps.Recap());
            if (_state.MandatoryRound && !_featureMaps.RoundIsOutput)
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
            if (round.Layer == 2)
            {
                Highlight(2);
                BeginPooling(0, 3);
                return;
            }
            BeginRound(round.Layer, round.Map, true);
            ShowRecap();
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

        private void Next()
        {
            if (_navigationFrame == Time.frameCount)
            {
                return;
            }
            _navigationFrame = Time.frameCount;
            switch (_state.Phase)
            {
                case LessonPhase.Pooling:
                if (_state.PoolingStage < 3)
                {
                    _state.PoolingStage++;
                    ShowPooling();
                    break;
                }
                CompleteRequiredRound();
                _featureMaps.ClosePooling(_snapshot);
                _state.RequiredRoundIndex++;
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
                if (!_state.MandatoryRound)
                {
                    AwaitRound();
                }
                else if (_featureMaps.RoundIsOutput)
                {
                    Finish();
                }
                else
                {
                    _state.RequiredRoundIndex++;
                    AwaitRound();
                }
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
            switch (_state.Phase)
            {
                case LessonPhase.Pooling:
                if (_state.PoolingStage > 0)
                {
                    _state.PoolingStage--;
                    ShowPooling();
                }
                else
                {
                    _featureMaps.ClosePooling(_snapshot);
                    ShowPreviousRequiredRound();
                }
                break;
                case LessonPhase.AwaitRound:
                if (_state.RequiredRoundIndex > 0)
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
                    if (_state.MandatoryRound)
                    {
                        AwaitRound();
                    }
                    else
                    {
                        AwaitRound();
                    }
                }
                break;
                case LessonPhase.Recap:
                BeginRound(_featureMaps.RoundLayer, _featureMaps.RoundMap, _state.MandatoryRound);
                _featureMaps.ReplayLastStep();
                RenderLesson();
                break;
            }
        }

        private void RenderLesson()
        {
            LessonContext context = (_state.Phase == LessonPhase.Round || _state.Phase == LessonPhase.Recap)
            ? _featureMaps.DescribeRound(_state.MandatoryRound) : new LessonContext();
            context.Phase = _state.Phase;
            context.Digit = _state.Digit;
            if (_state.Phase == LessonPhase.Pooling)
            {
                context.Stage = _state.PoolingStage;
                context.Map = _state.PoolingChannel;
                context.Pooling = _poolingCalculation;
            }
            if (_state.Phase == LessonPhase.AwaitRound)
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
