using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace KdyIntro
{
    // Lives on the existing persistent rig, with no static/global instance or project setting changes.
    public sealed class KdyTransitionController : MonoBehaviour
    {
        private Image _blackout;
        public bool IsLoading { get; private set; }

        public void Initialize(Camera camera)
        {
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
            SceneManager.sceneLoaded += HandleSceneLoaded;
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

        public void Load(string sceneName)
        {
            if (!IsLoading && CanLoad(sceneName))
            {
                StartCoroutine(LoadWithFade(sceneName));
            }
        }

        private IEnumerator LoadWithFade(string sceneName)
        {
            IsLoading = true;
            yield return Fade(0f, 1f);
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
            if (operation != null)
            {
                yield return operation;
                yield return null;
            }
            yield return Fade(1f, 0f);
            IsLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            const float duration = .25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _blackout.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            _blackout.color = new Color(0f, 0f, 0f, to);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.StartsWith("kdy_scene", StringComparison.Ordinal))
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }
}
