using UnityEngine;

namespace FactoryEnvironment
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FactoryAreaEntryController : MonoBehaviour
    {
        private BoxCollider _area;
        private Transform _player;

        public bool IsPlayerInside
        {
            get
            {
                if (!isActiveAndEnabled || _area == null || !_area.enabled || _player == null)
                {
                    return false;
                }
                Vector3 local = _area.transform.InverseTransformPoint(_player.position) - _area.center;
                Vector3 halfSize = _area.size * 0.5f;
                return Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.y) <= halfSize.y
                    && Mathf.Abs(local.z) <= halfSize.z;
            }
        }

        public void Bind(Transform player)
        {
            _area = GetComponent<BoxCollider>();
            _player = player;
        }
    }
}
