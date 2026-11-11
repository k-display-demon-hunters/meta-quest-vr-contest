using System.Collections.Generic;
using Learning;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private LearningSessionManager _session;
        private FactoryLayoutData _layoutData;
        private LearningStageId _destination = LearningStageId.None;

        private void Awake()
        {
            _layoutData = GetComponentInParent<FactoryLayoutData>();
        }

        private void OnEnable()
        {
            _areaController.AreaChanged += HandleAreaChanged;
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
        }

        private void OnDisable()
        {
            _areaController.AreaChanged -= HandleAreaChanged;
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
            SetDestination(LearningStageId.None);
        }

        private void Start() => SetDestination(LearningStageId.Tutorial);

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (area.Stage == _destination)
            {
                SetDestination(LearningStageId.None);
            }
        }

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            if (!available) { return; }

            IReadOnlyList<LearningStageDefinition> stages = LearningStageCatalog.Stages;
            for (int index = 0; index < stages.Count; index++)
            {
                if (stages[index].Id != stage) { continue; }

                LearningStageId destination = index + 1 < stages.Count
                    ? stages[index + 1].Id
                    : LearningStageId.Exit;
                SetDestination(destination);
                return;
            }
        }

        private void SetDestination(LearningStageId destination)
        {
            if (destination == _destination)
            {
                return;
            }
            _destination = destination;
            foreach (FactoryLayoutData.StageSpace data in _layoutData.StageSpaces)
            {
                data.Route.SetActive(data.Id == _destination);
            }
        }
    }
}
