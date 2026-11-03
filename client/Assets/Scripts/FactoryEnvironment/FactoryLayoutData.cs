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

    // One scene-owned source of factory layout data shared by the factory components.
    public sealed class FactoryLayoutData : MonoBehaviour
    {
        public sealed class Area
        {
            public FactoryAreaId Id { get; }
            public LearningStageId? Stage { get; }

            public Area(FactoryAreaId id, LearningStageId? stage)
            {
                Id = id;
                Stage = stage;
            }
        }

        [Serializable]
        public sealed class StageSpace
        {
            [SerializeField] private LearningStageId _id;
            [SerializeField] private GameObject _route;
            [SerializeField] private Transform _contentAnchor;
            [SerializeField] private FactoryShutterController _entryDoor;
            [SerializeField] private FactoryShutterController _exitDoor;

            public LearningStageId Id => _id;
            public GameObject Route => _route;
            public Transform ContentAnchor => _contentAnchor;
            internal FactoryShutterController EntryDoor => _entryDoor;
            // The rear exit court has no onward shutter.
            internal FactoryShutterController ExitDoor => _exitDoor;
        }

        private readonly Area[] _areaOrder =
        {
            new Area(FactoryAreaId.FrontCourtToTutorialRoom, null),
            new Area(FactoryAreaId.TutorialRoom, LearningStageId.Tutorial),
            new Area(FactoryAreaId.TutorialRoomToInspectionRoom, null),
            new Area(FactoryAreaId.ScreenRoom, LearningStageId.ScreenLab),
            new Area(FactoryAreaId.ConvolutionRoom, LearningStageId.ConvolutionLab),
            new Area(FactoryAreaId.InspectionRoomToFeatureMapRoom, null),
            new Area(FactoryAreaId.FeatureMapRoom, LearningStageId.FeatureMap),
            new Area(FactoryAreaId.FeatureMapRoomToModelDesignRoom, null),
            new Area(FactoryAreaId.ModelDesignRoom, LearningStageId.ModelDesign),
            new Area(FactoryAreaId.ModelDesignRoomToRearCourt, null),
            new Area(FactoryAreaId.RearCourt, LearningStageId.Exit)
        };

        public ReadOnlySpan<Area> AreaOrder => _areaOrder;

        public int GetAreaIndex(FactoryAreaId id) =>
            Array.FindIndex(_areaOrder, area => area.Id == id);

        [SerializeField] private Transform _entrance;
        [FormerlySerializedAs("_destinations")]
        [SerializeField] private StageSpace[] _stageSpaces;

        public Transform Entrance => _entrance;
        public ReadOnlySpan<StageSpace> StageSpaces => _stageSpaces;

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
