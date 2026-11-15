using System.Collections.Generic;
using Parchment;
using Tablet;
using UnityEngine;

namespace FactoryEnvironment
{
    public sealed class TabletMiniMapController : MonoBehaviour
    {
        [SerializeField] private Transform _inspectionDivider;

        private TabletManager _tablet;
        private TabletMapData _mapData;

        private void Awake()
        {
            _tablet = FindFirstObjectByType<TabletManager>();
        }

        private void OnEnable()
        {
            // The first map is published in Start, after the tablet initializes.
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
            _tablet.SetMapData(_mapData);
        }

        private void OnDisable()
        {
            if (_tablet != null && _mapData != null)
            {
                _tablet.ClearMapData(_mapData);
            }
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
                    bounds = floors.Count == 0 ? footprint : Union(bounds, footprint);
                    if (collider.name == "Inspection floor structural slab")
                    {
                        // The two rooms share one physical floor; split only the map at the partition.
                        float dividerX = _inspectionDivider.position.x;
                        floors.Add(new TabletMapData.Floor(
                            Rect.MinMaxRect(footprint.xMin, footprint.yMin, dividerX, footprint.yMax), "합성곱 실험실"));
                        floors.Add(new TabletMapData.Floor(
                            Rect.MinMaxRect(dividerX, footprint.yMin, footprint.xMax, footprint.yMax), "픽셀 관찰실"));
                    }
                    else
                    {
                        floors.Add(new TabletMapData.Floor(footprint, FloorName(collider.name)));
                    }
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
            if (objectName == "Tutorial floor structural slab") { return "튜토리얼실"; }
            if (objectName == "Feature floor structural slab") { return "특징 탐구실"; }
            if (objectName == "Assembly floor structural slab") { return "CNN 모델 제작실"; }
            if (objectName == "FrontCourtToTutorialRoom floor structural slab"
                || objectName == "TutorialRoomToInspectionRoom floor structural slab"
                || objectName == "InspectionRoomToFeatureMapRoom floor structural slab"
                || objectName == "FeatureMapRoomToModelDesignRoom floor structural slab"
                || objectName == "ModelDesignRoomToRearCourt floor structural slab")
            {
                return "연결 통로";
            }
            if (objectName.StartsWith("Corridor ", System.StringComparison.Ordinal))
            {
                return "복도 " + objectName["Corridor ".Length];
            }
            return "공장";
        }

    }
}
