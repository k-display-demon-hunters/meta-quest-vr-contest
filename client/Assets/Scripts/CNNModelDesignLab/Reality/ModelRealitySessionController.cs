using System;
using System.Threading.Tasks;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace RealityLab
{
    public sealed class ModelRealitySessionController : MonoBehaviour
    {
        private readonly RealityPresentationController _presentation = new RealityPresentationController();
        private Scene _realityScene;
        private bool _transitioning;
        private ContinuousMoveProvider _moveProvider;
        private bool _movementWasEnabled;

        public bool IsOpen => _realityScene.IsValid() && _realityScene.isLoaded;
        public bool IsTransitioning => _transitioning;
        public async Task EnterAsync(Scene source, Camera camera,
            Func<float[], string, Task<InferenceResult>> inferAsync)
        {
            if (_transitioning || IsOpen) { throw new InvalidOperationException("Reality transition is already active."); }
            _transitioning = true;
            try
            {
                AsyncOperation load = SceneManager.LoadSceneAsync("CNNRealityLab", LoadSceneMode.Additive);
                while (!load.isDone) { await Task.Yield(); }
                _realityScene = SceneManager.GetSceneByName("CNNRealityLab");
                if (this == null)
                {
                    SceneManager.UnloadSceneAsync(_realityScene);
                    return;
                }

                _moveProvider = camera.GetComponentInParent<ContinuousMoveProvider>();
                _movementWasEnabled = _moveProvider.enabled;
                _moveProvider.enabled = false;
                _presentation.Hide(source);
                Scene factory = SceneManager.GetSceneByName("FactoryEnvironment");
                if (factory.isLoaded && factory != source) { _presentation.Hide(factory); }

                RealityPassthroughController passthrough = null;
                foreach (GameObject root in _realityScene.GetRootGameObjects())
                {
                    RealityPassthroughController[] controllers = root.GetComponentsInChildren<RealityPassthroughController>(true);
                    if (controllers.Length > 0) { passthrough = controllers[0]; }
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = camera; }
                }
                passthrough.Initialize(camera);
                passthrough.gameObject.SetActive(true);
                CNNRealityLabManager reality = passthrough.gameObject.AddComponent<CNNRealityLabManager>();
                reality.Initialize(camera, inferAsync, ExitAsync);
            }
            catch
            {
                await UnloadAsync();
                throw;
            }
            finally { _transitioning = false; }
        }
        public async Task ExitAsync()
        {
            if (_transitioning) { throw new InvalidOperationException("Wait for the current transition."); }
            _transitioning = true;
            try { await UnloadAsync(); }
            finally { _transitioning = false; }
        }
        private async Task UnloadAsync()
        {
            if (IsOpen)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_realityScene);
                while (!unload.isDone) { await Task.Yield(); }
            }
            _realityScene = default;
            RestorePresentation();
        }
        private void RestorePresentation()
        {
            _presentation.Restore();
            if (_moveProvider != null) { _moveProvider.enabled = _movementWasEnabled; }
            _moveProvider = null;
        }

        private void OnDestroy()
        {
            RestorePresentation();
            if (IsOpen && !_transitioning) { SceneManager.UnloadSceneAsync(_realityScene); }
        }
    }
}
