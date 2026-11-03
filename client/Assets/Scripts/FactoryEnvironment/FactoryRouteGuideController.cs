using UnityEngine;
using Learning;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        [SerializeField] private FactoryAreaController _areaController;
        private FactoryLayoutData _layoutData;
        private LearningStageId _destination = LearningStageId.None;

        private void Awake()
        {
            _layoutData = GetComponentInParent<FactoryLayoutData>();
        }

        private void OnEnable()
        {
            _areaController.AreaChanged += HandleAreaChanged;
        }

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (area.Stage == _destination)
            {
                SetDestination(LearningStageId.None);
            }
        }

        private void Start()
        {
            foreach (FactoryLayoutData.StageSpace data in _layoutData.StageSpaces)
            {
                data.Route.SetActive(data.Id == _destination);
            }
        }

        public void SetDestination(LearningStageId destination)
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


        private void OnDisable()
        {
            _areaController.AreaChanged -= HandleAreaChanged;
            SetDestination(LearningStageId.None);
        }
    }
}
