using System;
using System.Collections;
using FactoryTutorial;
using Learning;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Owns loaded content and asynchronous operations; the session chooses their order.
    public sealed class FactorySceneLoadController : MonoBehaviour
    {
        private sealed class Lesson
        {
            public Scene Scene;
            public GameObject Root;
            public FactoryTutorialManager Tutorial;
            public ILearningExperience Experience;
        }

        [SerializeField] private FactoryNavigationData _navigationData;
        private Lesson _current;
        private Lesson _prepared;
        private string _loadingSceneName;
        private bool _destroying;
        public bool HasPreparedLesson => _prepared != null;
        public Scene CurrentScene => _current == null ? default : _current.Scene;
        public string LastError { get; private set; }
        public bool IsLeaving { get; private set; }
        public FactoryTutorialManager PreparedTutorial => _prepared.Tutorial;
        public ILearningExperience PreparedExperience => _prepared.Experience;

        public IEnumerator Prepare(LearningStageId stage)
        {
            _loadingSceneName = LearningStageCatalog.GetSceneName(stage);
            SceneManager.sceneLoaded += HoldLoadedLesson;
            AsyncOperation loading = BeginLoad(_loadingSceneName);
            if (loading == null)
            {
                SceneManager.sceneLoaded -= HoldLoadedLesson;
                yield break;
            }
            loading.completed += HandleLessonLoaded;
            yield return loading;
            Scene scene = SceneManager.GetSceneByName(_loadingSceneName);
            _prepared = FindLesson(scene, stage);
            PlaceContent(_prepared.Root, stage);
        }

        private void HoldLoadedLesson(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _loadingSceneName) { return; }
            SceneManager.sceneLoaded -= HoldLoadedLesson;
            // Configure and connect before Start caches positions or starts nested lessons.
            foreach (GameObject root in scene.GetRootGameObjects()) { root.SetActive(false); }
        }

        private static Lesson FindLesson(Scene scene, LearningStageId stage)
        {
            Lesson lesson = new Lesson { Scene = scene };
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (stage == LearningStageId.Tutorial)
                {
                    lesson.Tutorial = root.GetComponentInChildren<FactoryTutorialManager>(true);
                    if (lesson.Tutorial == null) { continue; }
                }
                else
                {
                    foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component is ILearningExperience experience && experience.StageId == stage)
                        {
                            lesson.Experience = experience;
                            break;
                        }
                    }
                    if (lesson.Experience == null) { continue; }
                }
                lesson.Root = root;
                return lesson;
            }
            throw new InvalidOperationException("Missing lesson entry point in " + scene.name);
        }

        private void PlaceContent(GameObject content, LearningStageId stage)
        {
            if (stage == LearningStageId.Tutorial)
            {
                content.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                content.transform.localScale = Vector3.one;
            }
            else
            {
                Transform anchor = _navigationData.Get(stage).ContentAnchor;
                content.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                content.transform.localScale = anchor.lossyScale;
                foreach (Light light in content.GetComponentsInChildren<Light>(true)) { light.enabled = false; }
            }
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
        }

        public void ActivatePrepared()
        {
            _current = _prepared;
            _prepared = null;
            _current.Root.SetActive(true);
        }

        public IEnumerator UnloadCurrent()
        {
            if (_current != null && _current.Scene.IsValid() && _current.Scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_current.Scene);
            _current = null;
        }

        public IEnumerator CancelPrepared()
        {
            if (_prepared != null && _prepared.Scene.IsValid() && _prepared.Scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_prepared.Scene);
            _prepared = null;
        }

        public IEnumerator LoadEnding()
        {
            IsLeaving = true;
            AsyncOperation loading = BeginLoad(LearningStageCatalog.GetSceneName(LearningStageId.Exit), LoadSceneMode.Single);
            if (loading == null) { IsLeaving = false; yield break; }
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
            catch (Exception exception) { Debug.LogException(exception, this); }
            LastError = sceneName + " 씬을 열지 못했습니다.";
            return null;
        }

        private void HandleLessonLoaded(AsyncOperation loading)
        {
            loading.completed -= HandleLessonLoaded;
            if (_destroying) { UnloadIfLoaded(_loadingSceneName); }
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
            if (IsLeaving || !Application.isPlaying) { return; }
            if (_current != null && _current.Scene.IsValid() && _current.Scene.isLoaded)
                SceneManager.UnloadSceneAsync(_current.Scene);
            if (_prepared != null && _prepared.Scene.IsValid() && _prepared.Scene.isLoaded)
                SceneManager.UnloadSceneAsync(_prepared.Scene);
        }
    }
}
