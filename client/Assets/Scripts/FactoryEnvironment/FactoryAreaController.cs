using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace FactoryEnvironment
{
    public sealed class FactoryAreaController : MonoBehaviour
    {
        [Serializable]
        private sealed class Area
        {
            [SerializeField] private FactoryAreaId _id;
            [FormerlySerializedAs("_entryVolume")]
            [SerializeField] private BoxCollider _volume;
            [SerializeField] private BoxCollider[] _floorVolumes;

            public FactoryAreaId Id => _id;

            public bool ContainsVolume(Vector3 position) =>
                _volume != null && Contains(_volume, position, false);

            public bool ContainsFloor(Vector3 position)
            {
                foreach (BoxCollider floor in _floorVolumes)
                {
                    if (Contains(floor, position, true)) { return true; }
                }
                return false;
            }

            private static bool Contains(BoxCollider volume, Vector3 position, bool floor)
            {
                if (!volume.enabled || !volume.gameObject.activeInHierarchy) { return false; }
                Vector3 local = volume.transform.InverseTransformPoint(position) - volume.center;
                Vector3 halfSize = volume.size * 0.5f;
                return Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.z) <= halfSize.z
                    && (floor || Mathf.Abs(local.y) <= halfSize.y);
            }
        }

        [SerializeField] private Area[] _areas;
        private Transform _player;

        public FactoryAreaId CurrentArea { get; private set; }
        public event Action<FactoryAreaId> AreaChanged;

        private void Start()
        {
            _player = FindFirstObjectByType<XROrigin>().Camera.transform;
        }

        private void LateUpdate()
        {
            Vector3 position = _player.position;
            FactoryAreaId areaId = FactoryAreaId.None;
            foreach (Area area in _areas)
            {
                if (area.ContainsVolume(position))
                {
                    areaId = area.Id;
                    break;
                }
                if ((areaId == FactoryAreaId.None || area.Id == CurrentArea) && area.ContainsFloor(position))
                    areaId = area.Id;
            }
            if (CurrentArea == areaId) { return; }
            CurrentArea = areaId;
            AreaChanged?.Invoke(CurrentArea);
        }

    }
}
