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
        private LearningStageId _routeDestination;
        private LearningStageId _routeSource;
        private LearningStageId _travelStage;
        private readonly HashSet<FactoryShutterId> _permittedShutters = new HashSet<FactoryShutterId>();
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
            _shutters.Configure(CanOpenShutter);
        }

        private void OnEnable()
        {
            _parchment.Collected += HandleParchmentCollected;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
            _areaController.AreaChanged += HandleAreaChanged;
            _sceneLoader.StateChanged += HandleLoadingStateChanged;
            _sceneLoader.LoadFailed += HandleLoadFailed;

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
            // OnEnable runs before an asynchronously loaded scene becomes available.
            SceneManager.SetActiveScene(gameObject.scene);
            _routeGuide.SetDestination(LearningStageId.Tutorial);
        }

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (!_layoutData.TryGetAdjacentAreas(area, out var previous, out var next)) { return; }
            LearningStageId stage = area.Stage ?? LearningStageId.None;
            CurrentStage = stage;
            _routeSource = area.Stage ?? previous?.Stage ?? LearningStageId.None;
            _routeDestination = next?.Stage ?? LearningStageId.None;
            StageChanged?.Invoke(stage);
            RefreshShutterConditions();
        }

        private void HandleLoadingStateChanged()
        {
            if (_sceneLoader.IsBusy) { return; }
            if (_travelStage != LearningStageId.None)
                FinishTravel(_travelStage, _sceneLoader.IsLoaded(_travelStage));
            RefreshShutterConditions();
        }

        private bool IsStageAvailable(LearningStageId stage) =>
            _availability.TryGetValue(stage, out bool available) && available;

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            _availability[stage] = available;
            RefreshShutterConditions();
        }

        private void HandleParchmentCollected(int documentId) => RefreshShutterConditions();

        private void RefreshShutterConditions()
        {
            _shutters.RefreshConditions();
            RefreshRouteGuide();
        }

        private bool CanOpenShutter(FactoryShutterId id)
        {
            if (_sceneLoader.IsBusy) { return false; }
            foreach (FactoryLayoutData.StageSpace space in _layoutData.StageSpaces)
            {
                if (space.EntryShutter != id && space.ExitShutter != id) { continue; }
                // The ending is loaded only after entering the rear court.
                if (space.Id != LearningStageId.Exit && !_sceneLoader.IsLoaded(space.Id)) { return false; }
            }
            if (_permittedShutters.Contains(id)) { return true; }

            foreach (FactoryLayoutData.StageSpace space in _layoutData.StageSpaces)
            {
                if (space.ExitShutter == id && !IsStageAvailable(space.Id)) { return false; }
                if (space.EntryShutter == id && !CanEnterStage(space.Id)) { return false; }
            }
            // Keep an earned passage permission across closing, resets and scene reloads.
            _permittedShutters.Add(id);
            return true;
        }

        private bool CanEnterStage(LearningStageId stage)
        {
            LearningStageId previous = LearningStageId.None;
            foreach (LearningStageDefinition definition in LearningStageCatalog.Stages)
            {
                if (definition.Id == stage)
                {
                    return (previous == LearningStageId.None || IsStageAvailable(previous))
                        && _parchment.HasCollectedAll(definition.EntranceParchmentIds);
                }
                previous = definition.Id;
            }
            // Exit follows the last learning stage and has no entrance parchment.
            return stage == LearningStageId.Exit && IsStageAvailable(previous);
        }

        private void RefreshRouteGuide()
        {
            if (_sceneLoader.IsBusy || _routeSource == LearningStageId.None
                || _routeSource == LearningStageId.Exit || _routeDestination == LearningStageId.None) { return; }
            if (!_sceneLoader.IsLoaded(_routeSource) || !IsStageAvailable(_routeSource)) { return; }
            if (_routeDestination != LearningStageId.Exit && !_sceneLoader.IsLoaded(_routeDestination)) { return; }
            _routeGuide.SetDestination(_routeDestination);
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
            if (stage == LearningStageId.Tutorial || stage == LearningStageId.Exit)
            {
                _routeGuide.SetDestination(stage);
            }
        }

    }
}
