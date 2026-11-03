using System;
using System.Collections;
using System.Collections.Generic;
using Learning;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    public sealed class FactorySceneLoadController : MonoBehaviour
    {
        [SerializeField] private FactoryAreaController _areaController;
        [FormerlySerializedAs("_navigationData")]
        [SerializeField] private FactoryLayoutData _layoutData;
        private readonly Dictionary<LearningStageId, Scene> _loadedScenes = new Dictionary<LearningStageId, Scene>();
        private int _requestedIndex;
        private bool _hasPendingArea;
        private bool _loadingSingle;
        private bool _loadFailed;

        public bool IsBusy { get; private set; }
        public bool IsLoaded(LearningStageId stage) => _loadedScenes.ContainsKey(stage);
        public event Action StateChanged;
        public event Action<LearningStageId, Scene> ContentLoaded;
        public event Func<LearningStageId, Scene, IEnumerator> ContentUnloading;
        public event Action<LearningStageId, string> LoadFailed;

        private void OnEnable() => _areaController.AreaChanged += HandleAreaChanged;
        private void OnDisable() => _areaController.AreaChanged -= HandleAreaChanged;

        private void HandleAreaChanged(FactoryAreaId area)
        {
            for (int i = 0; i < _layoutData.AreaOrder.Length; i++)
            {
                if (_layoutData.AreaOrder[i].Id != area) { continue; }
                _requestedIndex = i;
                if (IsBusy)
                {
                    _hasPendingArea = true;
                    return;
                }
                IsBusy = true;
                StateChanged?.Invoke();
                StartCoroutine(UpdateScenes());
                return;
            }
        }

        private IEnumerator UpdateScenes()
        {
            int index = _requestedIndex;
            _hasPendingArea = false;
            _loadFailed = false;
            // Finish notifying area listeners before loading or unloading scenes.
            yield return null;
            if (_layoutData.AreaOrder[index].Stage == LearningStageId.Exit)
            {
                if (!_hasPendingArea)
                    yield return Load(LearningStageId.Exit, LoadSceneMode.Single);
            }
            else
            {
                var required = new List<LearningStageId>();
                for (int i = Math.Max(0, index - 1); i <= Math.Min(index + 1, _layoutData.AreaOrder.Length - 1); i++)
                {
                    LearningStageId? stage = _layoutData.AreaOrder[i].Stage;
                    if (stage.HasValue && stage.Value != LearningStageId.None && stage.Value != LearningStageId.Exit
                        && !required.Contains(stage.Value))
                        required.Add(stage.Value);
                }
                foreach (LearningStageId stage in required)
                {
                    if (_hasPendingArea) { break; }
                    if (!IsLoaded(stage))
                    {
                        yield return Load(stage, LoadSceneMode.Additive);
                        if (_loadFailed) { break; }
                    }
                }
                if (!_loadFailed && !_hasPendingArea)
                {
                    foreach (LearningStageId stage in new List<LearningStageId>(_loadedScenes.Keys))
                    {
                        if (_hasPendingArea) { break; }
                        if (!required.Contains(stage)) { yield return Unload(stage); }
                    }
                }
            }
            // Only an AreaChanged received during this operation schedules another pass.
            if (_hasPendingArea)
            {
                StartCoroutine(UpdateScenes());
                yield break;
            }
            IsBusy = false;
            StateChanged?.Invoke();
        }

        private IEnumerator Load(LearningStageId stage, LoadSceneMode mode)
        {
            string sceneName = LearningStageCatalog.GetSceneName(stage);
            _loadFailed = false;
            // sceneLoaded runs before Start, so subscribers can place and initialize content.
            void HandleLoaded(Scene scene, LoadSceneMode loadedMode)
            {
                if (scene.name != sceneName) { return; }
                SceneManager.sceneLoaded -= HandleLoaded;
                if (mode == LoadSceneMode.Single) { return; }
                if (this == null)
                {
                    SceneManager.UnloadSceneAsync(scene);
                    return;
                }
                _loadedScenes.Add(stage, scene);
                ContentLoaded?.Invoke(stage, scene);
                SceneManager.SetActiveScene(gameObject.scene);
            }
            SceneManager.sceneLoaded += HandleLoaded;
            AsyncOperation operation = null;
            string error = null;
            try { operation = SceneManager.LoadSceneAsync(sceneName, mode); }
            catch (Exception exception) { error = exception.Message; }
            if (operation == null)
            {
                SceneManager.sceneLoaded -= HandleLoaded;
                _loadFailed = true;
                string message = error ?? sceneName + " 씬을 열지 못했습니다.";
                Debug.LogError(message, this);
                LoadFailed?.Invoke(stage, message);
                yield break;
            }
            _loadingSingle = mode == LoadSceneMode.Single;
            yield return operation;
            yield return null;
        }

        private IEnumerator Unload(LearningStageId stage)
        {
            Scene scene = _loadedScenes[stage];
            if (ContentUnloading != null)
            {
                foreach (Func<LearningStageId, Scene, IEnumerator> handler in ContentUnloading.GetInvocationList())
                    yield return handler(stage, scene);
            }
            _loadedScenes.Remove(stage);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private void OnDestroy()
        {
            if (_loadingSingle || !Application.isPlaying) { return; }
            foreach (Scene scene in _loadedScenes.Values)
            {
                if (scene.IsValid() && scene.isLoaded) { SceneManager.UnloadSceneAsync(scene); }
            }
        }
    }
}
