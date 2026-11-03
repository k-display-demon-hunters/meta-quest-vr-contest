using System;
using System.Collections.Generic;
using Learning;
using Parchment;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns space, placement and passage conditions, responding to scene lifecycle results.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [FormerlySerializedAs("_navigationData")]
        [SerializeField] private FactoryLayoutData _layoutData;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private LearningSessionManager _session;
        [SerializeField] private FactoryPlayerController _player;
        [SerializeField] private FactoryRouteGuideController _routeGuide;
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
            foreach (FactoryLayoutData.StageSpace room in _layoutData.StageSpaces)
            {
                room.EntryDoor.Bind(_player.CameraTransform);
                if (room.Id != LearningStageId.Exit) { room.ExitDoor.Bind(_player.CameraTransform); }
            }
            SetEntranceAvailable(LearningStageId.Tutorial, true);
            ShowDestination(LearningStageId.Tutorial);
        }

        public void SetEntranceAvailable(LearningStageId stage, bool available)
        {
            FactoryShutterController door = _layoutData.GetStageSpace(stage).EntryDoor;
            if (available) { door.Open(); } else { door.Close(); }
        }

        private void HandleAreaChanged(FactoryAreaId area)
        {
            int index = _layoutData.GetAreaIndex(area);
            
            LearningStageId stage = _layoutData.AreaOrder[index].Stage ?? LearningStageId.None;
            if (stage != LearningStageId.None && stage != CurrentStage) { HandleStageEntryStarted(stage); }
            CurrentStage = stage;
            _preparationSource = stage != LearningStageId.None ? stage
                : index > 0 ? _layoutData.AreaOrder[index - 1].Stage ?? LearningStageId.None : LearningStageId.None;
            _preparedStage = index + 1 < _layoutData.AreaOrder.Length
                ? _layoutData.AreaOrder[index + 1].Stage ?? LearningStageId.None : LearningStageId.None;
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
                if (_preparedStage != LearningStageId.None) { SetEntranceAvailable(_preparedStage, false); }
                ShowDestination(LearningStageId.None);
                return;
            }
            if (_preparedStage == LearningStageId.None)
            {
                _layoutData.GetStageSpace(_preparationSource).ExitDoor.Open();
                ShowDestination(LearningStageId.None);
                return;
            }
            if (_preparedStage == LearningStageId.Exit) { AllowExit(_preparationSource); return; }
            SetNextStagePrepared(_preparationSource, _preparedStage);
            SetEntranceAvailable(_preparedStage,
                _parchment.HasCollectedAll(LearningStageCatalog.Get(_preparedStage).EntranceParchmentIds));
        }

        private void HandleStageEntryStarted(LearningStageId stage)
        {
            _preparationSource = LearningStageId.None;
            _preparedStage = LearningStageId.None;
            if (stage == LearningStageId.Exit)
            {
                BeginExit();
            }
            else { BeginEntry(stage); }
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
                ResetForStageMove();
                RefreshPreparedEntrance();
            }
            TravelFinished?.Invoke(stage, succeeded);
        }

        private void HandleLoadFailed(LearningStageId stage, string message)
        {
            if (_travelStage == stage) { FinishTravel(stage, false); }
            if (stage == LearningStageId.Tutorial)
            {
                SetEntranceAvailable(stage, true);
                ShowDestination(stage);
            }
            else if (stage == LearningStageId.Exit)
            {
                SetEntranceAvailable(stage, true);
                ShowDestination(stage);
            }
        }

        public void ShowDestination(LearningStageId stage) =>
            _routeGuide.SetDestination(stage);

        public void SetNextStagePrepared(LearningStageId current, LearningStageId next)
        {
            FactoryShutterController exit = _layoutData.GetStageSpace(current).ExitDoor;
            if (exit != _layoutData.GetStageSpace(next).EntryDoor) { exit.Open(); }
            ShowDestination(next);
        }

        public void BeginEntry(LearningStageId stage)
        {
            SetEntranceAvailable(stage, false);
            _routeGuide.SetDestination(LearningStageId.None);
        }


        public void AllowExit(LearningStageId current)
        {
            if (current != LearningStageId.None) { _layoutData.GetStageSpace(current).ExitDoor.Open(); }
            _layoutData.GetStageSpace(LearningStageId.Exit).EntryDoor.Open();
            _routeGuide.SetDestination(LearningStageId.Exit);
        }

        public void BeginExit()
        {
            _layoutData.GetStageSpace(LearningStageId.Exit).EntryDoor.Close();
            _routeGuide.SetDestination(LearningStageId.None);
        }

        public void ResetForStageMove()
        {
            foreach (FactoryLayoutData.StageSpace room in _layoutData.StageSpaces)
            {
                room.EntryDoor.Close();
                if (room.Id != LearningStageId.Exit) { room.ExitDoor.Close(); }
            }
            _routeGuide.SetDestination(LearningStageId.None);
        }

    }
}
