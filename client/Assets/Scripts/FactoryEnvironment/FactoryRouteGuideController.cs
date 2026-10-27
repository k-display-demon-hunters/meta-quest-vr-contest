using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        private FactoryNavigationData _navigationData;
        private FactoryDestinationId _destination = FactoryDestinationId.None;

        public FactoryNavigationData NavigationData => _navigationData;

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

        public void SetDestination(FactoryDestinationId destination)
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

        private void OnDisable() => SetDestination(FactoryDestinationId.None);
    }
}
