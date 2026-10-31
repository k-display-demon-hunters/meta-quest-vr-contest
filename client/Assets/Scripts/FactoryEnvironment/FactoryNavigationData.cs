using System;
using Learning;
using UnityEngine;

namespace FactoryEnvironment
{
    // One scene-owned source of destination data shared by the factory components.
    public sealed class FactoryNavigationData : MonoBehaviour
    {
        [Serializable]
        public sealed class DestinationData
        {
            [SerializeField] private RoomId _id;
            [SerializeField] private GameObject _route;
            [SerializeField] private FactoryAreaEntryController _area;
            [SerializeField] private Transform _contentAnchor;
            [SerializeField] private FactoryShutterController _entryDoor;
            [SerializeField] private FactoryShutterController _exitDoor;

            public RoomId Id => _id;
            public GameObject Route => _route;
            public FactoryAreaEntryController Area => _area;
            public Transform ContentAnchor => _contentAnchor;
            internal FactoryShutterController EntryDoor => _entryDoor;
            // The rear exit court has no onward shutter.
            internal FactoryShutterController ExitDoor => _exitDoor;
        }

        [SerializeField] private FactoryShutterController _convolutionEntranceDoor;
        public FactoryShutterController ConvolutionEntranceDoor => _convolutionEntranceDoor;
        [SerializeField] private Transform _entrance;
        [SerializeField] private Transform _tutorialStart;
        [SerializeField] private DestinationData[] _destinations;

        public Transform Entrance => _entrance;
        public Transform TutorialStart => _tutorialStart;
        public ReadOnlySpan<DestinationData> Destinations => _destinations;

        public DestinationData Get(RoomId id)
        {
            foreach (DestinationData destination in _destinations)
            {
                if (destination.Id == id)
                {
                    return destination;
                }
            }
            return null;
        }
    }
}
