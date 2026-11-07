using System;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryShuttersManager : MonoBehaviour
    {
        [SerializeField] private FactoryShutterController[] _shutters;
        [SerializeField] private FactoryAreaController _areaController;
        private Func<FactoryShutterId, bool> _canOpen;

        public void Configure(Func<FactoryShutterId, bool> canOpen)
        {
            _canOpen = canOpen;
        }

        private void OnEnable() => _areaController.ShutterProximityChanged += HandleProximityChanged;

        private void OnDisable() => _areaController.ShutterProximityChanged -= HandleProximityChanged;

        private void HandleProximityChanged(FactoryShutterId id, bool isNear)
        {
            RefreshShutter(GetShutter(id), isNear);
        }

        public void RefreshConditions()
        {
            foreach (FactoryShutterController shutter in _shutters)
            {
                RefreshShutter(shutter, _areaController.IsNearShutter(shutter.Id));
            }
        }

        private void RefreshShutter(FactoryShutterController shutter, bool isNear)
        {
            shutter.SetNear(isNear);
            if (!isNear)
            {
                shutter.Close();
                return;
            }
            if (_canOpen(shutter.Id)) { shutter.Open(); }
        }

        private FactoryShutterController GetShutter(FactoryShutterId id) =>
            Array.Find(_shutters, shutter => shutter.Id == id);
    }
}
