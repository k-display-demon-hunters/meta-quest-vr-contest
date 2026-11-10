using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class CorridorEnvironmentManager
    {
        [SerializeField] private CorridorRenderUtility _visuals;
        [SerializeField] private CorridorFloorController _floor;
        [SerializeField] private CorridorCameraController _camera;
        internal event Action<bool> VisibilityChanged;
        internal Camera Camera => _camera.Camera;
        internal bool Visible => _camera.Visible;
        internal void Initialize()
        {
            _visuals.InitializeRuntime();
            _floor.Bind(_visuals);
        }

        internal void Start(bool standalone) => _camera.Start(standalone);
        internal void Enable() => _camera.VisibilityChanged += OnVisibility;
        internal void Disable()
        {
            _camera.VisibilityChanged -= OnVisibility;
            _camera.Disable();
        }

        private void OnVisibility(bool visible) => VisibilityChanged?.Invoke(visible);
        internal void Tick(float now)
        {
            _camera.Tick();
            if (!Visible)
            {
                return;
            }
            _floor.DrawFlow(now);
            _visuals.UpdateBillboards(Camera);
        }

        internal void Highlight(int zone) => _floor.Highlight(zone);
    }
}
