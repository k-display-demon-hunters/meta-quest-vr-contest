using System;
using System.Collections.Generic;
using Learning;
using Parchment;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Coordinates progression, passage conditions and route guidance.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [FormerlySerializedAs("_navigationData")]
        [SerializeField] private FactoryLayoutData _layoutData;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private LearningSessionManager _session;
        [SerializeField] private FactoryPlayerController _player;
        [SerializeField] private FactoryRouteGuideController _routeGuide;
        [SerializeField] private FactoryShuttersManager _shutters;
        private ParchmentManager _parchment;
        private LearningStageId _preparedStage;
        private LearningStageId _preparationSource;
        private LearningStageId _travelStage;
        private readonly Dictionary<LearningStageId, bool> _availability = new Dictionary<LearningStageId, bool>();

        public LearningStageId CurrentStage { get; private set; }
        public event Action<LearningStageId> StageChanged;
        public event Action TravelStarted;
        public event Action<LearningStageId, bool> TravelFinished;
        public bool CanTravel => !_sceneLoader.IsBusy
            && (CurrentStage != LearningStageId.Tutorial || IsStageAvailable(LearningStageId.Tutorial));

        private void Awake()
        {
            _parchment = FindFirstObjectByType<ParchmentManager>();
        }

        private void OnEnable()
        {
            _parchment.Collected += HandleParchmentCollected;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
            _areaController.AreaChanged += HandleAreaChanged;
            _sceneLoader.StateChanged += HandleLoadingStateChanged;
            _sceneLoader.LoadFailed += HandleLoadFailed;
            SceneManager.SetActiveScene(gameObject.scene);
        }

        private void OnDisable()
        {
            _parchment.Collected -= HandleParchmentCollected;
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
            _areaController.AreaChanged -= HandleAreaChanged;
            _sceneLoader.StateChanged -= HandleLoadingStateChanged;
            _sceneLoader.LoadFailed -= HandleLoadFailed;
        }

        private void Start()
        {
            _routeGuide.SetDestination(LearningStageId.Tutorial);
        }

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (!_layoutData.TryGetAdjacentAreas(area, out var previous, out var next)) { return; }
            LearningStageId stage = area.Stage ?? LearningStageId.None;
            if (stage != LearningStageId.None && stage != CurrentStage) { HandleStageEntryStarted(stage); }
            CurrentStage = stage;
            _preparationSource = area.Stage ?? previous?.Stage ?? LearningStageId.None;
            _preparedStage = next?.Stage ?? LearningStageId.None;
            StageChanged?.Invoke(stage);
            RefreshPreparedEntrance();
        }

        private void HandleLoadingStateChanged()
        {
            if (_sceneLoader.IsBusy) { return; }
            if (_travelStage != LearningStageId.None)
                FinishTravel(_travelStage, _sceneLoader.IsLoaded(_travelStage));
            RefreshPreparedEntrance();
        }

        private bool IsStageAvailable(LearningStageId stage) =>
            _availability.TryGetValue(stage, out bool available) && available;

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            _availability[stage] = available;
            if (stage == _preparationSource) { RefreshPreparedEntrance(); }
        }

        private void HandleParchmentCollected(int documentId) => RefreshPreparedEntrance();

        private void RefreshPreparedEntrance()
        {
            if (_preparationSource == LearningStageId.None || _preparationSource == LearningStageId.Exit) { return; }
            if (_sceneLoader.IsBusy || !_sceneLoader.IsLoaded(_preparationSource)) { return; }
            if (_preparedStage != LearningStageId.None && _preparedStage != LearningStageId.Exit
                && !_sceneLoader.IsLoaded(_preparedStage)) { return; }
            if (CurrentStage != LearningStageId.None && _preparationSource != CurrentStage) { return; }
            if (!IsStageAvailable(_preparationSource))
            {
                if (_preparedStage != LearningStageId.None) { _shutters.SetEntranceAvailable(_preparedStage, false); }
                return;
            }
            if (_preparedStage == LearningStageId.None)
            {
                _shutters.OpenExit(_preparationSource);
                return;
            }
            if (_preparedStage == LearningStageId.Exit)
            {
                _shutters.AllowFactoryExit(_preparationSource);
                _routeGuide.SetDestination(LearningStageId.Exit);
                return;
            }
            _shutters.OpenPassage(_preparationSource, _preparedStage);
            _routeGuide.SetDestination(_preparedStage);
            _shutters.SetEntranceAvailable(_preparedStage,
                _parchment.HasCollectedAll(LearningStageCatalog.Get(_preparedStage).EntranceParchmentIds));
        }

        private void HandleStageEntryStarted(LearningStageId stage)
        {
            _preparationSource = LearningStageId.None;
            _preparedStage = LearningStageId.None;
            _shutters.SetEntranceAvailable(stage, false);
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
            if (succeeded)
            {
                _shutters.CloseAll();
                RefreshPreparedEntrance();
            }
            TravelFinished?.Invoke(stage, succeeded);
        }

        private void HandleLoadFailed(LearningStageId stage, string message)
        {
            if (_travelStage == stage) { FinishTravel(stage, false); }
            if (stage == LearningStageId.Tutorial)
            {
                _shutters.SetEntranceAvailable(stage, true);
                _routeGuide.SetDestination(stage);
            }
            else if (stage == LearningStageId.Exit)
            {
                _shutters.SetEntranceAvailable(stage, true);
                _routeGuide.SetDestination(stage);
            }
        }

    }
}
