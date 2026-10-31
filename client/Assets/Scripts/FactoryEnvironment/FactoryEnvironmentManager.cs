using Tablet;
using UnityEngine;

namespace FactoryEnvironment
{
    // Connects the scene's learning flow to the tablet on the persistent XR rig.
    public sealed class FactoryEnvironmentManager : MonoBehaviour
    {
        [SerializeField] private FactoryLearningFlowManager _learningFlow;
        private TabletManager _tablet;

        private void Awake()
        {
            // The surviving rig can belong to the previous scene, so resolve its live tablet.
            _tablet = FindFirstObjectByType<TabletManager>();
        }

        private void OnEnable()
        {
            _tablet.StageMoveRequested += _learningFlow.RequestStageMove;
            _learningFlow.LearningStateChanged += _tablet.SetLearningState;
            _learningFlow.TravelStarted += _tablet.ShowTravelStarted;
            _learningFlow.TravelFailed += _tablet.ShowTravelFailed;
            _learningFlow.TravelCompleted += _tablet.ShowTravelCompleted;
            _tablet.SetLearningState(_learningFlow.CurrentLesson, _learningFlow.CompletedStages,
                _learningFlow.CanTravel);
        }

        private void OnDisable()
        {
            _learningFlow.LearningStateChanged -= _tablet.SetLearningState;
            _learningFlow.TravelStarted -= _tablet.ShowTravelStarted;
            _learningFlow.TravelFailed -= _tablet.ShowTravelFailed;
            _learningFlow.TravelCompleted -= _tablet.ShowTravelCompleted;
            // The persistent rig may already be destroyed when the application exits.
            if (_tablet != null)
            {
                _tablet.StageMoveRequested -= _learningFlow.RequestStageMove;
                _tablet.SetLearningState(_learningFlow.CurrentLesson, _learningFlow.CompletedStages, false);
            }
        }
    }
}
