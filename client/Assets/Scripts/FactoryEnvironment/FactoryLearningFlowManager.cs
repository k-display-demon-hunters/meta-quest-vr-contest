using System.Collections;
using CNNFeatureMaps;
using ModelDesignScene;
using Tablet;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FactoryEnvironment
{
    // Owns this factory session; calculations and interaction stay in the lesson scenes.
    public sealed class FactoryLearningFlowManager : MonoBehaviour
    {
        [SerializeField] private Transform[] _contentAnchors;
        [SerializeField] private Font _font;
        [SerializeField] private Transform _entrance;
        [SerializeField] private FactoryAreaEntryController[] _areas;
        [SerializeField] private FactoryRouteGuideController _routeGuide;
        [SerializeField] private string _endingSceneName = "FactoryEnding";

        private readonly string[] _scenes = { "CNNPixelLab", "CNNFeatureMapLab", "CNNModelDesignLab" };
        private Scene _lesson;
        private CNNPixelLabManager _pixels;
        private CNNFeatureMapGuide _maps;
        private CNNModelDesignLabManager _model;
        private XRBaseInteractor[] _interactors;
        private FactoryAreaEntryController _failedArea;
        private int _stage = -1;
        private bool _loading;
        private bool _leaving;
        private float _nextCheckTime;
        private TabletManager _tablet;
        private TabletMapData _mapData;

        private void OnEnable()
        {
            if (_mapData != null)
            {
                PublishMap();
            }
        }

        private void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            if (rig == null || rig.Camera == null || !HasValidConfiguration())
            {
                Debug.LogError("[FactoryFlow] XR 리그와 공장 입구·진입 영역·체험 기준점 연결을 확인해 주세요.", this);
                enabled = false;
                return;
            }
            PlaceAtEntrance(rig);
            _interactors = rig.GetComponentsInChildren<XRBaseInteractor>(true);
            foreach (FactoryAreaEntryController area in _areas)
            {
                area.Bind(rig.Camera.transform);
            }
            _routeGuide.Bind(rig.Camera.transform);
            PublishMap();
            FactoryFlowPortalController portal = new GameObject("Factory route instruction")
                .AddComponent<FactoryFlowPortalController>();
            portal.transform.SetParent(transform, false);
            portal.transform.SetPositionAndRotation(_entrance.position + _entrance.forward * 3f + Vector3.up * 1.4f,
                _entrance.rotation);
            portal.Initialize(_font);
            SceneManager.SetActiveScene(gameObject.scene);
            _routeGuide.SetDestination(0);
        }

        private void Update()
        {
            if (_loading || _leaving || Time.time < _nextCheckTime)
            {
                return;
            }
            _nextCheckTime = Time.time + 0.1f;
            if (_failedArea != null && !_failedArea.IsPlayerInside)
            {
                _failedArea = null;
            }
            int destination = GetDestination();
            FactoryAreaEntryController area = _areas[destination];
            _routeGuide.SetDestination(area.IsPlayerInside ? -1 : destination);
            if (destination == _stage || !area.IsPlayerInside || area == _failedArea || HasSelection())
            {
                return;
            }
            // Check occupancy, not just entry events: releasing a held object inside the room must also work.
            _loading = true;
            _routeGuide.SetDestination(-1);
            if (destination == _scenes.Length)
            {
                StartCoroutine(EnterEnding());
            }
            else
            {
                StartCoroutine(LoadLesson(destination));
            }
        }

        private bool HasValidConfiguration()
        {
            if (_entrance == null || _font == null || _routeGuide == null
                || _areas == null || _areas.Length != _scenes.Length + 1
                || _contentAnchors == null || _contentAnchors.Length != _scenes.Length)
            {
                return false;
            }
            foreach (FactoryAreaEntryController area in _areas)
            {
                if (area == null)
                {
                    return false;
                }
            }
            foreach (Transform anchor in _contentAnchors)
            {
                if (anchor == null)
                {
                    return false;
                }
            }
            return true;
        }

        private int GetDestination()
        {
            if (_stage < 0)
            {
                return 0;
            }
            bool completed = _stage == 0 ? _pixels != null && _pixels.ConvolutionCompleted
                : _stage == 1 ? _maps != null && _maps.ReadyForModelDesign
                : _model != null && _model.ReadyForFactoryExit;
            return completed ? _stage + 1 : _stage;
        }

        private bool HasSelection()
        {
            foreach (XRBaseInteractor interactor in _interactors)
            {
                if (interactor != null && interactor.hasSelection)
                {
                    return true;
                }
            }
            return false;
        }

        private IEnumerator LoadLesson(int index)
        {
            yield return null;
            string next = _scenes[index];
            if (!Application.CanStreamedLevelBeLoaded(next) || SceneManager.GetSceneByName(next).isLoaded)
            {
                Fail(index, "체험 씬의 빌드 등록과 중복 로드 여부를 확인해 주세요.");
                yield break;
            }
            AsyncOperation load = SceneManager.LoadSceneAsync(next, LoadSceneMode.Additive);
            if (load == null)
            {
                Fail(index, "체험을 열지 못했습니다. 방을 나갔다 다시 들어와 주세요.");
                yield break;
            }
            yield return load;
            Scene incoming = SceneManager.GetSceneByName(next);
            GameObject content = FindContent(incoming);
            if (content == null)
            {
                yield return SceneManager.UnloadSceneAsync(incoming);
                Fail(index, "체험 루트를 찾지 못했습니다. 이전 체험은 유지됩니다.");
                yield break;
            }
            // Keep the existing factory floor, lighting and persistent rig.
            foreach (GameObject root in incoming.GetRootGameObjects())
            {
                if (root != content)
                {
                    root.SetActive(false);
                }
            }
            foreach (Light light in content.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }
            Vector3 offset = _contentAnchors[index].position - Vector3.forward * 1.5f;
            content.transform.position += offset;
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
            CNNPixelLabManager nextPixels = content.GetComponentInChildren<CNNPixelLabManager>(true);
            if (nextPixels != null)
            {
                nextPixels.RebaseDock(offset);
            }
            // Loading can finish while an old or newly loaded object is held. Never unload its scene mid-grab.
            while (HasSelection())
            {
                yield return null;
            }
            if (GetDestination() != index)
            {
                yield return SceneManager.UnloadSceneAsync(incoming);
                _loading = false;
                yield break;
            }
            if (_pixels != null)
            {
                _pixels.CloseDetails();
            }
            if (_lesson.IsValid() && _lesson.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_lesson);
            }
            _lesson = incoming;
            _pixels = nextPixels;
            _maps = content.GetComponentInChildren<CNNFeatureMapGuide>(true);
            _model = content.GetComponentInChildren<CNNModelDesignLabManager>(true);
            _stage = index;
            SceneManager.SetActiveScene(gameObject.scene);
            _loading = false;
        }

        private IEnumerator EnterEnding()
        {
            yield return null;
            if (GetDestination() != _scenes.Length || HasSelection())
            {
                _loading = false;
                yield break;
            }
            if (!Application.CanStreamedLevelBeLoaded(_endingSceneName))
            {
                Fail(_scenes.Length, "엔딩 씬의 빌드 등록을 확인해 주세요.");
                yield break;
            }
            // Single loading owns cleanup; OnDestroy must not request a second lesson unload.
            _leaving = true;
            AsyncOperation load = SceneManager.LoadSceneAsync(_endingSceneName, LoadSceneMode.Single);
            if (load == null)
            {
                _leaving = false;
                Fail(_scenes.Length, "엔딩을 열지 못했습니다. 출구를 나갔다 다시 들어와 주세요.");
                yield break;
            }
            yield return load;
        }

        private static GameObject FindContent(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<CNNPixelLabManager>(true) != null
                    || root.GetComponentInChildren<CNNFeatureMapLabManager>(true) != null
                    || root.GetComponentInChildren<CNNModelDesignLabManager>(true) != null)
                {
                    return root;
                }
            }
            return null;
        }

        private void PlaceAtEntrance(XROrigin rig)
        {
            float height = rig.Camera.transform.position.y - rig.Origin.transform.position.y;
#if UNITY_EDITOR
            // Keep the editor simulator at eye level without changing real headset tracking.
            if (!UnityEngine.XR.XRSettings.isDeviceActive
                && FindFirstObjectByType<XR.XREditorSimulator>() != null && height < 0.5f)
            {
                Vector3 offset = rig.CameraFloorOffsetObject.transform.localPosition;
                offset.y += rig.CameraYOffset - height;
                rig.CameraFloorOffsetObject.transform.localPosition = offset;
                height = rig.CameraYOffset;
            }
#endif
            rig.MatchOriginUpCameraForward(Vector3.up, _entrance.forward);
            rig.MoveCameraToWorldLocation(_entrance.position + Vector3.up * height);
        }

        private void Fail(int destination, string message)
        {
            // Retry on re-entry instead of repeatedly issuing a failed load every frame.
            _failedArea = _areas[destination];
            _loading = false;
            Debug.LogError("[FactoryFlow] " + message, this);
        }

        private void PublishMap()
        {
            _mapData = FactoryMapReader.Read(gameObject);
            _tablet = FindFirstObjectByType<TabletManager>();
            if (_tablet != null)
            {
                _tablet.SetMapData(_mapData);
            }
        }

        private void OnDisable()
        {
            if (_tablet != null)
            {
                _tablet.ClearMapData(_mapData);
            }
            if (_routeGuide != null)
            {
                _routeGuide.SetDestination(-1);
            }
        }

        private void OnDestroy()
        {
            if (!_leaving && _lesson.IsValid() && _lesson.isLoaded && Application.isPlaying)
            {
                SceneManager.UnloadSceneAsync(_lesson);
            }
        }
    }
}
