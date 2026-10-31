using System.Collections.Generic;
using System;
using Parchment;
using Tablet;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryMapController : MonoBehaviour
    {
        public TabletMapData MapData { get; private set; }
        public event Action<TabletMapData> MapChanged;

        private void OnEnable()
        {
            // OnEnable can precede session initialization; publish an existing map on reactivation.
            if (MapData != null)
            {
                PublishMap();
            }
        }

        public void Initialize()
        {
            PublishMap();
        }

        private void PublishMap()
        {
            MapData = ReadMap();
            MapChanged?.Invoke(MapData);
        }

        private TabletMapData ReadMap()
        {
            List<TabletMapData.Floor> floors = new List<TabletMapData.Floor>();
            List<Rect> walls = new List<Rect>();
            List<TabletMapData.Parchment> parchments = new List<TabletMapData.Parchment>();
            Rect bounds = default;
            // The existing scene names distinguish structural floors/walls from roofs and machines.
            foreach (BoxCollider collider in gameObject.GetComponentsInChildren<BoxCollider>())
            {
                Bounds colliderBounds = collider.bounds;
                Rect footprint = Rect.MinMaxRect(colliderBounds.min.x, colliderBounds.min.z, colliderBounds.max.x, colliderBounds.max.z);
                if (collider.name.EndsWith("structural slab", System.StringComparison.Ordinal))
                {
                    floors.Add(new TabletMapData.Floor(footprint, FloorName(collider.name)));
                    bounds = floors.Count == 1 ? footprint : Union(bounds, footprint);
                }
                else if (collider.name.Contains("concrete") && colliderBounds.size.y >= 1f && colliderBounds.min.y < 0.6f)
                {
                    walls.Add(footprint);
                }
            }
            foreach (ParchmentPickupController pickup in gameObject.GetComponentsInChildren<ParchmentPickupController>(true))
            {
                parchments.Add(new TabletMapData.Parchment(pickup, pickup.LessonIndex));
            }
            return new TabletMapData(floors, walls, parchments, bounds);
        }

        private static Rect Union(Rect first, Rect second)
        {
            return Rect.MinMaxRect(Mathf.Min(first.xMin, second.xMin), Mathf.Min(first.yMin, second.yMin),
                Mathf.Max(first.xMax, second.xMax), Mathf.Max(first.yMax, second.yMax));
        }

        private static string FloorName(string objectName)
        {
            if (objectName.StartsWith("Entrance", System.StringComparison.Ordinal)) { return "공장 입구"; }
            if (objectName.StartsWith("Rear", System.StringComparison.Ordinal)) { return "공장 출구"; }
            if (objectName.StartsWith("Tutorial to", System.StringComparison.Ordinal)) { return "연결 통로"; }
            if (objectName.StartsWith("Tutorial", System.StringComparison.Ordinal)) { return "튜토리얼"; }
            if (objectName.StartsWith("Inspection", System.StringComparison.Ordinal)) { return "ROOM #1 · 픽셀 관찰·합성곱"; }
            if (objectName.StartsWith("Feature", System.StringComparison.Ordinal)) { return "ROOM #2 · Feature Map Lab"; }
            if (objectName.StartsWith("Assembly", System.StringComparison.Ordinal)) { return "ROOM #3 · Model Design Lab"; }
            if (objectName.StartsWith("Corridor ", System.StringComparison.Ordinal))
            {
                return "복도 " + objectName["Corridor ".Length];
            }
            return "공장";
        }

    }
}
