using System;
using System.Collections.Generic;
using Learning;
using Parchment;
using Tablet;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Coordinates the current stage and travel.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [FormerlySerializedAs("_navigationData")]
        [SerializeField] private FactoryLayoutData _layoutData;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private LearningSessionManager _session;
        [SerializeField] private FactoryPlayerController _player;
        [SerializeField] private FactoryTeleportFeedbackController _teleportFeedback;
        private TabletManager _tablet;
        private ParchmentManager _parchment;
        private readonly HashSet<Transform> _usedTeleportArrivals = new HashSet<Transform>();
        private bool _teleportRequested;
        private LearningStageId _travelStage;
        private bool _tutorialAvailable;

        public LearningStageId CurrentStage { get; private set; }
        public event Action<LearningStageId> StageChanged;
        public event Action TravelStarted;
        public event Action<LearningStageId, bool> TravelFinished;
        public bool CanTravel => !_sceneLoader.IsBusy
            && (CurrentStage != LearningStageId.Tutorial || _tutorialAvailable);

        private void Awake()
        {
            _tablet = FindFirstObjectByType<TabletManager>();
            _parchment = FindFirstObjectByType<ParchmentManager>();
        }

        private void OnEnable()
        {
            _tablet.StageMoveRequested += HandleStageMoveRequested;
            _player.TeleportRequested += HandleTeleportRequested;
            _areaController.AreaChecked += HandleAreaChecked;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
            _areaController.AreaChanged += HandleAreaChanged;
            _sceneLoader.StateChanged += HandleLoadingStateChanged;
            _sceneLoader.LoadFailed += HandleLoadFailed;
        }

        private void OnDisable()
        {
            if (_tablet != null)
            {
                _tablet.StageMoveRequested -= HandleStageMoveRequested;
            }
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
            _player.TeleportRequested -= HandleTeleportRequested;
            _areaController.AreaChecked -= HandleAreaChecked;
            _teleportRequested = false;
            _areaController.AreaChanged -= HandleAreaChanged;
            _sceneLoader.StateChanged -= HandleLoadingStateChanged;
            _sceneLoader.LoadFailed -= HandleLoadFailed;
        }

        private void Start()
        {
            // OnEnable runs before an asynchronously loaded scene becomes available.
            SceneManager.SetActiveScene(gameObject.scene);
        }

        private void HandleTeleportRequested() => _teleportRequested = true;

        private void HandleAreaChecked()
        {
            if (!_teleportRequested) { return; }
            _teleportRequested = false;
            if (!_sceneLoader.IsBusy && !_parchment.IsOpen)
            {
                FactoryAreaId source = _areaController.GetTeleportSource();
                foreach (FactoryLayoutData.TeleportPoint point in _layoutData.TeleportPoints)
                {
                    if (point.Source != source || !CanUseTeleportPoint(point)) { continue; }
                    // Onward travel takes priority. Do not send the player backwards if it was already used.
                    if (!_usedTeleportArrivals.Add(point.Arrival)) { break; }
                    _teleportFeedback.Hide();
                    _player.TeleportTo(point.Arrival);
                    return;
                }
            }
            _teleportFeedback.ShowDenied(_player.CameraTransform);
        }

        private bool CanUseTeleportPoint(FactoryLayoutData.TeleportPoint point)
        {
            if (point.RequiredCompletion != LearningStageId.None && !_session.IsCompleted(point.RequiredCompletion))
            {
                return false;
            }
            if (point.Source == FactoryAreaId.ModelDesignRoom && point.RequiredCompletion == LearningStageId.ModelDesign
                && !_session.CanExitStage(LearningStageId.ModelDesign))
            {
                return false;
            }
            if (point.RequiredParchmentStage == LearningStageId.None) { return true; }
            foreach (int documentId in LearningStageCatalog.Get(point.RequiredParchmentStage).EntranceParchmentIds)
            {
                if (!_parchment.HasRead(documentId)) { return false; }
            }
            return true;
        }

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (!_layoutData.TryGetAdjacentAreas(area, out _, out _)) { return; }
            LearningStageId stage = area.Stage ?? LearningStageId.None;
            CurrentStage = stage;
            StageChanged?.Invoke(stage);
        }

        private void HandleLoadingStateChanged()
        {
            if (_sceneLoader.IsBusy) { return; }
            if (_travelStage != LearningStageId.None)
                FinishTravel(_travelStage, _sceneLoader.IsLoaded(_travelStage));
        }

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            if (stage == LearningStageId.Tutorial)
            {
                _tutorialAvailable = available;
            }
        }

        private void HandleStageMoveRequested(LearningStageId stage)
        {
            bool known = false;
            foreach (LearningStageDefinition definition in LearningStageCatalog.ProgressStages)
                if (definition.Id == stage) { known = true; break; }
            if (!known || !CanTravel) { return; }
            _travelStage = stage;
            TravelStarted?.Invoke();
            // Movement produces the same AreaChanged event as walking.
            _player.MoveToStage(stage);
            if (CurrentStage == stage && _sceneLoader.IsLoaded(stage)) { FinishTravel(stage, true); }
        }

        private void FinishTravel(LearningStageId stage, bool succeeded)
        {
            _travelStage = LearningStageId.None;
            TravelFinished?.Invoke(stage, succeeded);
        }

        private void HandleLoadFailed(LearningStageId stage, string message)
        {
            if (_travelStage == stage) { FinishTravel(stage, false); }
        }

    }
}
