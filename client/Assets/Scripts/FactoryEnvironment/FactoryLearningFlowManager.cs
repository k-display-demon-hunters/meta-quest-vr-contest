using System.Collections;
using CNNFeatureMaps;
using ModelDesignScene;
using UI;
using UnityEngine;

namespace FactoryEnvironment
{
    // Owns this factory session; calculations and interaction stay in the lesson scenes.
    public sealed class FactoryLearningFlowManager : MonoBehaviour
    {
        private FactoryRouteGuideController _routeGuide;
        private FactoryPlayerController _player;
        private FactorySceneLoadController _sceneLoader;
        private FactoryNavigationData _navigationData;
        private CNNPixelLabManager _pixels;
        private CNNFeatureMapGuide _maps;
        private CNNModelDesignLabManager _model;
        private FactoryDestinationId _stage = FactoryDestinationId.None;
        private bool _loading;
        private float _nextCheckTime;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private FactoryDestinationId _skippedStage = FactoryDestinationId.None;
#endif

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
            foreach (FactoryNavigationData.DestinationData destination in _navigationData.Destinations)
            {
                destination.Area.Bind(_player.CameraTransform);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FactoryDebugController debugController = gameObject.AddComponent<FactoryDebugController>();
            debugController.Initialize(this);
#endif
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
            if (destination == _stage || !area.IsPlayerInside)
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
            bool skipped = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            skipped = _skippedStage == _stage;
#endif
            switch (_stage)
            {
                case FactoryDestinationId.None:
                    return FactoryDestinationId.PixelLab;
                case FactoryDestinationId.PixelLab:
                    return skipped || _pixels.ConvolutionCompleted
                        ? FactoryDestinationId.FeatureMapLab : FactoryDestinationId.PixelLab;
                case FactoryDestinationId.FeatureMapLab:
                    return skipped || _maps.ReadyForModelDesign
                        ? FactoryDestinationId.ModelDesignLab : FactoryDestinationId.FeatureMapLab;
                case FactoryDestinationId.ModelDesignLab:
                    return skipped || _model.ReadyForFactoryExit
                        ? FactoryDestinationId.Exit : FactoryDestinationId.ModelDesignLab;
                default:
                    return FactoryDestinationId.Exit;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TrySkipCurrentLesson()
        {
            if (!isActiveAndEnabled || _loading || _sceneLoader.IsLeaving
                || _stage == FactoryDestinationId.None || _stage == FactoryDestinationId.Exit)
            {
                return false;
            }
            _skippedStage = _stage;
            _nextCheckTime = 0f;
            return true;
        }
#endif

        private IEnumerator LoadLesson(FactoryDestinationId destination)
        {
            yield return null;
            if (_stage == FactoryDestinationId.None)
            {
                yield return _sceneLoader.EndTutorial();
            }
            yield return _sceneLoader.PrepareLesson(destination);
            GameObject content = _sceneLoader.PreparedContent;
            if (content == null)
            {
                Fail(_sceneLoader.LastError);
                yield break;
            }
            if (GetDestination() != destination)
            {
                yield return _sceneLoader.CancelPreparedLesson();
                _loading = false;
                yield break;
            }
            if (_pixels != null)
            {
                _pixels.CloseDetails();
            }
            yield return _sceneLoader.CommitPreparedLesson();
            _pixels = content.GetComponentInChildren<CNNPixelLabManager>(true);
            _maps = content.GetComponentInChildren<CNNFeatureMapGuide>(true);
            _model = content.GetComponentInChildren<CNNModelDesignLabManager>(true);
            _stage = destination;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _skippedStage = FactoryDestinationId.None;
#endif
            _loading = false;
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
