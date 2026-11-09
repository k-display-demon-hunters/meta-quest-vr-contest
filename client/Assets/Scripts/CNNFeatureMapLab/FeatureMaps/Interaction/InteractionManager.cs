using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class InteractionManager
    {
        [SerializeField] private ZoneSelectionController _zones;
        private readonly FeatureMapPointerController _pointer = new FeatureMapPointerController();
        internal event Action<int> ZoneSelected;
        internal string HoverName => _pointer.HoverName;
        internal string LastClick => _pointer.LastClick;
        internal void Initialize()
        {
            _zones.Bind(this);
        }

        internal void Enable()
        {
            _zones.Selected += OnZone;
        }

        internal void Disable()
        {
            _zones.Selected -= OnZone;
            _pointer.Suspend();
        }

        private void OnZone(int zone) => ZoneSelected?.Invoke(zone);
        internal void Tick(Camera camera) => _pointer.Tick(camera);
        internal void Suspend() => _pointer.Suspend();
        internal void Register(Collider collider, Action<Vector3> click, Action<Vector3> twice = null, Action<Vector3?> hover = null) => _pointer.Register(collider, click, twice, hover);
        internal void Dispose() => _pointer.Dispose();
    }
}
