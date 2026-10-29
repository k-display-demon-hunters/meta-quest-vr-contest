using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UI
{
    public sealed class PixelLabSceneLoadController : MonoBehaviour
    {
        [SerializeField] private string _screenSceneName = "CNNScreenLab";
        [SerializeField] private string _convolutionSceneName = "CNNConvolutionLab";
        private Scene _current;
        private Scene _prepared;
        private GameObject _content;
        private GameObject _preparedContent;
        private string _loadingSceneName;
        private bool _destroying;

        public CNNScreenLabManager Screen { get; private set; }
        public CNNConvolutionLabManager Convolution { get; private set; }
        public string LastError { get; private set; }

        public IEnumerator PrepareStage(bool calculation)
        {
            _loadingSceneName = calculation ? _convolutionSceneName : _screenSceneName;
            AsyncOperation loading = BeginLoad();
            if (loading == null) { yield break; }
            loading.completed += HandleLoadCompleted;
            yield return loading;
            _prepared = SceneManager.GetSceneByName(_loadingSceneName);
            foreach (GameObject root in _prepared.GetRootGameObjects())
            {
                bool isContent = calculation
                    ? root.GetComponentInChildren<CNNConvolutionLabManager>(true) != null
                    : root.GetComponentInChildren<CNNScreenLabManager>(true) != null;
                root.SetActive(false);
                if (isContent) { _preparedContent = root; }
            }
            _preparedContent.transform.SetPositionAndRotation(transform.position, transform.rotation);
            foreach (Light light in _preparedContent.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }
            foreach (Rigidbody body in _preparedContent.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
        }

        public IEnumerator CommitStage()
        {
            yield return UnloadCurrent();
            _current = _prepared;
            _content = _preparedContent;
            _prepared = default;
            _preparedContent = null;
            Screen = _content.GetComponentInChildren<CNNScreenLabManager>(true);
            Convolution = _content.GetComponentInChildren<CNNConvolutionLabManager>(true);
            _content.SetActive(true);
        }

        public IEnumerator CancelPreparedStage()
        {
            if (_prepared.IsValid() && _prepared.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_prepared);
            }
            _prepared = default;
            _preparedContent = null;
        }

        public IEnumerator UnloadStages()
        {
            yield return CancelPreparedStage();
            yield return UnloadCurrent();
        }

        private IEnumerator UnloadCurrent()
        {
            if (_current.IsValid() && _current.isLoaded)
            {
                ReleaseSceneObjects(_current);
                _content.SetActive(false);
                yield return SceneManager.UnloadSceneAsync(_current);
            }
            _current = default;
            _content = null;
            Screen = null;
            Convolution = null;
        }

        private AsyncOperation BeginLoad()
        {
            LastError = null;
            if (SceneManager.GetSceneByName(_loadingSceneName).isLoaded)
            {
                LastError = _loadingSceneName + " 씬이 이미 로드되어 있습니다.";
                return null;
            }
            try
            {
                AsyncOperation loading = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);
                if (loading != null) { return loading; }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
            LastError = _loadingSceneName + " 씬을 열지 못했습니다.";
            return null;
        }

        private void HandleLoadCompleted(AsyncOperation loading)
        {
            loading.completed -= HandleLoadCompleted;
            // A host removed during loading cannot resume its coroutine to clean up the loaded scene.
            if (_destroying)
            {
                Scene scene = SceneManager.GetSceneByName(_loadingSceneName);
                if (scene.IsValid() && scene.isLoaded) { SceneManager.UnloadSceneAsync(scene); }
            }
        }

        private static void ReleaseSceneObjects(Scene scene)
        {
            foreach (XRBaseInteractor interactor in FindObjectsByType<XRBaseInteractor>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                for (int i = interactor.interactablesSelected.Count - 1; i >= 0; i--)
                {
                    IXRSelectInteractable selected = interactor.interactablesSelected[i];
                    if (selected.transform.gameObject.scene == scene)
                    {
                        interactor.interactionManager.SelectExit(interactor, selected);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            _destroying = true;
            if (!Application.isPlaying) { return; }
            if (_prepared.IsValid() && _prepared.isLoaded) { SceneManager.UnloadSceneAsync(_prepared); }
            if (_current.IsValid() && _current.isLoaded)
            {
                ReleaseSceneObjects(_current);
                SceneManager.UnloadSceneAsync(_current);
            }
        }
    }
}
