using UnityEngine;
using Learning;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        private FactoryNavigationData _navigationData;
        private LearningStageId _destination = LearningStageId.None;

        private void Awake()
        {
            _navigationData = GetComponentInParent<FactoryNavigationData>();
        }

        private void Start()
        {
            foreach (FactoryNavigationData.DestinationData data in _navigationData.Destinations)
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
            foreach (FactoryNavigationData.DestinationData data in _navigationData.Destinations)
            {
                data.Route.SetActive(data.Id == _destination);
            }
        }


        private void OnDisable() => SetDestination(LearningStageId.None);
    }
}
