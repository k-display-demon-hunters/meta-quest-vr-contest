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
        public Pose PixelLabContentPose => ToPose(_convolutionAnchor);
        public bool IsPixelPassageInside => _convolutionArea.IsPlayerInside;

        private void Awake()
        {
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                LearningStageId roomId = room.Id;
                _areaHandlers.Add(room.Area, inside => HandleRoomPresence(roomId, inside));
            }
        }

        private void OnEnable()
        {
            foreach (KeyValuePair<FactoryAreaEntryController, Action<bool>> entry in _areaHandlers)
                entry.Key.PresenceChanged += entry.Value;
            _map.MapChanged += PublishMap;
        }

        private void OnDisable()
        {
            foreach (KeyValuePair<FactoryAreaEntryController, Action<bool>> entry in _areaHandlers)
                entry.Key.PresenceChanged -= entry.Value;
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
                if (room.Id != LearningStageId.Exit) { room.ExitDoor.Bind(_player.CameraTransform); }
            }
            ActivateEnvironment();
            _map.Initialize();
        }

        private void HandleRoomPresence(LearningStageId room, bool inside)
        {
            if (room == LearningStageId.Exit) { ExitAreaChanged?.Invoke(inside); return; }
            StageAreaChanged?.Invoke(room, inside);
        }

        private void PublishMap(TabletMapData map) => MapChanged?.Invoke(map);
        private static Pose ToPose(Transform anchor) => new Pose(anchor.position, anchor.rotation);

        public bool IsStageAreaInside(LearningStageId stage) =>
            _navigationData.Get(stage).Area.IsPlayerInside;
        public bool IsExitAreaInside => _navigationData.Get(LearningStageId.Exit).Area.IsPlayerInside;

        public void ActivateEnvironment() => SceneManager.SetActiveScene(gameObject.scene);
        public void ReleaseSceneObjects(Scene scene) => _player.ReleaseSceneObjects(scene);

        public void SetEntranceAvailable(LearningStageId stage, bool available)
        {
            FactoryShutterController door = _navigationData.Get(stage).EntryDoor;
            if (available) { door.Open(); } else { door.Close(); }
        }

        public void ShowDestination(LearningStageId stage) =>
            _routeGuide.SetDestination(stage);

        public void SetNextStagePrepared(LearningStageId current, LearningStageId next)
        {
            _navigationData.Get(current).ExitDoor.Open();
            ShowDestination(next);
        }

        public void BeginEntry(LearningStageId stage)
        {
            SetEntranceAvailable(stage, false);
            _routeGuide.SetDestination(LearningStageId.None);
        }

        public void SetPixelPassageAvailable(bool available)
        {
            if (available) { _navigationData.ConvolutionEntranceDoor.Open(); }
            else { _navigationData.ConvolutionEntranceDoor.Close(); }
            _routeGuide.SetConvolutionRouteVisible(available);
        }

        public void AllowExit(LearningStageId current)
        {
            _navigationData.Get(current).ExitDoor.Open();
            _navigationData.Get(LearningStageId.Exit).EntryDoor.Open();
            _routeGuide.SetDestination(LearningStageId.Exit);
        }

        public void BeginExit()
        {
            _navigationData.Get(LearningStageId.Exit).EntryDoor.Close();
            _routeGuide.SetDestination(LearningStageId.None);
        }

        public void ResetForStageMove()
        {
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                room.EntryDoor.Close();
                if (room.Id != LearningStageId.Exit) { room.ExitDoor.Close(); }
            }
            _navigationData.ConvolutionEntranceDoor.Close();
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
