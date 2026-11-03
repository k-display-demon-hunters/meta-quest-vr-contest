using System;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryAreaController : MonoBehaviour
    {
        private FactoryLayoutData _layoutData;
        private Transform _player;

        public FactoryLayoutData.Area CurrentArea { get; private set; }
        public event Action<FactoryLayoutData.Area> AreaChanged;

        private void Awake()
        {
            _layoutData = GetComponent<FactoryLayoutData>();
            CurrentArea = _layoutData.GetArea(FactoryAreaId.None);
        }

        private void Start()
        {
            _player = FindFirstObjectByType<XROrigin>().Camera.transform;
        }

        private void LateUpdate()
        {
            Vector3 position = _player.position;
            FactoryLayoutData.Area selectedArea = _layoutData.GetArea(FactoryAreaId.None);
            foreach (FactoryLayoutData.Area area in _layoutData.Areas)
            {
                if (ContainsVolume(area, position))
                {
                    selectedArea = area;
                    break;
                }
                if ((selectedArea.Id == FactoryAreaId.None || area == CurrentArea) && ContainsFloor(area, position))
                    selectedArea = area;
            }
            if (CurrentArea == selectedArea) { return; }
            CurrentArea = selectedArea;
            AreaChanged?.Invoke(CurrentArea);
        }

        private static bool ContainsVolume(FactoryLayoutData.Area area, Vector3 position) =>
            area.Volume != null && Contains(area.Volume, position, false);

        private static bool ContainsFloor(FactoryLayoutData.Area area, Vector3 position)
        {
            foreach (BoxCollider floor in area.FloorVolumes)
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
}
