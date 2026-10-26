using System.Collections.Generic;
using Education;
using Tablet;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class FactoryMapController : MonoBehaviour
    {
        private TabletManager _tablet;
        private TabletMapData _mapData;

        private void OnEnable()
        {
            // OnEnable also runs before the initial Start; publish only after the first setup.
            if (_mapData != null)
            {
                PublishMap();
            }
        }

        private void Start()
        {
            PublishMap();
        }

        private void PublishMap()
        {
            _mapData = ReadMap();
            _tablet = FindFirstObjectByType<TabletManager>();
            _tablet.SetMapData(_mapData);
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
            if (objectName.StartsWith("Exterior", System.StringComparison.Ordinal))
            {
                return "하역장";
            }
            if (objectName.StartsWith("Security", System.StringComparison.Ordinal))
            {
                return "보안 구역";
            }
            if (objectName.StartsWith("Spine", System.StringComparison.Ordinal))
            {
                return "중앙 복도";
            }
            if (objectName.StartsWith("Inspection", System.StringComparison.Ordinal))
            {
                return "Pixel Lab";
            }
            if (objectName.StartsWith("Feature", System.StringComparison.Ordinal))
            {
                return "Feature Map Lab";
            }
            if (objectName.StartsWith("Assembly", System.StringComparison.Ordinal))
            {
                return "Model Design Lab";
            }
            if (objectName.StartsWith("Outbound", System.StringComparison.Ordinal))
            {
                return "출하 구역";
            }
            return "공장";
        }

        private void OnDisable()
        {
            // The tablet may already be destroyed during scene teardown, or Start may not have run.
            if (_tablet != null)
            {
                _tablet.ClearMapData(_mapData);
            }
        }
    }
}
