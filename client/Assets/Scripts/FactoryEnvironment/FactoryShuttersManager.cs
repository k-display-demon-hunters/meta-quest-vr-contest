using System;
using System.Collections.Generic;
using Learning;
using Parchment;
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
        private readonly HashSet<FactoryShutterId> _permittedShutters = new HashSet<FactoryShutterId>();
        private readonly HashSet<LearningStageId> _availableStages = new HashSet<LearningStageId>();
        private ParchmentManager _parchment;

        private void Awake()
        {
            _parchment = FindFirstObjectByType<ParchmentManager>();
        }

        private void OnEnable()
        {
            _areaController.ShutterProximityChanged += HandleProximityChanged;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
            _parchment.Collected += HandleParchmentCollected;
        }

        private void OnDisable()
        {
            _areaController.ShutterProximityChanged -= HandleProximityChanged;
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
            _parchment.Collected -= HandleParchmentCollected;
        }

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            if (available)
            {
                _availableStages.Add(stage);
                FactoryShutterId exitShutter = _layoutData.GetStageSpace(stage).ExitShutter;
                _autoOpenStates.TryAdd(exitShutter, AutoOpenState.PendingPermission);
            }
            else
            {
                _availableStages.Remove(stage);
            }
            RefreshConditions();
        }

        private void HandleParchmentCollected(int documentId) => RefreshConditions();

        private void HandleProximityChanged(FactoryShutterId id, bool isNear)
        {
            RefreshShutter(GetShutter(id), isNear);
        }

        private void RefreshConditions()
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
            bool canOpen = (isNear || pendingAutoOpen) && CanOpenShutter(id);
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

        private bool CanOpenShutter(FactoryShutterId id)
        {
            if (_permittedShutters.Contains(id)) { return true; }

            foreach (FactoryLayoutData.StageSpace space in _layoutData.StageSpaces)
            {
                if (space.ExitShutter == id && !_availableStages.Contains(space.Id)) { return false; }
                if (space.EntryShutter == id && !CanEnterStage(space.Id)) { return false; }
            }
            // Keep an earned passage permission across closing, resets and scene reloads.
            _permittedShutters.Add(id);
            return true;
        }

        private bool CanEnterStage(LearningStageId stage)
        {
            LearningStageId previous = LearningStageId.None;
            foreach (LearningStageDefinition definition in LearningStageCatalog.Stages)
            {
                if (definition.Id == stage)
                {
                    return (previous == LearningStageId.None || _availableStages.Contains(previous))
                        && _parchment.HasCollectedAll(definition.EntranceParchmentIds);
                }
                previous = definition.Id;
            }
            // Exit follows the last learning stage and has no entrance parchment.
            return stage == LearningStageId.Exit && _availableStages.Contains(previous);
        }

        private FactoryShutterController GetShutter(FactoryShutterId id) =>
            Array.Find(_shutters, shutter => shutter.Id == id);
    }
}
