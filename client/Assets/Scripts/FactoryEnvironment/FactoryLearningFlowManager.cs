using System.Collections;
using CNNFeatureMaps;
using ModelDesignScene;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns the room sequence. Each lesson owns its existing completion criteria.
    public sealed class FactoryLearningFlowManager : MonoBehaviour
    {
        private FactoryRouteGuideController _routeGuide;
        private FactoryPlayerController _player;
        private FactorySceneLoadController _sceneLoader;
        private FactoryNavigationData _navigationData;
        private FactoryDestinationId _stage = FactoryDestinationId.None;
        private FactoryDestinationId _destination = FactoryDestinationId.Tutorial;
        private bool _loading;
        private float _nextCheckTime;

        public bool HasReachedExit => _stage == FactoryDestinationId.Exit;

        private void Awake()
        {
            _player = GetComponent<FactoryPlayerController>();
            _sceneLoader = GetComponent<FactorySceneLoadController>();
            _navigationData = GetComponent<FactoryNavigationData>();
            _routeGuide = GetComponentInChildren<FactoryRouteGuideController>(true);
        }

        private void Start()
        {
            _player.PlaceAtEntrance();
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                room.Area.Bind(_player.CameraTransform);
                room.EntryDoor.Bind(_player.CameraTransform);
                if (room.Id != FactoryDestinationId.Exit) { room.ExitDoor.Bind(_player.CameraTransform); }
            }
            _sceneLoader.ActivateFactoryScene();
            _player.PrepareRayInput();
            _navigationData.Get(_destination).EntryDoor.Open();
            _routeGuide.SetDestination(_destination);
        }

        private void Update()
        {
            if (_loading || HasReachedExit || Time.time < _nextCheckTime) { return; }
            _nextCheckTime = Time.time + .1f;
            if (_destination != _stage)
            {
                if (_navigationData.Get(_destination).Area.IsPlayerInside)
                {
                    _loading = true;
                    StartCoroutine(EnterRoom());
                }
            }
            else if (IsRoomComplete())
            {
                _loading = true;
                StartCoroutine(PrepareNextRoom());
            }
        }

        private bool IsRoomComplete()
        {
            switch (_stage)
            {
                case FactoryDestinationId.Tutorial: return _sceneLoader.TutorialCompleted;
                case FactoryDestinationId.PixelLab: return _navigationData.Get(_stage).Pixels.ConvolutionCompleted;
                case FactoryDestinationId.FeatureMapLab: return _navigationData.Get(_stage).Maps.ReadyForModelDesign;
                case FactoryDestinationId.ModelDesignLab: return _navigationData.Get(_stage).Model.ReadyForFactoryExit;
                default: return false;
            }
        }

        private static FactoryDestinationId NextRoom(FactoryDestinationId current)
        {
            switch (current)
            {
                case FactoryDestinationId.Tutorial: return FactoryDestinationId.PixelLab;
                case FactoryDestinationId.PixelLab: return FactoryDestinationId.FeatureMapLab;
                case FactoryDestinationId.FeatureMapLab: return FactoryDestinationId.ModelDesignLab;
                default: return FactoryDestinationId.Exit;
            }
        }

        private IEnumerator PrepareNextRoom()
        {
            FactoryDestinationId next = NextRoom(_stage);
            if (next != FactoryDestinationId.Exit)
            {
                yield return _sceneLoader.PrepareLesson(next);
                if (_sceneLoader.LastError != null)
                {
                    Fail(_sceneLoader.LastError);
                    yield break;
                }
                FactoryNavigationData.DestinationData data = _navigationData.Get(next);
                data.Content = FindContent(_sceneLoader.PreparedScene, data);
                _sceneLoader.PlacePreparedContent();
                if (!IsRoomComplete())
                {
                    yield return _sceneLoader.CancelPreparedLesson();
                    _loading = false;
                    yield break;
                }
            }
            // Unlock once per visit; resetting a finished lesson cannot close an exit on the user.
            _navigationData.Get(_stage).ExitDoor.Open();
            _navigationData.Get(next).EntryDoor.Open();
            _destination = next;
            _routeGuide.SetDestination(next);
            _loading = false;
        }

        private IEnumerator EnterRoom()
        {
            FactoryNavigationData.DestinationData room = _navigationData.Get(_destination);
            room.EntryDoor.Close();
            _routeGuide.SetDestination(FactoryDestinationId.None);
            if (_stage == FactoryDestinationId.PixelLab)
            {
                yield return _navigationData.Get(_stage).Pixels.EndSession();
            }
            if (_destination == FactoryDestinationId.Tutorial)
            {
                yield return _sceneLoader.RunTutorial(_player.Rig);
                if (!_sceneLoader.TutorialCompleted)
                {
                    room.EntryDoor.Open();
                    Fail(_sceneLoader.LastError);
                    yield break;
                }
            }
            else if (_destination == FactoryDestinationId.Exit)
            {
                yield return _sceneLoader.LoadEnding();
                if (!_sceneLoader.IsLeaving)
                {
                    room.EntryDoor.Open();
                    _routeGuide.SetDestination(_destination);
                    Fail(_sceneLoader.LastError);
                    yield break;
                }
            }
            else
            {
                yield return _sceneLoader.CommitPreparedLesson();
            }
            _stage = _destination;
            _loading = false;
        }

        private static GameObject FindContent(Scene scene, FactoryNavigationData.DestinationData data)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                switch (data.Id)
                {
                    case FactoryDestinationId.PixelLab:
                        data.Pixels = root.GetComponentInChildren<CNNPixelLabManager>(true);
                        if (data.Pixels != null) { return root; }
                        break;
                    case FactoryDestinationId.FeatureMapLab:
                        data.Maps = root.GetComponentInChildren<CNNFeatureMapLabManager>(true);
                        if (data.Maps != null) { return root; }
                        break;
                    case FactoryDestinationId.ModelDesignLab:
                        data.Model = root.GetComponentInChildren<CNNModelDesignLabManager>(true);
                        if (data.Model != null) { return root; }
                        break;
                }
            }
            return null;
        }

        private void Fail(string message)
        {
            _loading = false;
            _nextCheckTime = Time.time + 3f;
            Debug.LogError("[FactoryFlow] " + message, this);
        }
    }
}
