using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
namespace UI
{
    public sealed class SceneTransitionController : MonoBehaviour
    {
        private AsyncOperation _operation;
        internal bool Succeeded { get; private set; }
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

        internal void Prepare(string sceneName)
        {
            if (!CanLoad(sceneName)) { return; }
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
                return;
            }
            _operation = operation;
            operation.allowSceneActivation = false;
        }
        internal IEnumerator Activate()
        {
            if (_operation == null) { yield break; }
            _operation.allowSceneActivation = true;
            yield return _operation;
            yield return null;
            Succeeded = true;
        }
    }
}
