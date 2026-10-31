using UnityEngine;
using Learning;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        [SerializeField] private GameObject _convolutionEntranceRoute;
        private FactoryNavigationData _navigationData;
        private RoomId _destination = RoomId.None;

        private void Awake()
        {
            _navigationData = GetComponentInParent<FactoryNavigationData>();
        }

        private void Start()
        {
            _convolutionEntranceRoute.SetActive(false);
            foreach (FactoryNavigationData.DestinationData data in _navigationData.Destinations)
            {
                data.Route.SetActive(data.Id == _destination);
            }
        }

        public void SetDestination(RoomId destination)
        {
            _convolutionEntranceRoute.SetActive(false);
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

        public void SetConvolutionRouteVisible(bool visible)
        {
            if (visible) { SetDestination(RoomId.None); }
            _convolutionEntranceRoute.SetActive(visible);
        }

        private void OnDisable() => SetDestination(RoomId.None);
    }
}
