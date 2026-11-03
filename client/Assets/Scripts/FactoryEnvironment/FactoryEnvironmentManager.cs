using System;
using System.Collections;
using System.Collections.Generic;
using Learning;
using Parchment;
using Tablet;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns space, placement and passage conditions, responding to scene lifecycle results.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [Serializable]
        private sealed class StageArrival
        {
            public LearningStageId Stage;
            public Transform Arrival;
        }

        [FormerlySerializedAs("_navigationData")]
        [SerializeField] private FactoryLayoutData _layoutData;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private LearningSessionManager _session;
        [SerializeField] private FactoryPlayerController _player;
        [SerializeField] private FactoryRouteGuideController _routeGuide;
        [SerializeField] private FactoryMapController _map;
        [SerializeField] private StageArrival[] _stageArrivals;
        private ParchmentManager _parchment;
        private LearningStageId _preparedStage;
        private LearningStageId _preparationSource;
        private LearningStageId _travelStage;
        private TabletManager _tablet;
        private readonly Dictionary<LearningStageId, bool> _availability = new Dictionary<LearningStageId, bool>();

        public LearningStageId CurrentStage { get; private set; }
        public event Action<LearningStageId> StageChanged;
        public event Action TravelStarted;
        public event Action<LearningStageId, bool> TravelFinished;
        public bool CanTravel => !_sceneLoader.IsBusy
            && (CurrentStage != LearningStageId.Tutorial || IsStageAvailable(LearningStageId.Tutorial));
        public event Action<TabletMapData> MapChanged;
        public TabletMapData MapData => _map.MapData;
        public Pose TutorialPose => ToPose(_layoutData.TutorialStart);

        private void Awake()
        {
            _parchment = FindFirstObjectByType<ParchmentManager>();
            _tablet = FindFirstObjectByType<TabletManager>();
        }

        private void OnEnable()
        {
            _map.MapChanged += PublishMap;
            _parchment.Collected += HandleParchmentCollected;
            _tablet.StageMoveRequested += HandleStageMoveRequested;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
            _sceneLoader.ContentLoaded += HandleContentLoaded;
            _sceneLoader.ContentUnloading += HandleContentUnloading;
            _areaController.AreaChanged += HandleAreaChanged;
            _sceneLoader.StateChanged += HandleLoadingStateChanged;
            _sceneLoader.LoadFailed += HandleLoadFailed;
            ActivateEnvironment();
            _map.Initialize();
        }

        private void OnDisable()
        {
            _map.MapChanged -= PublishMap;
            _parchment.Collected -= HandleParchmentCollected;
            _tablet.StageMoveRequested -= HandleStageMoveRequested;
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
            _sceneLoader.ContentLoaded -= HandleContentLoaded;
            _sceneLoader.ContentUnloading -= HandleContentUnloading;
            _areaController.AreaChanged -= HandleAreaChanged;
            _sceneLoader.StateChanged -= HandleLoadingStateChanged;
            _sceneLoader.LoadFailed -= HandleLoadFailed;
        }

        private void Start()
        {
            _player.PlaceAtEntrance(FindFirstObjectByType<XROrigin>());
            _player.PrepareRayInput();
            foreach (FactoryLayoutData.StageSpace room in _layoutData.StageSpaces)
            {
                room.EntryDoor.Bind(_player.CameraTransform);
                if (room.Id != LearningStageId.Exit) { room.ExitDoor.Bind(_player.CameraTransform); }
            }
            SetEntranceAvailable(LearningStageId.Tutorial, true);
            ShowDestination(LearningStageId.Tutorial);
        }

        private void PublishMap(TabletMapData map) => MapChanged?.Invoke(map);
        private static Pose ToPose(Transform anchor) => new Pose(anchor.position, anchor.rotation);

        public void ActivateEnvironment() => SceneManager.SetActiveScene(gameObject.scene);
        public void ReleaseSceneObjects(Scene scene) => _player.ReleaseSceneObjects(scene);

        private void HandleContentLoaded(LearningStageId stage, Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is ILearningExperience experience && experience.StageId == stage)
                    {
                        foreach (GameObject other in scene.GetRootGameObjects())
                            if (other != root) { other.SetActive(false); }
                        PlaceContent(stage, root);
                        return;
                    }
                }
            }
        }

        private void PlaceContent(LearningStageId stage, GameObject content)
        {
            if (stage == LearningStageId.Tutorial)
            {
                content.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                content.transform.localScale = Vector3.one;
            }
            else
            {
                Transform anchor = _layoutData.GetStageSpace(stage).ContentAnchor;
                content.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                content.transform.localScale = anchor.lossyScale;
                foreach (Light light in content.GetComponentsInChildren<Light>(true)) { light.enabled = false; }
            }
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
        }

        public void SetEntranceAvailable(LearningStageId stage, bool available)
        {
            FactoryShutterController door = _layoutData.GetStageSpace(stage).EntryDoor;
            if (available) { door.Open(); } else { door.Close(); }
        }

        private IEnumerator HandleContentUnloading(LearningStageId stage, Scene scene)
        {
            ReleaseSceneObjects(scene);
            yield break;
        }

        private void HandleAreaChanged(FactoryAreaId area)
        {
            for (int i = 0; i < _layoutData.AreaOrder.Length; i++)
            {
                if (_layoutData.AreaOrder[i].Id != area) { continue; }
                LearningStageId stage = _layoutData.AreaOrder[i].Stage ?? LearningStageId.None;
                if (stage != LearningStageId.None && stage != CurrentStage) { HandleStageEntryStarted(stage); }
                CurrentStage = stage;
                _preparationSource = stage != LearningStageId.None ? stage
                    : i > 0 ? _layoutData.AreaOrder[i - 1].Stage ?? LearningStageId.None : LearningStageId.None;
                _preparedStage = i + 1 < _layoutData.AreaOrder.Length
                    ? _layoutData.AreaOrder[i + 1].Stage ?? LearningStageId.None : LearningStageId.None;
                StageChanged?.Invoke(stage);
                RefreshPreparedEntrance();
                return;
            }
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
                foreach (FactoryLayoutData.StageSpace space in _layoutData.StageSpaces)
                {
                    if (_sceneLoader.IsLoaded(space.Id))
                        ReleaseSceneObjects(SceneManager.GetSceneByName(LearningStageCatalog.GetSceneName(space.Id)));
                }
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
            MovePlayerToStage(stage);
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

        public void MovePlayerToStage(LearningStageId stage)
        {
            foreach (StageArrival arrival in _stageArrivals)
            {
                if (arrival.Stage != stage) { continue; }
                _player.MoveTo(arrival.Arrival);
                return;
            }
            throw new InvalidOperationException("No arrival for " + stage);
        }
    }
}
