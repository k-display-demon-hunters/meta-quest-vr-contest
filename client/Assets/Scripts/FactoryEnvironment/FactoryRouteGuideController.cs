using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryRouteGuideController : MonoBehaviour
    {
        [SerializeField] private GameObject[] _routes;
        private int _destination = -1;

        public void Bind(Transform player)
        {
            _destination = -1;
            if (_routes == null || _routes.Length != 4)
            {
                Debug.LogError("[FactoryRoute] 네 개의 고정 바닥 경로를 연결해 주세요.", this);
                return;
            }
            foreach (GameObject route in _routes)
            {
                if (route != null)
                {
                    route.SetActive(false);
                }
            }
        }

        public void SetDestination(int destination)
        {
            if (_routes == null || destination == _destination)
            {
                return;
            }
            _destination = destination;
            for (int index = 0; index < _routes.Length; index++)
            {
                if (_routes[index] != null)
                {
                    _routes[index].SetActive(index == destination);
                }
            }
        }

        private void OnDisable() => SetDestination(-1);
    }
}
