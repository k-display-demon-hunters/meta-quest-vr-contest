using System.Collections;
using FactoryTutorial;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Loads and places content; the factory Manager decides when doors may unlock.
    public sealed class FactorySceneLoadController : MonoBehaviour
    {
        private FactoryNavigationData _navigationData;
        private FactoryPlayerController _player;
        private Scene _tutorialScene;
        private Scene _lesson;
        private Scene _preparedLesson;
        private FactoryNavigationData.DestinationData _lessonData;
        private FactoryNavigationData.DestinationData _preparedData;
        private string _loadingLessonName;
        private bool _destroying;

        public Scene PreparedScene => _preparedLesson;
        public bool HasPreparedLesson => _preparedLesson.IsValid() && _preparedLesson.isLoaded;
        public string LastError { get; private set; }
        public bool TutorialCompleted { get; private set; }
        public bool IsLeaving { get; private set; }

        private void Awake()
        {
            _navigationData = GetComponent<FactoryNavigationData>();
            _player = GetComponent<FactoryPlayerController>();
        }

        public void ActivateFactoryScene() => SceneManager.SetActiveScene(gameObject.scene);

        public IEnumerator RunTutorial(XROrigin rig)
        {
            TutorialCompleted = false;
            AsyncOperation loading = BeginLoad(_navigationData.TutorialSceneName);
            if (loading == null) { yield break; }
            loading.completed += HandleTutorialLoaded;
            yield return loading;
            _tutorialScene = SceneManager.GetSceneByName(_navigationData.TutorialSceneName);
            FactoryTutorialManager tutorial = null;
            foreach (GameObject root in _tutorialScene.GetRootGameObjects())
            {
                tutorial = root.GetComponentInChildren<FactoryTutorialManager>(true);
                if (tutorial != null) { break; }
            }
            tutorial.Initialize(rig, _navigationData.TutorialStart);
            while (tutorial != null && !tutorial.IsComplete) { yield return null; }
            TutorialCompleted = tutorial != null && tutorial.IsComplete;
            if (!TutorialCompleted) { LastError = "완료 전에 튜토리얼이 제거되었습니다."; }
            if (_tutorialScene.IsValid() && _tutorialScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_tutorialScene);
            }
            _tutorialScene = default;
        }

        public IEnumerator PrepareLesson(FactoryDestinationId destination)
        {
            _preparedData = _navigationData.Get(destination);
            _loadingLessonName = _preparedData.SceneName;
            SceneManager.sceneLoaded += HoldLoadedLesson;
            AsyncOperation loading = BeginLoad(_loadingLessonName);
            if (loading == null)
            {
                SceneManager.sceneLoaded -= HoldLoadedLesson;
                _preparedData = null;
                yield break;
            }
            loading.completed += HandleLessonLoaded;
            yield return loading;
            _preparedLesson = SceneManager.GetSceneByName(_loadingLessonName);
        }

        private void HoldLoadedLesson(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _loadingLessonName) { return; }
            SceneManager.sceneLoaded -= HoldLoadedLesson;
            // Place the scene before Start caches world positions or creates nested lesson scenes.
            foreach (GameObject root in scene.GetRootGameObjects()) { root.SetActive(false); }
        }

        public void PlacePreparedContent()
        {
            GameObject content = _preparedData.Content;
            Transform anchor = _preparedData.ContentAnchor;
            content.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            content.transform.localScale = anchor.lossyScale;
            foreach (Light light in content.GetComponentsInChildren<Light>(true)) { light.enabled = false; }
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
        }

        public IEnumerator CommitPreparedLesson()
        {
            yield return UnloadCurrentLesson();
            _lesson = _preparedLesson;
            _lessonData = _preparedData;
            _preparedLesson = default;
            _preparedData = null;
            ActivateFactoryScene();
            _lessonData.Content.SetActive(true);
        }

        public IEnumerator UnloadCurrentLesson()
        {
            if (_lesson.IsValid() && _lesson.isLoaded)
            {
                _player.ReleaseSceneObjects(_lesson);
                yield return SceneManager.UnloadSceneAsync(_lesson);
                _lessonData.ClearContent();
            }
            _lesson = default;
            _lessonData = null;
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
            _player.ReleaseSceneObjects(_lesson);
            IsLeaving = true;
            AsyncOperation loading = BeginLoad(
                _navigationData.Get(FactoryDestinationId.Exit).SceneName, LoadSceneMode.Single);
            if (loading == null)
            {
                IsLeaving = false;
                yield break;
            }
            yield return loading;
        }

        private AsyncOperation BeginLoad(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            LastError = null;
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                LastError = sceneName + " 씬이 이미 로드되어 있습니다.";
                return null;
            }
            try
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName, mode);
                if (loading != null) { return loading; }
            }
            catch (System.Exception exception) { Debug.LogException(exception, this); }
            LastError = sceneName + " 씬을 열지 못했습니다.";
            return null;
        }

        private void HandleLessonLoaded(AsyncOperation loading)
        {
            loading.completed -= HandleLessonLoaded;
            if (_destroying) { UnloadIfLoaded(_loadingLessonName); }
        }

        private void HandleTutorialLoaded(AsyncOperation loading)
        {
            loading.completed -= HandleTutorialLoaded;
            if (_destroying) { UnloadIfLoaded(_navigationData.TutorialSceneName); }
        }

        private static void UnloadIfLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded) { SceneManager.UnloadSceneAsync(scene); }
        }

        private void OnDestroy()
        {
            _destroying = true;
            SceneManager.sceneLoaded -= HoldLoadedLesson;
            _lessonData?.ClearContent();
            _preparedData?.ClearContent();
            // A Single ending load owns scene cleanup; do not race it with extra unload requests.
            if (IsLeaving || !Application.isPlaying) { return; }
            if (_tutorialScene.IsValid() && _tutorialScene.isLoaded) { SceneManager.UnloadSceneAsync(_tutorialScene); }
            if (_preparedLesson.IsValid() && _preparedLesson.isLoaded) { SceneManager.UnloadSceneAsync(_preparedLesson); }
            if (_lesson.IsValid() && _lesson.isLoaded) { SceneManager.UnloadSceneAsync(_lesson); }
        }
    }
}
