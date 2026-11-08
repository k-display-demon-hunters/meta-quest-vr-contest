using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RealityLab
{
    // The scene owns passthrough; the shared rig keeps its tablet and controller input.
    public sealed class RealityPassthroughController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private UniversalAdditionalCameraData _cameraData;
        private CameraClearFlags _clearFlags;
        private Color _background;
        private bool _postProcessing;
        private bool _hdr;

        public void Initialize(Camera camera)
        {
            _camera = camera;
            _cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        }

        private void OnEnable()
        {
            _clearFlags = _camera.clearFlags;
            _background = _camera.backgroundColor;
            _postProcessing = _cameraData.renderPostProcessing;
            _hdr = _camera.allowHDR;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _cameraData.renderPostProcessing = false;
            _camera.allowHDR = false;
        }

        private void OnDisable()
        {
            // The rig may already have been destroyed when the application closes.
            if (_camera != null)
            {
                _camera.clearFlags = _clearFlags;
                _camera.backgroundColor = _background;
                _camera.allowHDR = _hdr;
            }
            if (_cameraData != null)
                _cameraData.renderPostProcessing = _postProcessing;
        }
    }
}
