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
            WaitingForExit
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
            _session.StageCompleted += HandleStageCompleted;
        }

        private void OnDisable()
        {
            _areaController.ShutterProximityChanged -= HandleProximityChanged;
            _session.StageCompleted -= HandleStageCompleted;
        }

        private void HandleStageCompleted(LearningStageId stage)
        {
            FactoryShutterId exitShutter = _layoutData.GetStageSpace(stage).ExitShutter;
            _autoOpenStates.Add(exitShutter, AutoOpenState.PendingPermission);
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

                _autoOpenStates.Remove(id);
                hasAutoOpen = false;
            }

            bool canOpen = (isNear || hasAutoOpen) && _canOpen(id);
            if (canOpen)
            {
                shutter.Open();
                if (hasAutoOpen)
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
