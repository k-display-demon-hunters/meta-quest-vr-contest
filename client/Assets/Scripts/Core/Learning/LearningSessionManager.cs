using System;
using System.Collections;
using FactoryEnvironment;
using FactoryTutorial;
using Parchment;
using Tablet;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Learning
{
    // Scene-scoped learning session: one owner for progress, transitions and module connections.
    public sealed class LearningSessionManager : MonoBehaviour
    {
        [SerializeField] private FactoryEnvironmentManager _factory;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        private readonly LearningProgress _progress = new LearningProgress();
        private TabletManager _tablet;
        private ParchmentManager _parchment;
        private XROrigin _rig;
        private FactoryTutorialManager _tutorial;
        private ILearningExperience _experience;
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
                _factory.SetEntranceAvailable(_destination, available);
                if (available && _factory.IsStageAreaInside(_destination))
                {
                    BeginOperation(EnterPreparedLesson(_destination));
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
            yield return _sceneLoader.Prepare(LearningStageId.Tutorial);
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
            yield return _sceneLoader.Prepare(next);
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
                _parchment.CollectDocuments(definition.ParchmentIds);
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
            yield return _sceneLoader.Prepare(stage);
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
            _tablet.ShowTravelCompleted();
            FinishOperation();
        }

        private void ConfigurePrepared(LearningStageId stage)
        {
            if (stage == LearningStageId.Tutorial)
                _sceneLoader.PreparedTutorial.Initialize(_rig, _factory.TutorialPose);

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
            _experience = _sceneLoader.PreparedExperience;
            SubscribeLesson();
            ReadLessonState();
        }

        private void SubscribeLesson()
        {
            if (_experience != null)
            {
                _experience.ProgressChanged += HandleExperienceProgress;
                _experience.ExitAvailabilityChanged += HandleExperienceExit;
            }
            if (_tutorial != null)
            {
                _tutorial.StageCompleted += HandleStageCompleted;
                _tutorial.ProgressionAvailabilityChanged += HandleAvailabilityChanged;
            }
        }

        private void UnsubscribeLesson()
        {
            if (_experience != null)
            {
                _experience.ProgressChanged -= HandleExperienceProgress;
                _experience.ExitAvailabilityChanged -= HandleExperienceExit;
            }
            if (_tutorial != null)
            {
                _tutorial.StageCompleted -= HandleStageCompleted;
                _tutorial.ProgressionAvailabilityChanged -= HandleAvailabilityChanged;
            }
        }

        private void HandleExperienceProgress(float progress)
        {
            if (_progress.RecordProgress(_experience.StageId, progress)) { PublishState(); }
            RequestEvaluation();
        }

        private void HandleExperienceExit(bool available)
        {
            _canProceed = available;
            RequestEvaluation();
        }

        private void ReadLessonState()
        {
            if (_currentStage == LearningStageId.None) { return; }
            if (_experience != null)
            {
                _progress.RecordProgress(_experience.StageId, _experience.Progress);
                _canProceed = _experience.CanExit;
            }
            else if (_tutorial != null)
            {
                if (_tutorial.HasCompleted(LearningStageId.Tutorial))
                    _progress.RecordCompletion(LearningStageId.Tutorial);
                _canProceed = _tutorial.CanProceed(LearningStageId.Tutorial);
            }
            PublishState();
        }

        private IEnumerator EndCurrentLesson()
        {
            ILearningExperience experience = _experience;
            UnsubscribeLesson();
            _experience = null;
            _tutorial = null;
            if (experience != null) { yield return experience.EndSession(); }
            _factory.ReleaseSceneObjects(_sceneLoader.CurrentScene);
            yield return _sceneLoader.UnloadCurrent();
        }
    }
}
