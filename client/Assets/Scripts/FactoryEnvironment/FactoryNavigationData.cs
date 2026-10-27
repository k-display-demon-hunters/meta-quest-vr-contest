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
        Exit = 4
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

            public FactoryDestinationId Id => _id;
            public GameObject Route => _route;
            public FactoryAreaEntryController Area => _area;
            public string SceneName => _sceneName;
            public Transform ContentAnchor => _contentAnchor;

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
        [SerializeField] private string _tutorialSceneName = "FactoryTutorial";
        [SerializeField] private DestinationData[] _destinations;

        public Transform Entrance => _entrance;
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
