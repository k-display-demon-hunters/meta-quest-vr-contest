using System.Collections;
using FactoryTutorial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns loaded scenes and their placement; the Manager decides when to advance.
    public sealed class FactorySceneLoadController : MonoBehaviour
    {
        [SerializeField] private GameObject _routeInstruction;

        private FactoryNavigationData _navigationData;
        private FactoryPlayerController _player;

        private Scene _tutorialScene;
        private bool _tutorialRunning;
        private bool _endTutorialRequested;
        private Scene _lesson;
        private Scene _preparedLesson;
        private FactoryNavigationData.DestinationData _lessonData;
        private FactoryNavigationData.DestinationData _preparedData;

        public FactoryNavigationData NavigationData => _navigationData;
        public Scene PreparedScene => _preparedLesson;
        public string LastError { get; private set; }
        public bool IsLeaving { get; private set; }
        public bool TutorialCompleted { get; private set; }

        private void Awake()
        {
            _navigationData = GetComponent<FactoryNavigationData>();
            _player = GetComponent<FactoryPlayerController>();
        }

        public void ActivateFactoryScene()
        {
            SceneManager.SetActiveScene(gameObject.scene);
        }

        public IEnumerator RunTutorial(XROrigin rig)
        {
            _tutorialRunning = true;
            TutorialCompleted = false;
            AsyncOperation loading = BeginLoad(_navigationData.TutorialSceneName, LoadSceneMode.Additive);
            if (loading == null)
            {
                Debug.LogError("[FactoryFlow] " + LastError, this);
                _tutorialRunning = false;
                yield break;
            }
            yield return loading;
            _tutorialScene = SceneManager.GetSceneByName(_navigationData.TutorialSceneName);
            if (!_endTutorialRequested)
            {
                FactoryTutorialManager tutorial = null;
                foreach (GameObject root in _tutorialScene.GetRootGameObjects())
                {
                    tutorial = root.GetComponentInChildren<FactoryTutorialManager>(true);
                    if (tutorial != null)
                    {
                        break;
                    }
                }
                tutorial.Initialize(rig, _navigationData.Entrance);
                while (!_endTutorialRequested && tutorial != null && !tutorial.IsComplete)
                {
                    yield return null;
                }
                TutorialCompleted = tutorial != null && tutorial.IsComplete;
                if (tutorial == null)
                {
                    Debug.LogError("[FactoryFlow] 완료 전에 공장 입구 안내가 제거되었습니다.", this);
                }
            }
            if (_tutorialScene.IsValid() && _tutorialScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_tutorialScene);
            }
            _tutorialScene = default;
            _routeInstruction.SetActive(TutorialCompleted);
            _tutorialRunning = false;
        }

        public IEnumerator EndTutorial()
        {
            _endTutorialRequested = true;
            while (_tutorialRunning)
            {
                yield return null;
            }
        }

        public IEnumerator PrepareLesson(FactoryDestinationId destination)
        {
            FactoryNavigationData.DestinationData data = _navigationData.Get(destination);
            AsyncOperation load = BeginLoad(data.SceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                yield break;
            }
            yield return load;
            _preparedLesson = SceneManager.GetSceneByName(data.SceneName);
            _preparedData = data;
        }

        public void PlacePreparedContent()
        {
            GameObject content = _preparedData.Content;
            // Keep the existing factory floor, lighting and persistent rig.
            foreach (GameObject root in _preparedLesson.GetRootGameObjects())
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
            Vector3 offset = _preparedData.ContentAnchor.position - Vector3.forward * 1.5f;
            content.transform.position += offset;
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
        }

        public IEnumerator CommitPreparedLesson()
        {
            if (_lesson.IsValid() && _lesson.isLoaded)
            {
                _player.ReleaseSceneObjects(_lesson);
                yield return SceneManager.UnloadSceneAsync(_lesson);
                _lessonData.ClearContent();
            }
            _lesson = _preparedLesson;
            _lessonData = _preparedData;
            _preparedLesson = default;
            _preparedData = null;
            ActivateFactoryScene();
            _lessonData.Content.SetActive(true);
        }

        public IEnumerator CancelPreparedLesson()
        {
            if (_preparedLesson.IsValid() && _preparedLesson.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_preparedLesson);
            }
            _preparedData.ClearContent();
            _preparedLesson = default;
            _preparedData = null;
        }

        public IEnumerator LoadEnding()
        {
            // Single loading owns cleanup; OnDestroy must not also unload additive scenes.
            FactoryNavigationData.DestinationData exit = _navigationData.Get(FactoryDestinationId.Exit);
            _player.ReleaseSceneObjects(_lesson);
            IsLeaving = true;
            AsyncOperation load = BeginLoad(exit.SceneName, LoadSceneMode.Single);
            if (load == null)
            {
                IsLeaving = false;
                yield break;
            }
            yield return load;
        }

        private AsyncOperation BeginLoad(string sceneName, LoadSceneMode mode)
        {
            LastError = null;
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                LastError = $"{sceneName} 씬이 이미 로드되어 있습니다.";
                return null;
            }
            try
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, mode);
                if (load != null)
                {
                    return load;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
            LastError = $"{sceneName} 씬을 열지 못했습니다.";
            return null;
        }

        private void OnDestroy()
        {
            _lessonData?.ClearContent();
            _preparedData?.ClearContent();
            if (IsLeaving || !Application.isPlaying)
            {
                return;
            }
            if (_tutorialScene.IsValid() && _tutorialScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_tutorialScene);
            }
            if (_preparedLesson.IsValid() && _preparedLesson.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_preparedLesson);
            }
            if (_lesson.IsValid() && _lesson.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_lesson);
            }
        }
    }
}
