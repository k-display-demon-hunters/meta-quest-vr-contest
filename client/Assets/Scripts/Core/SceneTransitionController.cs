using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UI
{
    // Lives on the existing persistent rig, with no static/global instance or project setting changes.
    public sealed class SceneTransitionController : MonoBehaviour
    {
        private Image _blackout;
        private Camera _camera;
        private string _destination;
        private AsyncOperation _operation;
        private int? _openingRunId;
        private float _reportedProgress;
        private float _nextWaitReport;
        private float _fadeDuration = .25f;

        internal static SceneTransitionController Begin(Camera camera, Transform rig, string destination, int? openingRunId, float fadeDuration = .25f)
        {
            SceneTransitionController transition = Prepare(camera, rig, destination, openingRunId);
            transition._fadeDuration = fadeDuration;
            transition.BeginTransition();
            return transition;
        }

        internal static SceneTransitionController Prepare(Camera camera, Transform rig, string destination, int? openingRunId)
        {
            GameObject overlay = new GameObject("Archive transition", typeof(RectTransform));
            overlay.transform.SetParent(rig, false);
            SceneTransitionController transition = overlay.AddComponent<SceneTransitionController>();
            transition._openingRunId = openingRunId;
            transition._destination = destination;
            transition.Initialize(camera);
            if (openingRunId.HasValue) { SceneManager.sceneLoaded += transition.HandleSceneLoaded; }
            transition.StartCoroutine(transition.Preload(destination));
            return transition;
        }

        internal bool IsLoading { get; private set; }
        internal bool Succeeded { get; private set; }

        private void Initialize(Camera camera)
        {
            _camera = camera;
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + .04f;
            canvas.sortingOrder = 32000;
            GameObject overlay = new GameObject("Blackout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)overlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _blackout = overlay.GetComponent<Image>();
            _blackout.raycastTarget = false;
            _blackout.color = new Color(0f, 0f, 0f, 0f);
        }

        public static bool CanLoad(string sceneName)
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                return AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/" + sceneName + ".unity") != null;
            }
#endif
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        internal void BeginTransition()
        {
            if (IsLoading) { return; }
            IsLoading = true;
            StartCoroutine(ActivateWithFade());
        }

        private IEnumerator Preload(string sceneName)
        {
            bool canLoad = CanLoad(sceneName);
            Log($"LoadCheck destination={sceneName} canLoad={canLoad}");
            if (!canLoad) { yield break; }
            Log($"LoadStarted destination={sceneName}");
            AsyncOperation operation = null;
            try
            {
#if UNITY_EDITOR
                if (Application.isEditor)
                {
                    operation = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/" + sceneName + ".unity",
                        new LoadSceneParameters(LoadSceneMode.Single));
                }
                else
#endif
                {
                    operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            if (operation == null)
            {
                Log($"LoadFailed destination={sceneName}");
                yield break;
            }
            _operation = operation;
            operation.allowSceneActivation = false;
            _nextWaitReport = Time.realtimeSinceStartup + 5f;
            while (operation.progress < .9f) { yield return null; }
            Log("PreloadReady");
        }

        private IEnumerator ActivateWithFade()
        {
            Log("FadeOutStarted");
            yield return Fade(0f, 1f);
            Log("FadeOutCompleted");
            AsyncOperation operation = _operation;
            if (operation != null)
            {
                Log("ActivationRequested");
                operation.allowSceneActivation = true;
                yield return operation;
                _operation = null;
                Log($"LoadCompleted activeScene={SceneManager.GetActiveScene().name}");
                yield return null;
            }
            yield return Fade(1f, 0f);
            Succeeded = operation != null;
            LogViewState($"FadeInCompleted success={Succeeded}");
            IsLoading = false;
            if (Succeeded)
            {
                Destroy(gameObject);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == _destination)
            {
                Log($"SceneLoaded scene={scene.name} mode={mode}");
            }
        }

        private void Update()
        {
            if (!_openingRunId.HasValue || _operation == null) { return; }
            float progress = _operation.progress;
            float now = Time.realtimeSinceStartup;
            if (progress - _reportedProgress < .1f && now < _nextWaitReport) { return; }
            _reportedProgress = progress;
            _nextWaitReport = now + 5f;
            LogViewState($"Loading progress={progress:F2}");
        }

        private void OnDestroy()
        {
            if (!_openingRunId.HasValue) { return; }
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (IsLoading) { Log($"DestroyedDuringTransition destination={_destination}"); }
        }

        private void LogViewState(string message)
        {
            if (!_openingRunId.HasValue) { return; }
            bool cameraActive = _camera != null && _camera.isActiveAndEnabled;
            Log($"{message} activeScene={SceneManager.GetActiveScene().name} " +
                $"cameraActive={cameraActive} blackout={_blackout.color.a:F2}");
        }

        private void Log(string message)
        {
            if (_openingRunId.HasValue)
            {
                Debug.Log($"[Opening][run={_openingRunId.Value}][transition={GetInstanceID()}] {message}", this);
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _blackout.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / _fadeDuration));
                yield return null;
            }
            _blackout.color = new Color(0f, 0f, 0f, to);
        }

    }
}
