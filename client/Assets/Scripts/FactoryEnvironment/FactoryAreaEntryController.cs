using UnityEngine;
using System;

namespace FactoryEnvironment
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FactoryAreaEntryController : MonoBehaviour
    {
        private BoxCollider _area;
        private Transform _player;
        private bool _bound;
        private bool _inside;
        public event Action<bool> PresenceChanged;

        public bool IsPlayerInside
        {
            get
            {
                if (!isActiveAndEnabled || !_area.enabled || _player == null)
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
            _bound = true;
            RefreshPresence();
        }

        private void Update()
        {
            if (_bound) { RefreshPresence(); }
        }

        private void RefreshPresence()
        {
            bool inside = IsPlayerInside;
            if (_inside == inside) { return; }
            _inside = inside;
            PresenceChanged?.Invoke(inside);
        }

        private void OnDisable()
        {
            if (!_inside) { return; }
            _inside = false;
            PresenceChanged?.Invoke(false);
        }
    }
}
