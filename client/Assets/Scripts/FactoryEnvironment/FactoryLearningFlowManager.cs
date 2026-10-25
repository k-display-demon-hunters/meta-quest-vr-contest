using System.Collections;
using CNNFeatureMaps;
using FactoryTutorial;
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
        private const string TutorialSceneName = "FactoryTutorial";

        [SerializeField] private Transform[] _contentAnchors;
        [SerializeField] private Font _font;
        [SerializeField] private Transform _entrance;
        [SerializeField] private FactoryAreaEntryController[] _areas;
        [SerializeField] private FactoryRouteGuideController _routeGuide;
        [SerializeField] private string _endingSceneName = "FactoryEnding";

        private readonly string[] _scenes = { "CNNPixelLab", "CNNFeatureMapLab", "CNNModelDesignLab" };
        private Scene _lesson;
        private Camera _camera;
        private GameObject _briefing;
        private XR.XRPlayerInputController _briefInput;
        private bool _previousBriefRay;
        private readonly bool[] _completed = new bool[3];
        private readonly string[] _roomNames = { "Pixel Lab", "Feature Map Lab", "Model Design Lab" };
        private readonly string[] _roomRoles = {
            "CNN의 출발점: 이미지의 숫자와 합성곱\n픽셀의 RGB 값을 살펴보고, 같은 위치의 입력과 가중치를 곱해 더합니다.\n필터를 잡아 이미지 위로 옮기는 것부터 시작하세요.",
            "CNN의 특징 추출: 계산을 반복해 피처맵 만들기\n앞 방에서 배운 합성곱을 여러 위치에 적용하면 특징 맵이 됩니다.\n필터를 움직이며 이미지의 어느 부분에 반응하는지 관찰하세요.",
            "CNN의 모델 구성: 처리 단계를 연결하기\n합성곱과 활성화 등의 블록을 연결해 이미지 특징을 처리하는 모델을 구성합니다.\n모델을 제작하고 학습·결과를 확인하세요." };
        private string _lastMission;
        private int _lastProgress = -1;
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
        private Scene _tutorialScene;
        private bool _tutorialComplete;
        private FactoryFlowPortalController _portal;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private int _skippedStage = -1;
#endif

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
            _camera = rig.Camera;
            PlaceAtEntrance(rig);
            _interactors = rig.GetComponentsInChildren<XRBaseInteractor>(true);
            foreach (FactoryAreaEntryController area in _areas)
            {
                area.Bind(rig.Camera.transform);
            }
            _routeGuide.Bind(rig.Camera.transform);
            PublishMap();
            _tablet?.SetFactoryMission(0, 0, 0, "양피지 5개를 수집하고 세 방의 체험을 완료하세요. 먼저 공장 안내를 읽어보세요.");
            CreateRouteInstruction();
            FactoryRoomSignController signs = gameObject.AddComponent<FactoryRoomSignController>();
            signs.Initialize(_font);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            FactoryDebugController debugController = gameObject.AddComponent<FactoryDebugController>();
            debugController.Initialize(this, _font);
#endif
            SceneManager.SetActiveScene(gameObject.scene);
            // Keep the UI input module in the factory scene when the guide scene is unloaded.
            rig.GetComponent<XR.XRPlayerInputController>()?.EnsureRayReady();
            StartCoroutine(LoadEntranceGuide(rig));
        }

        private void CreateRouteInstruction()
        {
            _portal = new GameObject("Factory route instruction")
                .AddComponent<FactoryFlowPortalController>();
            _portal.transform.SetParent(transform, false);
            _portal.transform.SetPositionAndRotation(_entrance.position + _entrance.forward * 3f + Vector3.up * 1.4f,
                _entrance.rotation);
            _portal.Initialize(_font);
            _portal.gameObject.SetActive(false);
        }

        private IEnumerator LoadEntranceGuide(XROrigin rig)
        {
            if (!Application.CanStreamedLevelBeLoaded(TutorialSceneName)
                || SceneManager.GetSceneByName(TutorialSceneName).isLoaded)
            {
                Debug.LogError("[FactoryFlow] FactoryTutorial 씬의 빌드 등록과 중복 로드 여부를 확인해 주세요.", this);
                enabled = false;
                yield break;
            }
            AsyncOperation loading = null;
            try
            {
                loading = SceneManager.LoadSceneAsync(TutorialSceneName, LoadSceneMode.Additive);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
            if (loading == null)
            {
                Debug.LogError("[FactoryFlow] 공장 입구 안내 씬을 열지 못했습니다.", this);
                enabled = false;
                yield break;
            }
            yield return loading;
            _tutorialScene = SceneManager.GetSceneByName(TutorialSceneName);
            FactoryTutorialManager tutorial = null;
            foreach (GameObject root in _tutorialScene.GetRootGameObjects())
            {
                tutorial = root.GetComponentInChildren<FactoryTutorialManager>(true);
                if (tutorial != null)
                {
                    break;
                }
            }
            if (tutorial == null || !tutorial.Initialize(rig, _entrance))
            {
                Debug.LogError("[FactoryFlow] FactoryTutorial의 Manager·안내 패널 연결을 확인해 주세요.", this);
                yield return SceneManager.UnloadSceneAsync(_tutorialScene);
                enabled = false;
                yield break;
            }
            while (tutorial != null && !tutorial.IsComplete)
            {
                yield return null;
            }
            if (tutorial == null)
            {
                Debug.LogError("[FactoryFlow] 완료 전에 공장 입구 안내가 제거되었습니다.", this);
                enabled = false;
                yield break;
            }
            yield return SceneManager.UnloadSceneAsync(_tutorialScene);
            _tutorialComplete = true;
            _portal.gameObject.SetActive(true);
            _routeGuide.SetDestination(0);
            UpdateMission(0);
        }

        private void Update()
        {
            if (!_tutorialComplete || _loading || _leaving || Time.time < _nextCheckTime)
            {
                return;
            }
            _nextCheckTime = Time.time + 0.1f;
            if (_failedArea != null && !_failedArea.IsPlayerInside)
            {
                _failedArea = null;
            }
            int destination = GetDestination();
            if (_stage >= 0 && destination > _stage) { _completed[_stage] = true; }
            UpdateMission(destination);
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
                StartCoroutine(BriefThenLoad(destination));
            }
        }

        private void UpdateMission(int destination)
        {
            if (_tablet == null) { return; }
            int collected = 0;
            int completed = 0;
            for (int i = 0; i < ParchmentLesson.All.Length; i++) { if (_tablet.HasParchment(i)) { collected++; } }
            foreach (bool done in _completed) { if (done) { completed++; } }
            int first = destination == 0 ? 0 : destination == 1 ? 2 : 4;
            int last = destination == 0 ? 1 : destination == 1 ? 3 : 4;
            string missing = "";
            if (destination < 3)
            {
                for (int i = first; i <= last; i++)
                {
                    if (!_tablet.HasParchment(i)) { missing += "「" + ParchmentLesson.All[i].Title + "」 "; }
                }
            }
            string mission = destination >= 3
                ? (collected == 5 ? "모든 미션을 완료했어요. 출구로 이동하세요." : "세 방 체험 완료! 지도에서 남은 양피지를 찾아 수집하세요. 출구로 먼저 이동할 수도 있어요.")
                : missing.Length > 0
                    ? _roomNames[destination] + "으로 이동하며 양피지를 수집하세요!\n" + missing +
                      "\n오른손으로 가리키고 Trigger를 누르세요. 놓쳐도 입장할 수 있고 지도에서 다시 찾을 수 있어요."
                    : _stage == destination ? _roomNames[destination] + " 체험을 완료하세요.\n배운 원리는 태블릿의 양피지에서 다시 읽을 수 있어요."
                    : "양피지 수집 완료! " + _roomNames[destination] + "에 들어가 체험을 시작하세요.";
            int progress = collected + completed * 10;
            if (_lastMission == mission && _lastProgress == progress) { return; }
            _lastMission = mission;
            _lastProgress = progress;
            _tablet.SetFactoryMission(Mathf.Min(destination + 1, 4), collected, completed, mission);
            _portal.SetInstruction(mission);
        }

        private IEnumerator BriefThenLoad(int index)
        {
            _briefInput = _camera.GetComponentInParent<XR.XRPlayerInputController>();
            if (_briefInput != null)
            {
                _previousBriefRay = _briefInput.TutorialRayForcedOn;
                _briefInput.TutorialRayForcedOn = true;
                _briefInput.EnsureRayReady();
            }
            _briefing = new GameObject("Room mission briefing");
            _briefing.transform.SetParent(transform, false);
            Vector3 direction = Vector3.ProjectOnPlane(_contentAnchors[index].position - _camera.transform.position, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.1f) { direction = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized; }
            _briefing.transform.SetPositionAndRotation(_camera.transform.position + direction * 2f, Quaternion.LookRotation(direction));
            FactoryTutorialPanelController panel = _briefing.AddComponent<FactoryTutorialPanelController>();
            panel.Initialize(_font, _camera);
            panel.ShowBriefing(_roomNames[index] + " · 이 방의 역할", _roomRoles[index]);
            bool accepted = false;
            panel.OnNext += () => accepted = true;
            while (!accepted && _areas[index].IsPlayerInside) { yield return null; }
            ReleaseBriefing();
            if (!accepted) { _loading = false; yield break; }
            yield return LoadLesson(index);
        }

        private void ReleaseBriefing()
        {
            if (_briefing != null) { Destroy(_briefing); _briefing = null; }
            if (_briefInput != null) { _briefInput.TutorialRayForcedOn = _previousBriefRay; _briefInput = null; }
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_skippedStage == _stage)
            {
                return _stage + 1;
            }
#endif
            bool completed = _stage == 0 ? _pixels != null && _pixels.ConvolutionCompleted
                : _stage == 1 ? _maps != null && _maps.ReadyForModelDesign
                : _model != null && _model.ReadyForFactoryExit;
            return completed ? _stage + 1 : _stage;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TrySkipCurrentLesson()
        {
            if (!isActiveAndEnabled || !_tutorialComplete || _loading || _leaving
                || _stage < 0 || _stage >= _scenes.Length)
            {
                return false;
            }
            _skippedStage = _stage;
            _nextCheckTime = 0f;
            Debug.Log($"[FactoryFlow] 개발자 건너뛰기: {_scenes[_stage]} 완료 조건 우회. 다음 방 또는 출구로 이동하세요.", this);
            return true;
        }
#endif

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
            // Face the approach from the central corridor; do not rotate the user's head.
            Quaternion facing = Quaternion.LookRotation(index < 2 ? Vector3.left : Vector3.right, Vector3.up);
            Vector3 offset = _contentAnchors[index].position - facing * Vector3.forward * 1.5f;
            content.transform.SetPositionAndRotation(offset, facing);
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
            CNNPixelLabManager nextPixels = content.GetComponentInChildren<CNNPixelLabManager>(true);
            if (nextPixels != null)
            {
                nextPixels.RebaseDock(offset, facing);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _skippedStage = -1;
#endif
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
            ReleaseBriefing();
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
            if (!_leaving && _tutorialScene.IsValid() && _tutorialScene.isLoaded && Application.isPlaying)
            {
                SceneManager.UnloadSceneAsync(_tutorialScene);
            }
            if (!_leaving && _lesson.IsValid() && _lesson.isLoaded && Application.isPlaying)
            {
                SceneManager.UnloadSceneAsync(_lesson);
            }
        }
    }
}
