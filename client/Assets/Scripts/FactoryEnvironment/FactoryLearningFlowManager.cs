using System.Collections;
using Tablet;
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
        [SerializeField] private FactoryShutterController _room1Passage;
        [SerializeField] private FactoryAreaEntryController _room1SecondArea;
        [SerializeField] private Transform _room1SecondAnchor;
        [SerializeField] private Transform[] _stageArrivals;
        private CNNPixelLabManager _room1Pixels;
        private TabletManager _tablet;
        private int _completedStages;
        private int _currentLesson = -1;
        private int _publishedMask = -1;
        private int _publishedStage = -2;
        private bool _publishedTravel;


        private FactoryRouteGuideController _routeGuide;
        private FactoryPlayerController _player;
        private FactorySceneLoadController _sceneLoader;
        private FactoryNavigationData _navigationData;
        private FactoryDestinationId _stage = FactoryDestinationId.None;
        private FactoryDestinationId _destination = FactoryDestinationId.Tutorial;
        private bool _loading;
        private float _nextCheckTime;

        public bool HasReachedExit => _stage == FactoryDestinationId.Exit;

        public void NotifyRoom1FirstExperienceCompleted()
        {
            _completedStages |= 1;
            PublishTablet();
            _room1Passage.Open();
            _routeGuide.SetRoom1PassageVisible(true);
        }

        public void NotifyRoom1SecondExperienceStarted()
        {
            _currentLesson = 1;
            PublishTablet();
            _room1Passage.Close();
            _routeGuide.SetRoom1PassageVisible(false);
        }

        private void OnEnable()
        {
            SubscribeRoom1();
            _tablet.StageMoveRequested += RequestStageMove;
        }
        private void OnDisable()
        {
            UnsubscribeRoom1();
            if (_tablet != null)
            {
                _tablet.StageMoveRequested -= RequestStageMove;
                _tablet.SetLearningState(_currentLesson, _completedStages, false);
            }
        }

        private void SubscribeRoom1()
        {
            // This module is absent until ROOM #1 has been prepared.
            if (_room1Pixels == null) { return; }
            _room1Pixels.ScreenCompleted += NotifyRoom1FirstExperienceCompleted;
            _room1Pixels.ConvolutionStarted += NotifyRoom1SecondExperienceStarted;
        }

        private void UnsubscribeRoom1()
        {
            if (_room1Pixels == null) { return; }
            _room1Pixels.ScreenCompleted -= NotifyRoom1FirstExperienceCompleted;
            _room1Pixels.ConvolutionStarted -= NotifyRoom1SecondExperienceStarted;
        }

        private void Awake()
        {
            _tablet = FindFirstObjectByType<TabletManager>();
            _player = GetComponent<FactoryPlayerController>();
            _sceneLoader = GetComponent<FactorySceneLoadController>();
            _navigationData = GetComponent<FactoryNavigationData>();
            _routeGuide = GetComponentInChildren<FactoryRouteGuideController>(true);
        }

        private void Start()
        {
            _player.PlaceAtEntrance();
            PublishTablet();
            _room1Passage.Bind(_player.CameraTransform);
            _room1SecondArea.Bind(_player.CameraTransform);
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
            PublishTablet();
            if (_loading || HasReachedExit || _destination == FactoryDestinationId.None || Time.time < _nextCheckTime) { return; }
            _nextCheckTime = Time.time + .1f;
            if (_stage == FactoryDestinationId.PixelLab && _room1Pixels.WaitingForFactoryConvolution
                && _room1SecondArea.IsPlayerInside)
            {
                _loading = true;
                StartCoroutine(StartRoom1Convolution());
                return;
            }
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
                if (_currentLesson >= 0) { _completedStages |= 1 << _currentLesson; }
                PublishTablet();
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

        private IEnumerator StartRoom1Convolution()
        {
            yield return _room1Pixels.StartFactoryConvolution();
            _loading = false;
            _nextCheckTime = Time.time + (_room1Pixels.WaitingForFactoryConvolution ? 3f : .1f);
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
                if (next == FactoryDestinationId.PixelLab)
                {
                    data.Pixels.ConfigureFactoryConvolution(
                        new Pose(_room1SecondAnchor.position, _room1SecondAnchor.rotation));
                }
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
                UnsubscribeRoom1();
                _room1Pixels = null;
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
                if (_destination == FactoryDestinationId.PixelLab)
                {
                    _room1Pixels = room.Pixels;
                    SubscribeRoom1();
                }
                yield return _sceneLoader.CommitPreparedLesson();
            }
            _stage = _destination;
            _currentLesson = _stage == FactoryDestinationId.PixelLab ? 0
                : _stage == FactoryDestinationId.FeatureMapLab ? 2
                : _stage == FactoryDestinationId.ModelDesignLab ? 3 : -1;
            _loading = false;
            PublishTablet();
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

        private void PublishTablet()
        {
            bool canTravel = !_loading && !HasReachedExit && isActiveAndEnabled;
            if (_publishedMask == _completedStages && _publishedStage == _currentLesson && _publishedTravel == canTravel) { return; }
            _publishedMask = _completedStages;
            _publishedStage = _currentLesson;
            _publishedTravel = canTravel;
            _tablet.SetLearningState(_currentLesson, _completedStages, canTravel);
        }

        private void RequestStageMove(int index)
        {
            if (_loading || HasReachedExit || index < 0 || index >= _stageArrivals.Length) { return; }
            if (_currentLesson >= 0 && IsRoomComplete()) { _completedStages |= 1 << _currentLesson; }
            _loading = true;
            PublishTablet();
            _tablet.ShowTravelStatus("선택한 단계로 이동 중…");
            StartCoroutine(MoveToLesson(index));
        }

        private IEnumerator MoveToLesson(int index)
        {
            // Leave the button callback before unloading any lesson objects.
            yield return null;
            if (_sceneLoader.HasPreparedLesson) { yield return _sceneLoader.CancelPreparedLesson(); }
            if (_room1Pixels != null)
            {
                UnsubscribeRoom1();
                yield return _room1Pixels.EndSession();
                _room1Pixels = null;
            }
            yield return _sceneLoader.UnloadCurrentLesson();
            _stage = FactoryDestinationId.None;
            _destination = FactoryDestinationId.None;
            _currentLesson = -1;
            FactoryDestinationId destination = index < 2 ? FactoryDestinationId.PixelLab
                : index == 2 ? FactoryDestinationId.FeatureMapLab : FactoryDestinationId.ModelDesignLab;
            yield return _sceneLoader.PrepareLesson(destination);
            if (_sceneLoader.LastError != null)
            {
                _tablet.ShowTravelStatus("이동하지 못했습니다. 단계를 다시 선택하세요.");
                Fail(_sceneLoader.LastError);
                PublishTablet();
                yield break;
            }
            FactoryNavigationData.DestinationData data = _navigationData.Get(destination);
            data.Content = FindContent(_sceneLoader.PreparedScene, data);
            _sceneLoader.PlacePreparedContent();
            if (destination == FactoryDestinationId.PixelLab)
            {
                data.Pixels.ConfigureFactoryConvolution(new Pose(_room1SecondAnchor.position, _room1SecondAnchor.rotation), index == 1);
                _room1Pixels = data.Pixels;
                SubscribeRoom1();
            }
            CloseRoomDoors();
            _room1Passage.Close();
            _routeGuide.SetRoom1PassageVisible(false);
            _routeGuide.SetDestination(FactoryDestinationId.None);
            yield return _sceneLoader.CommitPreparedLesson();
            _stage = destination;
            _destination = destination;
            _currentLesson = index;
            _player.MoveTo(_stageArrivals[index]);
            // PixelLab starts its nested screen/convolution loader in Start.
            yield return null;
            if (_room1Pixels != null)
            {
                while (_room1Pixels.IsSwitching) { yield return null; }
                if (_room1Pixels.LoadError != null)
                {
                    _tablet.ShowTravelStatus("체험을 열지 못했습니다. 단계를 다시 선택하세요.");
                    Fail(_room1Pixels.LoadError);
                    PublishTablet();
                    yield break;
                }
            }
            _loading = false;
            _tablet.ShowTravelStatus("");
            PublishTablet();
        }

        private void CloseRoomDoors()
        {
            foreach (FactoryNavigationData.DestinationData room in _navigationData.Destinations)
            {
                room.EntryDoor.Close();
                if (room.Id != FactoryDestinationId.Exit) { room.ExitDoor.Close(); }
            }
        }

        private void Fail(string message)
        {
            _loading = false;
            _nextCheckTime = Time.time + 3f;
            Debug.LogError("[FactoryFlow] " + message, this);
        }
    }
}
