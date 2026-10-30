using System;
using CNNFeatureMaps;
using ModelDesignScene;
using UI;
using UnityEngine;

namespace FactoryEnvironment
{
    public enum FactoryDestinationId
    {
        None = 0,
        PixelLab = 1,
        FeatureMapLab = 2,
        ModelDesignLab = 3,
        Exit = 4,
        Tutorial = 5
    }

    // One scene-owned source of destination data shared by the factory components.
    public sealed class FactoryNavigationData : MonoBehaviour
    {
        [Serializable]
        public sealed class DestinationData
        {
            [SerializeField] private FactoryDestinationId _id;
            [SerializeField] private GameObject _route;
            [SerializeField] private FactoryAreaEntryController _area;
            [SerializeField] private string _sceneName;
            [SerializeField] private Transform _contentAnchor;
            [SerializeField] private FactoryShutterController _entryDoor;
            [SerializeField] private FactoryShutterController _exitDoor;

            public FactoryDestinationId Id => _id;
            public GameObject Route => _route;
            public FactoryAreaEntryController Area => _area;
            public string SceneName => _sceneName;
            public Transform ContentAnchor => _contentAnchor;
            internal FactoryShutterController EntryDoor => _entryDoor;
            // The rear exit court has no onward shutter.
            internal FactoryShutterController ExitDoor => _exitDoor;

            public GameObject Content { get; internal set; }
            public CNNPixelLabManager Pixels { get; internal set; }
            public CNNFeatureMapLabManager Maps { get; internal set; }
            public CNNModelDesignLabManager Model { get; internal set; }

            public void ClearContent()
            {
                Content = null;
                Pixels = null;
                Maps = null;
                Model = null;
            }
        }

        [SerializeField] private Transform _entrance;
        [SerializeField] private Transform _tutorialStart;
        [SerializeField] private string _tutorialSceneName = "FactoryTutorial";
        [SerializeField] private DestinationData[] _destinations;

        public Transform Entrance => _entrance;
        public Transform TutorialStart => _tutorialStart;
        public string TutorialSceneName => _tutorialSceneName;
        public ReadOnlySpan<DestinationData> Destinations => _destinations;

        public DestinationData Get(FactoryDestinationId id)
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
