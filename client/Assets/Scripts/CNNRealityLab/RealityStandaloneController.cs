using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityLab
{
    // Only the single-scene launch uses the scene-owned rig. Additive model sessions keep their existing entry.
    public sealed class RealityStandaloneController : MonoBehaviour
    {
        [SerializeField] private GameObject _rig;
        [SerializeField] private Camera _camera;
        [SerializeField] private RealityPassthroughController _passthrough;

        private void Start()
        {
            if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene() != gameObject.scene)
            {
                return;
            }
            _rig.SetActive(true);
            _passthrough.Initialize(_camera);
            _passthrough.gameObject.SetActive(true);
            CNNRealityLabManager manager = _passthrough.gameObject.AddComponent<CNNRealityLabManager>();
            manager.InitializeCapturePreview();
        }
    }
}
