using System;
using System.Collections.Generic;
using Learning;
using Tablet;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns the physical factory. Learning policy and lesson connections belong to the session.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [Serializable]
        private sealed class StageArrival
        {
            public LearningStageId Stage;
            public Transform Arrival;
        }

        [SerializeField] private FactoryNavigationData _navigationData;
        [SerializeField] private FactoryPlayerController _player;
        [SerializeField] private FactoryRouteGuideController _routeGuide;
        [SerializeField] private FactoryMapController _map;
        [SerializeField] private FactoryAreaEntryController _convolutionArea;
        [SerializeField] private Transform _convolutionAnchor;
        [SerializeField] private StageArrival[] _stageArrivals;
        private readonly Dictionary<FactoryAreaEntryController, Action<bool>> _areaHandlers =
            new Dictionary<FactoryAreaEntryController, Action<bool>>();

        public event Action<LearningStageId, bool> StageAreaChanged;
        public event Action<bool> ExitAreaChanged;
        public event Action<TabletMapData> MapChanged;
        public TabletMapData MapData => _map.MapData;
        public Pose TutorialPose => ToPose(_navigationData.TutorialStart);
        public Pose ConvolutionPose => ToPose(_convolutionAnchor);

        private void Awake()
        {
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                RoomId roomId = room.Id;
                _areaHandlers.Add(room.Area, inside => HandleRoomPresence(roomId, inside));
            }
        }

        private void OnEnable()
        {
            foreach (KeyValuePair<FactoryAreaEntryController, Action<bool>> entry in _areaHandlers)
                entry.Key.PresenceChanged += entry.Value;
            _convolutionArea.PresenceChanged += HandleConvolutionPresence;
            _map.MapChanged += PublishMap;
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<FactoryAreaEntryController, Action<bool>> entry in _areaHandlers)
                entry.Key.PresenceChanged -= entry.Value;
            _convolutionArea.PresenceChanged -= HandleConvolutionPresence;
            _map.MapChanged -= PublishMap;
        }

        public void Initialize(XROrigin rig)
        {
            _player.PlaceAtEntrance(rig);
            _player.PrepareRayInput();
            _navigationData.ConvolutionEntranceDoor.Bind(_player.CameraTransform);
            _convolutionArea.Bind(_player.CameraTransform);
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                room.Area.Bind(_player.CameraTransform);
                room.EntryDoor.Bind(_player.CameraTransform);
                if (room.Id != RoomId.Exit) { room.ExitDoor.Bind(_player.CameraTransform); }
            }
            ActivateEnvironment();
            _map.Initialize();
        }

        private void HandleRoomPresence(RoomId room, bool inside)
        {
            if (room == RoomId.Exit) { ExitAreaChanged?.Invoke(inside); return; }
            foreach (LearningStageDefinition stage in LearningStageCatalog.Stages)
            {
                if (stage.Room != room) { continue; }
                StageAreaChanged?.Invoke(stage.Id, inside);
                return;
            }
        }

        private void HandleConvolutionPresence(bool inside) =>
            StageAreaChanged?.Invoke(LearningStageId.Convolution, inside);
        private void PublishMap(TabletMapData map) => MapChanged?.Invoke(map);
        private static Pose ToPose(Transform anchor) => new Pose(anchor.position, anchor.rotation);

        public bool IsStageAreaInside(LearningStageId stage) =>
            stage == LearningStageId.Convolution ? _convolutionArea.IsPlayerInside
                : _navigationData.Get(LearningStageCatalog.Get(stage).Room).Area.IsPlayerInside;
        public bool IsExitAreaInside => _navigationData.Get(RoomId.Exit).Area.IsPlayerInside;

        public LearningPlacement GetPlacement(LearningStageId stage)
        {
            if (stage == LearningStageId.Tutorial)
                return new LearningPlacement(new Pose(Vector3.zero, Quaternion.identity), Vector3.one, false);
            Transform anchor = _navigationData.Get(LearningStageCatalog.Get(stage).Room).ContentAnchor;
            return new LearningPlacement(ToPose(anchor), anchor.lossyScale, true);
        }

        public void ActivateEnvironment() => SceneManager.SetActiveScene(gameObject.scene);
        public void ReleaseSceneObjects(Scene scene) => _player.ReleaseSceneObjects(scene);

        public void SetEntranceAvailable(LearningStageId stage, bool available)
        {
            FactoryShutterController door = stage == LearningStageId.Convolution
                ? _navigationData.ConvolutionEntranceDoor
                : _navigationData.Get(LearningStageCatalog.Get(stage).Room).EntryDoor;
            if (available) { door.Open(); } else { door.Close(); }
            if (stage == LearningStageId.Convolution)
                _routeGuide.SetConvolutionRouteVisible(available);
        }

        public void ShowDestination(LearningStageId stage) =>
            _routeGuide.SetDestination(LearningStageCatalog.Get(stage).Room);

        public void SetNextStagePrepared(LearningStageId current, LearningStageId next)
        {
            if (next == LearningStageId.Convolution) { return; }
            _navigationData.Get(LearningStageCatalog.Get(current).Room).ExitDoor.Open();
            ShowDestination(next);
        }

        public void BeginEntry(LearningStageId stage)
        {
            SetEntranceAvailable(stage, false);
            _routeGuide.SetDestination(RoomId.None);
        }

        public void StageActivated(LearningStageId stage)
        {
            if (stage == LearningStageId.Convolution)
            {
                _navigationData.ConvolutionEntranceDoor.Close();
                _routeGuide.SetConvolutionRouteVisible(false);
            }
        }

        public void AllowExit(LearningStageId current)
        {
            _navigationData.Get(LearningStageCatalog.Get(current).Room).ExitDoor.Open();
            _navigationData.Get(RoomId.Exit).EntryDoor.Open();
            _routeGuide.SetDestination(RoomId.Exit);
        }

        public void BeginExit()
        {
            _navigationData.Get(RoomId.Exit).EntryDoor.Close();
            _routeGuide.SetDestination(RoomId.None);
        }

        public void ResetForStageMove()
        {
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                room.EntryDoor.Close();
                if (room.Id != RoomId.Exit) { room.ExitDoor.Close(); }
            }
            _navigationData.ConvolutionEntranceDoor.Close();
            _routeGuide.SetDestination(RoomId.None);
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
