using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        [SerializeField] private GameObject _room1PassageRoute;
        private FactoryNavigationData _navigationData;
        private FactoryDestinationId _destination = FactoryDestinationId.None;

        public FactoryNavigationData NavigationData => _navigationData;

        private void Awake()
        {
            _navigationData = GetComponentInParent<FactoryNavigationData>();
        }

        private void Start()
        {
            _room1PassageRoute.SetActive(false);
            foreach (FactoryNavigationData.DestinationData data in _navigationData.Destinations)
            {
                data.Route.SetActive(data.Id == _destination);
            }
        }

        public void SetDestination(FactoryDestinationId destination)
        {
            _room1PassageRoute.SetActive(false);
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

        public void SetRoom1PassageVisible(bool visible)
        {
            if (visible) { SetDestination(FactoryDestinationId.None); }
            _room1PassageRoute.SetActive(visible);
        }

        private void OnDisable() => SetDestination(FactoryDestinationId.None);
    }
}
