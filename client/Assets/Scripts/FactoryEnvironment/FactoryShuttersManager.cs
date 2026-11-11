using System;
using System.Collections.Generic;
using Learning;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryShuttersManager : MonoBehaviour
    {
        private enum AutoOpenState
        {
            PendingPermission,
            WaitingForApproach,
            WaitingForExit,
            Finished
        }

        [SerializeField] private FactoryShutterController[] _shutters;
        [SerializeField] private FactoryLayoutData _layoutData;
        [SerializeField] private LearningSessionManager _session;
        [SerializeField] private FactoryAreaController _areaController;
        private readonly Dictionary<FactoryShutterId, AutoOpenState> _autoOpenStates =
            new Dictionary<FactoryShutterId, AutoOpenState>();
        private Func<FactoryShutterId, bool> _canOpen;

        public void Configure(Func<FactoryShutterId, bool> canOpen)
        {
            _canOpen = canOpen;
        }

        private void OnEnable()
        {
            _areaController.ShutterProximityChanged += HandleProximityChanged;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
        }

        private void OnDisable()
        {
            _areaController.ShutterProximityChanged -= HandleProximityChanged;
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
        }

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            if (!available) { return; }

            FactoryShutterId exitShutter = _layoutData.GetStageSpace(stage).ExitShutter;
            if (!_autoOpenStates.TryAdd(exitShutter, AutoOpenState.PendingPermission)) { return; }

            RefreshShutter(GetShutter(exitShutter), _areaController.IsNearShutter(exitShutter));
        }

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
            FactoryShutterId id = shutter.Id;
            bool hasAutoOpen = _autoOpenStates.TryGetValue(id, out AutoOpenState state);
            if (hasAutoOpen && state == AutoOpenState.WaitingForApproach)
            {
                if (isNear)
                {
                    _autoOpenStates[id] = AutoOpenState.WaitingForExit;
                }
                return;
            }

            if (hasAutoOpen && state == AutoOpenState.WaitingForExit)
            {
                if (isNear) { return; }

                _autoOpenStates[id] = AutoOpenState.Finished;
            }

            bool pendingAutoOpen = hasAutoOpen && state == AutoOpenState.PendingPermission;
            bool canOpen = (isNear || pendingAutoOpen) && _canOpen(id);
            if (canOpen)
            {
                shutter.Open();
                if (pendingAutoOpen)
                {
                    _autoOpenStates[id] = isNear ? AutoOpenState.WaitingForExit : AutoOpenState.WaitingForApproach;
                }
            }
            else if (!isNear)
            {
                shutter.Close();
            }

            shutter.SetUnavailable(isNear && !canOpen);
        }

        private FactoryShutterController GetShutter(FactoryShutterId id) =>
            Array.Find(_shutters, shutter => shutter.Id == id);
    }
}
