using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class CorridorCameraController
    {
        [SerializeField] private CorridorTheme _theme;
        [SerializeField] private Transform _root;
        [SerializeField] private UnityEngine.Rendering.Volume _volume;
        private UnityEngine.Rendering.Universal.UniversalAdditionalCameraData _renderCamera;
        private bool _previousPostProcessing;
        private bool _standalone;
        private Camera _camera;
        internal Camera Camera => _camera;
        internal bool Visible
        {
            get;
            private set;
        }

        internal event Action<bool> VisibilityChanged;
        internal void Start(bool standalone)
        {
            _standalone = standalone;
            if (_standalone)
            {
#if UNITY_EDITOR
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    var rig = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                    float height = rig.Camera.transform.position.y - rig.Origin.transform.position.y;
                    if (height < .5f)
                    rig.CameraFloorOffsetObject.transform.localPosition += Vector3.up * (rig.CameraYOffset - height);
                }
#endif
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 16;
                RenderSettings.fogEndDistance = 46;
                RenderSettings.fogColor = _theme.Void;
            }
            _volume.enabled = false;
        }

        internal void Tick()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }
            if (_camera == null)
            {
                return;
            }
            Vector3 view = _root.InverseTransformPoint(_camera.transform.position);
            bool inside = Mathf.Abs(view.x) <= 5.7f && view.z >= -11.3f && view.z <= 26.8f;
            if (Visible == inside)
            {
                return;
            }
            Visible = inside;
            _volume.enabled = inside;
            if (inside)
            {
                _renderCamera = _camera.GetUniversalAdditionalCameraData();
                _previousPostProcessing = _renderCamera.renderPostProcessing;
                _renderCamera.renderPostProcessing = true;
                if (_standalone)
                {
                    _camera.backgroundColor = _theme.Void;
                }
            }
            else
            {
                RestoreCamera();
            }
            VisibilityChanged?.Invoke(inside);
        }

        internal void Disable()
        {
            RestoreCamera();
            Visible = false;
            _volume.enabled = false;
        }

        private void RestoreCamera()
        {
            if (_renderCamera == null)
            {
                return;
            }
            _renderCamera.renderPostProcessing = _previousPostProcessing;
            _renderCamera = null;
        }
    }
}
