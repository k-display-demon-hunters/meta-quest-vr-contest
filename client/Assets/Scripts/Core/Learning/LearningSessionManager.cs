using System;
using System.Collections;
using CNNFeatureMaps;
using FactoryEnvironment;
using FactoryTutorial;
using ModelDesignScene;
using Parchment;
using Tablet;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Learning
{
    // Scene-scoped learning session: one owner for progress, transitions and module connections.
    public sealed class LearningSessionManager : MonoBehaviour
    {
        [SerializeField] private FactoryEnvironmentManager _factory;
        [SerializeField] private LearningSceneLoadController _sceneLoader;
        private readonly LearningProgress _progress = new LearningProgress();
        private TabletManager _tablet;
        private ParchmentManager _parchment;
        private XROrigin _rig;
        private FactoryTutorialManager _tutorial;
        private CNNPixelLabManager _pixels;
        private CNNFeatureMapLabManager _maps;
        private CNNModelDesignLabManager _model;
        private LearningStageId _currentStage;
        private LearningStageId _destination = LearningStageId.Tutorial;
        private bool _canProceed;
        private bool _passagePrepared;
        private bool _transitioning;
        private bool _initialized;
        private bool _leaving;
        private Coroutine _evaluation;
        private Coroutine _retry;

        public event Action<LearningProgressSnapshot> StateChanged;
        public bool CanTravel => isActiveAndEnabled && !_transitioning && !_leaving
            && (_currentStage != LearningStageId.Tutorial || _progress.IsCompleted(LearningStageId.Tutorial));
        public LearningProgressSnapshot GetSnapshot() => _progress.CreateSnapshot(_currentStage, CanTravel);

        private void Awake()
        {
            // Persistent peripherals can have survived the preceding scene.
            _tablet = FindFirstObjectByType<TabletManager>();
            _parchment = FindFirstObjectByType<ParchmentManager>();
            _rig = FindFirstObjectByType<XROrigin>();
        }

        private void OnEnable()
        {
            _tablet.StageMoveRequested += RequestStageMove;
            StateChanged += _tablet.SetLearningState;
            _parchment.Collected += HandleParchmentCollected;
            _factory.StageAreaChanged += HandleStageAreaChanged;
            _factory.ExitAreaChanged += HandleExitAreaChanged;
            _factory.MapChanged += _tablet.SetMapData;
            SubscribeLesson();
            _tablet.SetLearningState(GetSnapshot());
            if (_initialized)
            {
                _tablet.SetMapData(_factory.MapData);
                ReadLessonState();
                RequestEvaluation();
            }
        }

        private void Start()
        {
            _factory.Initialize(_rig);
            _initialized = true;
            _factory.SetEntranceAvailable(LearningStageId.Tutorial, true);
            _factory.ShowDestination(LearningStageId.Tutorial);
            PublishState();
            RequestEvaluation();
        }

        private void OnDisable()
        {
            CancelRetry();
            if (_evaluation != null) { StopCoroutine(_evaluation); _evaluation = null; }
            UnsubscribeLesson();
            _parchment.Collected -= HandleParchmentCollected;
            _factory.StageAreaChanged -= HandleStageAreaChanged;
            _factory.ExitAreaChanged -= HandleExitAreaChanged;
            _factory.MapChanged -= _tablet.SetMapData;
            StateChanged -= _tablet.SetLearningState;
            if (_tablet != null)
            {
                _tablet.StageMoveRequested -= RequestStageMove;
                _tablet.SetLearningState(LearningProgressSnapshot.Empty);
                _tablet.ShowTravelCompleted();
                _tablet.ClearMapData(_factory.MapData);
            }
        }

        private void HandleStageAreaChanged(LearningStageId stage, bool inside) => RequestEvaluation();
        private void HandleExitAreaChanged(bool inside) => RequestEvaluation();
        private void HandleParchmentCollected(int documentId) => RequestEvaluation();
        private void PublishState() => StateChanged?.Invoke(GetSnapshot());

        private void HandleStageCompleted(LearningStageId stage)
        {
            if (_progress.RecordCompletion(stage)) { PublishState(); }
            RequestEvaluation();
        }

        private void HandleAvailabilityChanged(LearningStageId stage, bool ready)
        {
            if (stage != _currentStage) { return; }
            _canProceed = ready;
            RequestEvaluation();
        }

        private void HandleConvolutionStarted()
        {
            SetCurrentStage(LearningStageId.Convolution);
            _canProceed = _pixels.CanProceed(_currentStage);
            _factory.StageActivated(_currentStage);
            PublishState();
            RequestEvaluation();
        }

        // Coalesce synchronous events so listeners finish before any scene is unloaded.
        private void RequestEvaluation()
        {
            if (!_initialized || !isActiveAndEnabled || _evaluation != null) { return; }
            _evaluation = StartCoroutine(EvaluateAfterEvents());
        }

        private IEnumerator EvaluateAfterEvents()
        {
            yield return null;
            _evaluation = null;
            Evaluate();
        }

        private bool HasEntranceParchments(LearningStageId stage) =>
            _parchment.HasCollectedAll(LearningStageCatalog.Get(stage).EntranceParchmentIds);

        private void Evaluate()
        {
            if (_transitioning || _leaving || _retry != null) { return; }
            if (_currentStage == LearningStageId.None)
            {
                if (_destination == LearningStageId.Tutorial && _factory.IsStageAreaInside(_destination))
                    BeginOperation(EnterTutorial());
                return;
            }
            if (_passagePrepared)
            {
                if (_destination == LearningStageId.None)
                {
                    if (_factory.IsExitAreaInside) { BeginOperation(EnterEnding()); }
                    return;
                }
                bool available = HasEntranceParchments(_destination);
                if (_destination == LearningStageId.Convolution) { available &= _canProceed; }
                _factory.SetEntranceAvailable(_destination, available);
                if (available && _factory.IsStageAreaInside(_destination))
                {
                    BeginOperation(_destination == LearningStageId.Convolution
                        ? StartConvolution() : EnterPreparedLesson(_destination));
                }
                return;
            }
            if (!_canProceed) { return; }
            LearningStageId next = LearningStageCatalog.Next(_currentStage);
            if (next == LearningStageId.None)
            {
                _destination = LearningStageId.None;
                _passagePrepared = true;
                _factory.AllowExit(_currentStage);
                RequestEvaluation();
            }
            else if (next == LearningStageId.Convolution)
            {
                _destination = next;
                _passagePrepared = true;
                RequestEvaluation();
            }
            else { BeginOperation(PrepareNextLesson(next)); }
        }

        private void BeginOperation(IEnumerator operation)
        {
            CancelRetry();
            _transitioning = true;
            PublishState();
            StartCoroutine(operation);
        }

        private void FinishOperation()
        {
            _transitioning = false;
            PublishState();
            RequestEvaluation();
        }

        private void Fail(string message, bool retry)
        {
            Debug.LogError("[LearningSession] " + message, this);
            if (retry) { _retry = StartCoroutine(RetryAfterDelay()); }
            FinishOperation();
        }

        private IEnumerator RetryAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            _retry = null;
            RequestEvaluation();
        }

        private void CancelRetry()
        {
            if (_retry == null) { return; }
            StopCoroutine(_retry);
            _retry = null;
        }

        private IEnumerator EnterTutorial()
        {
            _factory.BeginEntry(LearningStageId.Tutorial);
            yield return _sceneLoader.Prepare(LearningStageId.Tutorial, _factory.GetPlacement(LearningStageId.Tutorial));
            if (_sceneLoader.LastError != null)
            {
                _factory.SetEntranceAvailable(LearningStageId.Tutorial, true);
                _factory.ShowDestination(LearningStageId.Tutorial);
                Fail(_sceneLoader.LastError, true);
                yield break;
            }
            ConfigurePrepared(LearningStageId.Tutorial);
            ConnectPrepared(LearningStageId.Tutorial);
            _factory.ActivateEnvironment();
            _sceneLoader.ActivatePrepared();
            FinishOperation();
        }

        private IEnumerator PrepareNextLesson(LearningStageId next)
        {
            // A finished tutorial releases its input and UI before the first lesson is prepared.
            if (_currentStage == LearningStageId.Tutorial && _tutorial != null)
                yield return EndCurrentLesson();
            yield return _sceneLoader.Prepare(next, _factory.GetPlacement(next));
            if (_sceneLoader.LastError != null) { Fail(_sceneLoader.LastError, true); yield break; }
            ConfigurePrepared(next);
            if (!_canProceed)
            {
                yield return _sceneLoader.CancelPrepared();
                FinishOperation();
                yield break;
            }
            _destination = next;
            _passagePrepared = true;
            // The current exit opens before collecting documents outside it.
            _factory.SetNextStagePrepared(_currentStage, next);
            FinishOperation();
        }

        private IEnumerator EnterPreparedLesson(LearningStageId stage)
        {
            _factory.BeginEntry(stage);
            yield return EndCurrentLesson();
            ConnectPrepared(stage);
            _factory.ActivateEnvironment();
            _sceneLoader.ActivatePrepared();
            _factory.StageActivated(stage);
            FinishOperation();
        }

        private IEnumerator StartConvolution()
        {
            yield return _pixels.StartConvolution();
            if (_pixels.LoadError != null) { Fail(_pixels.LoadError, true); yield break; }
            FinishOperation();
        }

        private IEnumerator EnterEnding()
        {
            _factory.BeginExit();
            _factory.ReleaseSceneObjects(_sceneLoader.CurrentScene);
            yield return _sceneLoader.LoadEnding();
            if (!_sceneLoader.IsLeaving)
            {
                _factory.AllowExit(_currentStage);
                Fail(_sceneLoader.LastError, true);
                yield break;
            }
            _leaving = true;
        }

        public void RequestStageMove(LearningStageId stage)
        {
            bool known = false;
            foreach (LearningStageDefinition definition in LearningStageCatalog.ProgressStages)
                if (definition.Id == stage) { known = true; break; }
            if (!known) { return; }

            // Grant on request; rejection, loading failure and backward travel never revoke documents.
            foreach (LearningStageDefinition definition in LearningStageCatalog.ProgressStages)
            {
                _parchment.CollectDocuments(definition.EntranceParchmentIds);
                if (definition.Id == stage) { break; }
            }
            if (!CanTravel) { return; }
            _tablet.ShowTravelStarted();
            BeginOperation(MoveToStage(stage));
        }

        private IEnumerator MoveToStage(LearningStageId stage)
        {
            yield return null;
            _passagePrepared = false;
            if (_sceneLoader.HasPreparedLesson) { yield return _sceneLoader.CancelPrepared(); }
            yield return EndCurrentLesson();
            SetCurrentStage(LearningStageId.None);
            PublishState();
            yield return _sceneLoader.Prepare(stage, _factory.GetPlacement(stage));
            if (_sceneLoader.LastError != null)
            {
                _tablet.ShowTravelFailed();
                Fail(_sceneLoader.LastError, false);
                yield break;
            }
            ConfigurePrepared(stage);
            _factory.ResetForStageMove();
            ConnectPrepared(stage);
            _factory.ActivateEnvironment();
            _sceneLoader.ActivatePrepared();
            _factory.MovePlayerToStage(stage);
            // The Pixel manager starts its nested scene load in Start.
            yield return null;
            if (_pixels != null)
            {
                while (_pixels.IsSwitching) { yield return null; }
                if (_pixels.LoadError != null)
                {
                    _tablet.ShowTravelFailed();
                    Fail(_pixels.LoadError, false);
                    yield break;
                }
            }
            _tablet.ShowTravelCompleted();
            FinishOperation();
        }

        private void ConfigurePrepared(LearningStageId stage)
        {
            if (stage == LearningStageId.Tutorial)
                _sceneLoader.PreparedTutorial.Initialize(_rig, _factory.TutorialPose);
            if (LearningStageCatalog.Get(stage).Room == RoomId.PixelLab)
                _sceneLoader.PreparedPixels.ConfigureHostedStages(_factory.ConvolutionPose, stage == LearningStageId.Convolution);
        }

        private void SetCurrentStage(LearningStageId stage)
        {
            _currentStage = stage;
            _destination = stage;
            _passagePrepared = false;
            _canProceed = false;
        }

        private void ConnectPrepared(LearningStageId stage)
        {
            SetCurrentStage(stage);
            _tutorial = _sceneLoader.PreparedTutorial;
            _pixels = _sceneLoader.PreparedPixels;
            _maps = _sceneLoader.PreparedMaps;
            _model = _sceneLoader.PreparedModel;
            SubscribeLesson();
            ReadLessonState();
        }

        private void SubscribeLesson()
        {
            if (_tutorial != null)
            {
                _tutorial.StageCompleted += HandleStageCompleted;
                _tutorial.ProgressionAvailabilityChanged += HandleAvailabilityChanged;
            }
            if (_pixels != null)
            {
                _pixels.StageCompleted += HandleStageCompleted;
                _pixels.ProgressionAvailabilityChanged += HandleAvailabilityChanged;
                _pixels.ConvolutionStarted += HandleConvolutionStarted;
            }
            if (_maps != null)
            {
                _maps.StageCompleted += HandleStageCompleted;
                _maps.ProgressionAvailabilityChanged += HandleAvailabilityChanged;
            }
            if (_model != null)
            {
                _model.StageCompleted += HandleStageCompleted;
                _model.ProgressionAvailabilityChanged += HandleAvailabilityChanged;
            }
        }

        private void UnsubscribeLesson()
        {
            if (_tutorial != null)
            {
                _tutorial.StageCompleted -= HandleStageCompleted;
                _tutorial.ProgressionAvailabilityChanged -= HandleAvailabilityChanged;
            }
            if (_pixels != null)
            {
                _pixels.StageCompleted -= HandleStageCompleted;
                _pixels.ProgressionAvailabilityChanged -= HandleAvailabilityChanged;
                _pixels.ConvolutionStarted -= HandleConvolutionStarted;
            }
            if (_maps != null)
            {
                _maps.StageCompleted -= HandleStageCompleted;
                _maps.ProgressionAvailabilityChanged -= HandleAvailabilityChanged;
            }
            if (_model != null)
            {
                _model.StageCompleted -= HandleStageCompleted;
                _model.ProgressionAvailabilityChanged -= HandleAvailabilityChanged;
            }
        }

        private void ReadLessonState()
        {
            if (_currentStage == LearningStageId.None) { return; }
            foreach (LearningStageDefinition stage in LearningStageCatalog.Stages)
            {
                bool complete = _tutorial != null ? _tutorial.HasCompleted(stage.Id)
                    : _pixels != null ? _pixels.HasCompleted(stage.Id)
                    : _maps != null ? _maps.HasCompleted(stage.Id)
                    : _model != null && _model.HasCompleted(stage.Id);
                if (complete) { _progress.RecordCompletion(stage.Id); }
            }
            _canProceed = _tutorial != null ? _tutorial.CanProceed(_currentStage)
                : _pixels != null ? _pixels.CanProceed(_currentStage)
                : _maps != null ? _maps.CanProceed(_currentStage)
                : _model != null && _model.CanProceed(_currentStage);
            PublishState();
        }

        private IEnumerator EndCurrentLesson()
        {
            CNNPixelLabManager pixels = _pixels;
            UnsubscribeLesson();
            _tutorial = null;
            _pixels = null;
            _maps = null;
            _model = null;
            if (pixels != null) { yield return pixels.EndSession(); }
            _factory.ReleaseSceneObjects(_sceneLoader.CurrentScene);
            yield return _sceneLoader.UnloadCurrent();
        }
    }
}
