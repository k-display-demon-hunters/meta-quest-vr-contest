using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class CorridorEnvironmentManager
    {
        [SerializeField] private Transform _root;
        [SerializeField] private CorridorRenderUtility _visuals;
        [SerializeField] private CorridorFloorController _floor;
        [SerializeField] private CorridorDoorController _door;
        [SerializeField] private CorridorCameraController _camera;
        internal event Action<bool> VisibilityChanged;
        internal Camera Camera => _camera.Camera;
        internal bool Visible => _camera.Visible;
        internal int HighlightedZone => _floor.HighlightedZone;
        internal Vector3 ExitHint => _root.InverseTransformPoint(_door.GuidePosition);
        internal void Initialize()
        {
            _visuals.InitializeRuntime();
            _floor.Bind(_visuals);
            _door.Bind(_visuals);
        }

        internal void Start(bool standalone) => _camera.Start(standalone);
        internal void Enable() => _camera.VisibilityChanged += OnVisibility;
        internal void Disable()
        {
            _camera.VisibilityChanged -= OnVisibility;
            _camera.Disable();
        }

        private void OnVisibility(bool visible) => VisibilityChanged?.Invoke(visible);
        internal void Tick(float now, float delta)
        {
            _camera.Tick();
            if (!Visible)
            {
                return;
            }
            _floor.DrawFlow(now);
            _door.Tick(delta);
            _visuals.UpdateBillboards(Camera);
        }

        internal void Highlight(int zone) => _floor.Highlight(zone);
        internal void OpenDoor() => _door.Open();
    }
}
