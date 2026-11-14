using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryAreaController : MonoBehaviour
    {
        private sealed class ShutterPresence
        {
            public FactoryLayoutData.Shutter Data;
            public bool IsNear;
            public bool ReportedNear;
        }

        private FactoryLayoutData _layoutData;
        private Transform _player;
        private readonly Dictionary<FactoryShutterId, ShutterPresence> _shutters =
            new Dictionary<FactoryShutterId, ShutterPresence>();

        public FactoryLayoutData.Area CurrentArea { get; private set; }
        public event Action<FactoryLayoutData.Area> AreaChanged;
        internal event Action AreaChecked;
        public event Action<FactoryShutterId, bool> ShutterProximityChanged;

        public bool IsNearShutter(FactoryShutterId id) => _shutters[id].IsNear;

        private void Awake()
        {
            _layoutData = GetComponent<FactoryLayoutData>();
            CurrentArea = _layoutData.GetArea(FactoryAreaId.None);
            foreach (FactoryLayoutData.Shutter data in _layoutData.Shutters)
            {
                _shutters.Add(data.Id, new ShutterPresence { Data = data });
            }
        }

        private void Start()
        {
            _player = FindFirstObjectByType<XROrigin>().Camera.transform;
        }

        private void LateUpdate()
        {
            Vector3 position = _player.position;
            UpdateShutterPresence(position);
            UpdateArea(position);
            PublishShutterChanges();
            AreaChecked?.Invoke();
        }

        internal FactoryAreaId GetTeleportSource()
        {
            if (CurrentArea.Id != FactoryAreaId.InspectionRoom) { return CurrentArea.Id; }

            // The room spans both sides of the arch, outside the smaller experience volumes.
            Transform divider = _shutters[FactoryShutterId.ScreenToConvolution].Data.Frame;
            return Vector3.Dot(_player.position - divider.position, divider.forward) > 0f
                ? FactoryAreaId.ConvolutionRoom : FactoryAreaId.ScreenRoom;
        }

        private void UpdateArea(Vector3 position)
        {
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

        private void UpdateShutterPresence(Vector3 position)
        {
            foreach (ShutterPresence presence in _shutters.Values)
            {
                FactoryLayoutData.Shutter data = presence.Data;
                Vector3 offset = position - data.Frame.position;
                presence.IsNear = offset.x * offset.x + offset.z * offset.z
                    <= data.ApproachDistance * data.ApproachDistance;
            }
        }

        private void PublishShutterChanges()
        {
            foreach (ShutterPresence presence in _shutters.Values)
            {
                if (presence.ReportedNear != presence.IsNear)
                {
                    presence.ReportedNear = presence.IsNear;
                    ShutterProximityChanged?.Invoke(presence.Data.Id, presence.IsNear);
                }
            }
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
