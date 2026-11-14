using System;
using Learning;
using UnityEngine;
using UnityEngine.Serialization;

namespace FactoryEnvironment
{
    public enum FactoryAreaId
    {
        None,
        TutorialRoom = 1,
        ScreenRoom = 2,
        ConvolutionRoom = 3,
        FeatureMapRoom = 4,
        ModelDesignRoom = 5,
        RearCourt = 6,
        FrontCourt = 7,
        Outside = 8,
        FrontCourtToTutorialRoom = 10,
        TutorialRoomToInspectionRoom = 11,
        InspectionRoomToFeatureMapRoom = 12,
        FeatureMapRoomToModelDesignRoom = 13,
        ModelDesignRoomToRearCourt = 14,
        StaffRestRoom = 15,
        InspectionRoom = 16,
    }

    public enum FactoryShutterId
    {
        None = 0,
        TutorialEntrance = 1,
        TutorialExit = 2,
        ScreenEntrance = 3,
        ScreenToConvolution = 4,
        ConvolutionExit = 5,
        FeatureMapEntrance = 6,
        FeatureMapExit = 7,
        ModelDesignEntrance = 8,
        ModelDesignExit = 9,
        RearCourtEntrance = 10
    }

    // One scene-owned source of factory layout data shared by the factory components.
    public sealed class FactoryLayoutData : MonoBehaviour
    {
        [Serializable]
        public sealed class Shutter
        {
            [SerializeField] private FactoryShutterId _id;
            [SerializeField] private Transform _frame;
            [SerializeField] private float _approachDistance = 6f;

            public FactoryShutterId Id => _id;
            public Transform Frame => _frame;
            public float ApproachDistance => _approachDistance;
        }

        [Serializable]
        public sealed class Area
        {
            [SerializeField] private FactoryAreaId _id;
            [SerializeField] private LearningStageId _stage;
            [FormerlySerializedAs("_entryVolume")]
            [SerializeField] private BoxCollider _volume;
            [SerializeField] private BoxCollider[] _floorVolumes;

            public FactoryAreaId Id => _id;
            public LearningStageId? Stage => _stage == LearningStageId.None ? null : _stage;
            public BoxCollider Volume => _volume;
            public ReadOnlySpan<BoxCollider> FloorVolumes => _floorVolumes;
        }

        [Serializable]
        public sealed class StageSpace
        {
            [SerializeField] private LearningStageId _id;
            [SerializeField] private GameObject _route;
            [SerializeField] private Transform _contentAnchor;
            [SerializeField] private FactoryShutterId _entryShutter;
            [SerializeField] private FactoryShutterId _exitShutter;

            public LearningStageId Id => _id;
            public GameObject Route => _route;
            public Transform ContentAnchor => _contentAnchor;
            public FactoryShutterId EntryShutter => _entryShutter;
            // The rear exit court has no onward shutter.
            public FactoryShutterId ExitShutter => _exitShutter;
        }

        [Serializable]
        internal sealed class TeleportPoint
        {
            [SerializeField] private FactoryAreaId _source;
            [SerializeField] private Transform _arrival;
            [SerializeField] private LearningStageId _requiredCompletion;
            [SerializeField] private LearningStageId _requiredParchmentStage;

            public FactoryAreaId Source => _source;
            public Transform Arrival => _arrival;
            public LearningStageId RequiredCompletion => _requiredCompletion;
            public LearningStageId RequiredParchmentStage => _requiredParchmentStage;
        }

        private readonly FactoryAreaId[] _areaOrder =
        {
            FactoryAreaId.FrontCourtToTutorialRoom,
            FactoryAreaId.TutorialRoom,
            FactoryAreaId.TutorialRoomToInspectionRoom,
            FactoryAreaId.ScreenRoom,
            FactoryAreaId.ConvolutionRoom,
            FactoryAreaId.InspectionRoomToFeatureMapRoom,
            FactoryAreaId.FeatureMapRoom,
            FactoryAreaId.FeatureMapRoomToModelDesignRoom,
            FactoryAreaId.ModelDesignRoom,
            FactoryAreaId.ModelDesignRoomToRearCourt,
            FactoryAreaId.RearCourt
        };

        [SerializeField] private Area[] _areas;
        public ReadOnlySpan<Area> Areas => _areas;

        [SerializeField] private Shutter[] _shutters;
        public ReadOnlySpan<Shutter> Shutters => _shutters;

        public Area GetArea(FactoryAreaId id) =>
            Array.Find(_areas, area => area.Id == id);

        public bool TryGetAdjacentAreas(Area area, out Area previous, out Area next)
        {
            int index = Array.IndexOf(_areaOrder, area.Id);
            previous = null;
            next = null;
            if (index < 0) { return false; }
            if (index > 0) { previous = GetArea(_areaOrder[index - 1]); }
            if (index + 1 < _areaOrder.Length) { next = GetArea(_areaOrder[index + 1]); }
            return true;
        }

        [SerializeField] private Transform _entrance;
        [FormerlySerializedAs("_destinations")]
        [SerializeField] private StageSpace[] _stageSpaces;
        [SerializeField] private TeleportPoint[] _teleportPoints;

        public Transform Entrance => _entrance;
        public ReadOnlySpan<StageSpace> StageSpaces => _stageSpaces;
        internal ReadOnlySpan<TeleportPoint> TeleportPoints => _teleportPoints;

        public StageSpace GetStageSpace(LearningStageId id)
        {
            foreach (StageSpace space in _stageSpaces)
            {
                if (space.Id == id)
                {
                    return space;
                }
            }
            return null;
        }
    }
}
