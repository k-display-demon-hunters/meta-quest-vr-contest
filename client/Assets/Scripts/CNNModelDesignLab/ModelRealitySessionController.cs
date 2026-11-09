using System;
using System.Threading.Tasks;
using RealityLab;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ModelDesignScene
{
    public sealed class ModelRealitySessionController : MonoBehaviour
    {
        private readonly RealityPresentationController _presentation = new RealityPresentationController();
        private Scene _realityScene;
        private bool _transitioning;
        public bool IsOpen => _realityScene.IsValid() && _realityScene.isLoaded;
        public CNNRealityLabManager Reality { get; private set; }
        public async Task EnterAsync(CNNModelDesignLabManager source, Camera camera)
        {
            if (_transitioning || IsOpen) { throw new InvalidOperationException("Reality transition is already active."); }
            if (SceneManager.GetSceneByName("FactoryEnvironment").isLoaded)
                throw new InvalidOperationException("This entry point currently supports standalone ModelDesign only.");
            _transitioning = true;
            try
            {
                AsyncOperation load = SceneManager.LoadSceneAsync("CNNRealityLab", LoadSceneMode.Additive);
                while (!load.isDone) { await Task.Yield(); }
                _realityScene = SceneManager.GetSceneByName("CNNRealityLab");
                RealityPassthroughController passthrough = null;
                foreach (GameObject root in _realityScene.GetRootGameObjects())
                {
                    RealityPassthroughController[] controllers = root.GetComponentsInChildren<RealityPassthroughController>(true);
                    if (controllers.Length > 0) { passthrough = controllers[0]; }
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = camera; }
                }
                passthrough.Initialize(camera);
                passthrough.gameObject.SetActive(true);
                Reality = passthrough.gameObject.AddComponent<CNNRealityLabManager>();
                Reality.Initialize(source);
                _presentation.Hide(source.gameObject.scene);
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
            _presentation.Restore();
            Reality = null;
            if (IsOpen)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_realityScene);
                while (!unload.isDone) { await Task.Yield(); }
            }
            _realityScene = default;
        }
        private void OnDestroy()
        {
            _presentation.Restore();
            if (IsOpen) { SceneManager.UnloadSceneAsync(_realityScene); }
        }
    }
}
