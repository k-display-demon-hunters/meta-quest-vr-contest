using System.Collections;
using CNNFeatureMaps;
using ModelDesignScene;
using Tablet;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns this factory session; calculations and interaction stay in the lesson scenes.
    public sealed class FactoryLearningFlowManager : MonoBehaviour
    {
        private FactoryRouteGuideController _routeGuide;
        private FactoryPlayerController _player;
        private FactorySceneLoadController _sceneLoader;
        private FactoryNavigationData _navigationData;
        private TabletManager _tablet;
        private FactoryDestinationId _stage = FactoryDestinationId.None;
        private bool _loading;
        private float _nextCheckTime;

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
            _tablet = _player.Rig.GetComponentInChildren<TabletManager>(true);
            if (_tablet == null)
            {
                Debug.LogError("[FactoryFlow] 양피지 진행 조건을 확인할 TabletManager가 없습니다.", this);
            }
            foreach (FactoryNavigationData.DestinationData destination in _navigationData.Destinations)
            {
                destination.Area.Bind(_player.CameraTransform);
            }
            _sceneLoader.ActivateFactoryScene();
            _player.PrepareRayInput();
            _routeGuide.SetDestination(FactoryDestinationId.PixelLab);
            StartCoroutine(_sceneLoader.RunTutorial(_player.Rig));
        }

        private void Update()
        {
            if (_loading || _sceneLoader.IsLeaving || Time.time < _nextCheckTime)
            {
                return;
            }
            _nextCheckTime = Time.time + 0.1f;
            FactoryDestinationId destination = GetDestination();
            FactoryAreaEntryController area = _navigationData.Get(destination).Area;
            _routeGuide.SetDestination(area.IsPlayerInside ? FactoryDestinationId.None : destination);
            if (destination == _stage || !area.IsPlayerInside || !HasRequiredParchments(destination))
            {
                return;
            }
            _loading = true;
            _routeGuide.SetDestination(FactoryDestinationId.None);
            if (destination == FactoryDestinationId.Exit)
            {
                StartCoroutine(EnterEnding());
            }
            else
            {
                StartCoroutine(LoadLesson(destination));
            }
        }

        private FactoryDestinationId GetDestination()
        {
            switch (_stage)
            {
                case FactoryDestinationId.None:
                    return FactoryDestinationId.PixelLab;
                case FactoryDestinationId.PixelLab:
                    return _navigationData.Get(_stage).Pixels.ConvolutionCompleted
                        ? FactoryDestinationId.FeatureMapLab : FactoryDestinationId.PixelLab;
                case FactoryDestinationId.FeatureMapLab:
                    return _navigationData.Get(_stage).Maps.ReadyForModelDesign
                        ? FactoryDestinationId.ModelDesignLab : FactoryDestinationId.FeatureMapLab;
                case FactoryDestinationId.ModelDesignLab:
                    return _navigationData.Get(_stage).Model.ReadyForFactoryExit
                        ? FactoryDestinationId.Exit : FactoryDestinationId.ModelDesignLab;
                default:
                    return FactoryDestinationId.Exit;
            }
        }

        private bool HasRequiredParchments(FactoryDestinationId destination)
        {
            if (destination == FactoryDestinationId.Exit)
            {
                return true;
            }
            if (_tablet == null)
            {
                return false;
            }
            switch (destination)
            {
                case FactoryDestinationId.PixelLab:
                    return _tablet.HasParchment(0) && _tablet.HasParchment(1);
                case FactoryDestinationId.FeatureMapLab:
                    return _tablet.HasParchment(2);
                case FactoryDestinationId.ModelDesignLab:
                    return _tablet.HasParchment(4);
                default:
                    return false;
            }
        }


        private IEnumerator LoadLesson(FactoryDestinationId destination)
        {
            yield return null;
            if (_stage == FactoryDestinationId.None)
            {
                yield return _sceneLoader.EndTutorial();
            }
            yield return _sceneLoader.PrepareLesson(destination);
            if (_sceneLoader.LastError != null)
            {
                Fail(_sceneLoader.LastError);
                yield break;
            }
            FactoryNavigationData.DestinationData data = _navigationData.Get(destination);
            data.Content = FindContent(_sceneLoader.PreparedScene, data);
            _sceneLoader.PlacePreparedContent();
            if (GetDestination() != destination)
            {
                yield return _sceneLoader.CancelPreparedLesson();
                _loading = false;
                yield break;
            }
            if (_stage == FactoryDestinationId.PixelLab)
            {
                yield return _navigationData.Get(_stage).Pixels.EndSession();
            }
            yield return _sceneLoader.CommitPreparedLesson();
            _stage = destination;
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

        private IEnumerator EnterEnding()
        {
            yield return null;
            if (GetDestination() != FactoryDestinationId.Exit)
            {
                _loading = false;
                yield break;
            }
            yield return _sceneLoader.LoadEnding();
            if (!_sceneLoader.IsLeaving)
            {
                Fail(_sceneLoader.LastError);
            }
        }

        private void Fail(string message)
        {
            _loading = false;
            Debug.LogError("[FactoryFlow] " + message, this);
        }
    }
}
