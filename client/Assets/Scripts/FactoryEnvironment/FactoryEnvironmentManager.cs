using System;
using Learning;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace FactoryEnvironment
{
    // Coordinates the current stage and travel.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [FormerlySerializedAs("_navigationData")]
        [SerializeField] private FactoryLayoutData _layoutData;
        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private LearningSessionManager _session;
        [SerializeField] private FactoryPlayerController _player;
        private LearningStageId _travelStage;
        private bool _tutorialAvailable;

        public LearningStageId CurrentStage { get; private set; }
        public event Action<LearningStageId> StageChanged;
        public event Action TravelStarted;
        public event Action<LearningStageId, bool> TravelFinished;
        public bool CanTravel => !_sceneLoader.IsBusy
            && (CurrentStage != LearningStageId.Tutorial || _tutorialAvailable);

        private void OnEnable()
        {
            _session.StageAvailabilityChanged += HandleStageAvailabilityChanged;
            _areaController.AreaChanged += HandleAreaChanged;
            _sceneLoader.StateChanged += HandleLoadingStateChanged;
            _sceneLoader.LoadFailed += HandleLoadFailed;
        }

        private void OnDisable()
        {
            _session.StageAvailabilityChanged -= HandleStageAvailabilityChanged;
            _areaController.AreaChanged -= HandleAreaChanged;
            _sceneLoader.StateChanged -= HandleLoadingStateChanged;
            _sceneLoader.LoadFailed -= HandleLoadFailed;
        }

        private void Start()
        {
            // OnEnable runs before an asynchronously loaded scene becomes available.
            SceneManager.SetActiveScene(gameObject.scene);
        }

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (!_layoutData.TryGetAdjacentAreas(area, out _, out _)) { return; }
            LearningStageId stage = area.Stage ?? LearningStageId.None;
            CurrentStage = stage;
            StageChanged?.Invoke(stage);
        }

        private void HandleLoadingStateChanged()
        {
            if (_sceneLoader.IsBusy) { return; }
            if (_travelStage != LearningStageId.None)
                FinishTravel(_travelStage, _sceneLoader.IsLoaded(_travelStage));
        }

        private void HandleStageAvailabilityChanged(LearningStageId stage, bool available)
        {
            if (stage == LearningStageId.Tutorial)
            {
                _tutorialAvailable = available;
            }
        }

        private void HandleStageMoveRequested(LearningStageId stage)
        {
            bool known = false;
            foreach (LearningStageDefinition definition in LearningStageCatalog.ProgressStages)
                if (definition.Id == stage) { known = true; break; }
            if (!known || !CanTravel) { return; }
            _travelStage = stage;
            TravelStarted?.Invoke();
            // Movement produces the same AreaChanged event as walking.
            _player.MoveToStage(stage);
            if (CurrentStage == stage && _sceneLoader.IsLoaded(stage)) { FinishTravel(stage, true); }
        }

        private void FinishTravel(LearningStageId stage, bool succeeded)
        {
            _travelStage = LearningStageId.None;
            TravelFinished?.Invoke(stage, succeeded);
        }

        private void HandleLoadFailed(LearningStageId stage, string message)
        {
            if (_travelStage == stage) { FinishTravel(stage, false); }
        }

    }
}
