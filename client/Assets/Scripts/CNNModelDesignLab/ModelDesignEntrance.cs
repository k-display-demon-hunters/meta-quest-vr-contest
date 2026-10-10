using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ModelDesignScene
{
    public sealed class ModelDesignEntrance : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable _interactable;
        [SerializeField] private string _sceneName = "CNNModelDesignLab";

        private bool _isLoading;

        private void OnEnable()
        {
            _interactable.selectEntered.AddListener(HandleSelected);
        }

        private void HandleSelected(SelectEnterEventArgs args)
        {
            if (!_isLoading)
            {
                StartCoroutine(EnterModelDesign());
            }
        }

        private IEnumerator EnterModelDesign()
        {
            _isLoading = true;
            // Leave the current interaction callback before loading or disabling its target.
            yield return null;
            if (!SceneManager.GetSceneByName(_sceneName).isLoaded)
            {
                if (!Application.CanStreamedLevelBeLoaded(_sceneName))
                {
                    Debug.LogError($"[ModelDesign] Scene '{_sceneName}' is not in the build scene list.", this);
                    _isLoading = false;
                    yield break;
                }
                yield return SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Additive);
            }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(HandleSelected);
            _isLoading = false;
        }
    }
}
