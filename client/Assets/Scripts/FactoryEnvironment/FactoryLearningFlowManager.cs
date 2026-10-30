using System.Collections;
using Parchment;
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
        private ParchmentManager _parchment;
        private bool _refreshGates;
        [SerializeField] private FactoryAreaEntryController _convolutionArea;
        [SerializeField] private Transform _convolutionAnchor;
        private CNNPixelLabManager _pixelLab;

        private FactoryRouteGuideController _routeGuide;
        private FactoryPlayerController _player;
        private FactorySceneLoadController _sceneLoader;
        private FactoryNavigationData _navigationData;
        private FactoryDestinationId _stage = FactoryDestinationId.None;
        private FactoryDestinationId _destination = FactoryDestinationId.Tutorial;
        private bool _loading;
        private float _nextCheckTime;

        public bool HasReachedExit => _stage == FactoryDestinationId.Exit;

        public void NotifyScreenCompleted()
        {
            _refreshGates = true;
        }

        public void NotifyConvolutionStarted()
        {
            _navigationData.ConvolutionEntranceDoor.Close();
            _routeGuide.SetConvolutionRouteVisible(false);
        }

        private void OnEnable() { SubscribePixelLab(); _parchment.Collected += OnCollected; _refreshGates = true; }
        private void OnDisable() { UnsubscribePixelLab(); _parchment.Collected -= OnCollected; }
        private void OnCollected(int id) => _refreshGates = true;

        private void SubscribePixelLab()
        {
            // This module is absent until ROOM #1 has been prepared.
            if (_pixelLab == null) { return; }
            _pixelLab.ScreenCompleted += NotifyScreenCompleted;
            _pixelLab.ConvolutionStarted += NotifyConvolutionStarted;
        }

        private void UnsubscribePixelLab()
        {
            if (_pixelLab == null) { return; }
            _pixelLab.ScreenCompleted -= NotifyScreenCompleted;
            _pixelLab.ConvolutionStarted -= NotifyConvolutionStarted;
        }

        private void Awake()
        {
            _parchment = FindFirstObjectByType<ParchmentManager>();
            _player = GetComponent<FactoryPlayerController>();
            _sceneLoader = GetComponent<FactorySceneLoadController>();
            _navigationData = GetComponent<FactoryNavigationData>();
            _routeGuide = GetComponentInChildren<FactoryRouteGuideController>(true);
        }

        private void Start()
        {
            _player.PlaceAtEntrance();
            _navigationData.ConvolutionEntranceDoor.Bind(_player.CameraTransform);
            _convolutionArea.Bind(_player.CameraTransform);
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
            if (_refreshGates)
            {
                _refreshGates = false;
                if (_destination != _stage && _destination != FactoryDestinationId.Tutorial
                    && _destination != FactoryDestinationId.Exit
                    && _parchment.IsGroupCollected(EntranceLocation(_destination)))
                    _navigationData.Get(_destination).EntryDoor.Open();
                if (_stage == FactoryDestinationId.PixelLab && _pixelLab.WaitingForFactoryConvolution
                    && _parchment.IsGroupCollected(ParchmentLocation.ConvolutionLabEntrance))
                {
                    _navigationData.ConvolutionEntranceDoor.Open();
                    _routeGuide.SetConvolutionRouteVisible(true);
                }
            }
            if (_stage == FactoryDestinationId.PixelLab && _pixelLab.WaitingForFactoryConvolution
                && _parchment.IsGroupCollected(ParchmentLocation.ConvolutionLabEntrance)
                && _convolutionArea.IsPlayerInside)
            {
                _loading = true;
                StartCoroutine(StartConvolution());
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
                _loading = true;
                StartCoroutine(PrepareNextRoom());
            }
        }

        private static ParchmentLocation EntranceLocation(FactoryDestinationId id) => id switch
        {
            FactoryDestinationId.PixelLab => ParchmentLocation.PixelLabEntrance,
            FactoryDestinationId.FeatureMapLab => ParchmentLocation.FeatureMapLabEntrance,
            FactoryDestinationId.ModelDesignLab => ParchmentLocation.ModelDesignLabEntrance,
            _ => ParchmentLocation.None
        };

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

        private IEnumerator StartConvolution()
        {
            yield return _pixelLab.StartFactoryConvolution();
            _loading = false;
            _nextCheckTime = Time.time + (_pixelLab.WaitingForFactoryConvolution ? 3f : .1f);
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
                        new Pose(_convolutionAnchor.position, _convolutionAnchor.rotation));
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
            if (next == FactoryDestinationId.Exit) _navigationData.Get(next).EntryDoor.Open();
            _refreshGates = true;
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
                UnsubscribePixelLab();
                _pixelLab = null;
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
                    _pixelLab = room.Pixels;
                    SubscribePixelLab();
                }
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
