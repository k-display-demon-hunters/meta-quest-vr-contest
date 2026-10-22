using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KdyLearning
{
    // Session data belongs to the existing persistent player, not a global singleton.
    public sealed class KdyLessonProgressController : MonoBehaviour
    {
        private readonly bool[] _collected = new bool[5];
        public int Tab { get; set; }
        public KdyNetworkConfiguration Network { get; } = new KdyNetworkConfiguration();
        public KdyNetworkConfiguration LastTrainedNetwork { get; } = new KdyNetworkConfiguration();
        public float TargetImprovement => 60f;
        public int TrainingAttempts { get; private set; }
        public int FailureCount { get; private set; }
        public float LastImprovement { get; private set; }
        public bool HasTrainingResult { get; private set; }
        public bool HasTrainingScroll { get; private set; }
        public bool HasInspectedFactorySign { get; private set; }
        public bool IsGameComplete { get; private set; }
        public float FactorySignImprovement { get; private set; }

        public void MarkFactorySignInspected(float improvement)
        {
            if (!HasTrainingResult || !LastTrainedNetwork.IsComplete || LastImprovement < TargetImprovement)
            {
                return;
            }
            HasInspectedFactorySign = true;
            FactorySignImprovement = improvement;
        }

        public void CompleteExperience()
        {
            if (HasInspectedFactorySign)
            {
                IsGameComplete = true;
            }
        }

        public void RecordTrainingResult(float improvement)
        {
            RecordTrainingResult(improvement, Network);
        }

        public void RecordTrainingResult(float improvement, KdyNetworkConfiguration trainedConfiguration)
        {
            if (trainedConfiguration == null || !trainedConfiguration.IsComplete)
            {
                return;
            }
            HasInspectedFactorySign = false;
            IsGameComplete = false;
            FactorySignImprovement = 0f;
            TrainingAttempts++;
            LastImprovement = improvement;
            HasTrainingResult = true;
            HasTrainingScroll = true;
            LastTrainedNetwork.CopyFrom(trainedConfiguration);
            if (improvement < TargetImprovement)
            {
                FailureCount++;
            }
        }

        public int ScrollCount
        {
            get
            {
                int count = 0;
                foreach (bool collected in _collected)
                {
                    if (collected)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public int NextScroll
        {
            get
            {
                for (int i = 0; i < _collected.Length; i++)
                {
                    if (!_collected[i])
                    {
                        return i;
                    }
                }
                return _collected.Length;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public bool HasScroll(int index)
        {
            return index >= 0 && index < _collected.Length && _collected[index];
        }

        public void Collect(int index)
        {
            if (index >= 0 && index < _collected.Length)
            {
                _collected[index] = true;
            }
        }

        public void ClearProgress()
        {
            Array.Clear(_collected, 0, _collected.Length);
            Tab = 0;
            Network.Clear();
            LastTrainedNetwork.Clear();
            TrainingAttempts = 0;
            FailureCount = 0;
            LastImprovement = 0f;
            HasTrainingResult = false;
            HasTrainingScroll = false;
            HasInspectedFactorySign = false;
            IsGameComplete = false;
            FactorySignImprovement = 0f;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.StartsWith("kdy_scene", StringComparison.Ordinal))
            {
                Destroy(this);
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }
}
