using System;
using System.Collections;
using System.Collections.Generic;
using FactoryEnvironment;
using Parchment;
using Tablet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Learning
{
    // Records learning progress and updates the tablet from scene lifecycle results.
    public sealed class LearningSessionManager : MonoBehaviour
    {
        private sealed class ExperienceSubscription
        {
            public ILearningExperience Experience;
            public Action<float> ProgressChanged;
            public Action<bool> AvailabilityChanged;
        }

        [SerializeField] private FactoryEnvironmentManager _factory;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        private readonly LearningProgress _progress = new LearningProgress();
        private readonly Dictionary<LearningStageId, ExperienceSubscription> _experiences =
            new Dictionary<LearningStageId, ExperienceSubscription>();
        private TabletManager _tablet;
        private ParchmentManager _parchment;

        public event Action<LearningProgressSnapshot> StateChanged;
        public event Action<LearningStageId, bool> StageAvailabilityChanged;
        public bool CanTravel => isActiveAndEnabled && _factory.CanTravel;
        public LearningProgressSnapshot GetSnapshot() => _progress.CreateSnapshot(_factory.CurrentStage, CanTravel);

        private void Awake()
        {
            _tablet = FindFirstObjectByType<TabletManager>();
            _parchment = FindFirstObjectByType<ParchmentManager>();
        }

        private void OnEnable()
        {
            _tablet.StageMoveRequested += HandleStageMoveRequested;
            StateChanged += _tablet.SetLearningState;
            _sceneLoader.ContentLoaded += HandleContentLoaded;
            _sceneLoader.ContentUnloading += HandleContentUnloading;
            _factory.StageChanged += HandleStageChanged;
            _sceneLoader.StateChanged += PublishState;
            _factory.TravelStarted += _tablet.ShowTravelStarted;
            _factory.TravelFinished += HandleTravelFinished;
            foreach (ExperienceSubscription subscription in _experiences.Values)
            {
                subscription.Experience.ProgressChanged += subscription.ProgressChanged;
                subscription.Experience.ExitAvailabilityChanged += subscription.AvailabilityChanged;
            }
            HandleStageChanged(_factory.CurrentStage);
        }

        private void OnDisable()
        {
            _sceneLoader.ContentLoaded -= HandleContentLoaded;
            _sceneLoader.ContentUnloading -= HandleContentUnloading;
            _factory.StageChanged -= HandleStageChanged;
            _sceneLoader.StateChanged -= PublishState;
            _factory.TravelStarted -= _tablet.ShowTravelStarted;
            _factory.TravelFinished -= HandleTravelFinished;
            foreach (ExperienceSubscription subscription in _experiences.Values)
            {
                subscription.Experience.ProgressChanged -= subscription.ProgressChanged;
                subscription.Experience.ExitAvailabilityChanged -= subscription.AvailabilityChanged;
            }
            StateChanged -= _tablet.SetLearningState;
            if (_tablet != null)
            {
                _tablet.StageMoveRequested -= HandleStageMoveRequested;
                _tablet.SetLearningState(LearningProgressSnapshot.Empty);
                _tablet.ShowTravelCompleted();
            }
        }

        private void HandleContentLoaded(LearningStageId stage, Scene scene)
        {
            ILearningExperience experience = FindExperience(scene, stage);
            ExperienceSubscription subscription = new ExperienceSubscription
            {
                Experience = experience,
                ProgressChanged = value => HandleProgressChanged(stage, value),
                AvailabilityChanged = available => HandleAvailabilityChanged(stage, available)
            };
            _experiences.Add(stage, subscription);
            experience.ProgressChanged += subscription.ProgressChanged;
            experience.ExitAvailabilityChanged += subscription.AvailabilityChanged;
            HandleAvailabilityChanged(stage, experience.CanExit);
        }

        private void HandleAvailabilityChanged(LearningStageId stage, bool available)
        {
            StageAvailabilityChanged?.Invoke(stage, available);
            PublishState();
        }

        private static ILearningExperience FindExperience(Scene scene, LearningStageId stage)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component is ILearningExperience experience && experience.StageId == stage) { return experience; }
            throw new InvalidOperationException("Missing learning entry point for " + stage);
        }

        private IEnumerator HandleContentUnloading(LearningStageId stage, Scene scene)
        {
            if (_experiences.TryGetValue(stage, out ExperienceSubscription subscription))
            {
                subscription.Experience.ProgressChanged -= subscription.ProgressChanged;
                subscription.Experience.ExitAvailabilityChanged -= subscription.AvailabilityChanged;
                _experiences.Remove(stage);
                StageAvailabilityChanged?.Invoke(stage, false);
                yield return subscription.Experience.EndSession();
            }
        }

        private void HandleStageChanged(LearningStageId stage)
        {
            if (_experiences.TryGetValue(stage, out ExperienceSubscription subscription))
                _progress.RecordProgress(stage, subscription.Experience.Progress);
            PublishState();
        }

        private void HandleProgressChanged(LearningStageId stage, float progress)
        {
            if (_progress.RecordProgress(stage, progress)) { PublishState(); }
        }

        // Document grants follow the request, even when travel is rejected or loading fails.
        private void HandleStageMoveRequested(LearningStageId stage)
        {
            bool known = false;
            foreach (LearningStageDefinition definition in LearningStageCatalog.ProgressStages)
                if (definition.Id == stage) { known = true; break; }
            if (!known) { return; }
            foreach (LearningStageDefinition definition in LearningStageCatalog.ProgressStages)
            {
                _parchment.CollectDocuments(definition.ParchmentIds);
                if (definition.Id == stage) { break; }
            }
        }

        private void HandleTravelFinished(LearningStageId stage, bool succeeded)
        {
            if (succeeded) { _tablet.ShowTravelCompleted(); }
            else { _tablet.ShowTravelFailed(); }
        }

        private void PublishState() => StateChanged?.Invoke(GetSnapshot());
    }
}
