using System;
using System.Collections.Generic;
using ModelDesignScene.Dataset;
using ModelDesignScene.Model;
using ModelDesignScene.Execution;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.ControlPanel
{
    public sealed class ModelControlPanelManager : MonoBehaviour
    {
        [SerializeField] private ModelControlPanelController _controller;

        public event Action OnStartRequested
        {
            add => _controller.OnStartRequested += value;
            remove => _controller.OnStartRequested -= value;
        }

        public event Action OnStopRequested
        {
            add => _controller.OnStopRequested += value;
            remove => _controller.OnStopRequested -= value;
        }

        public event Action<string> OnInferenceRequested
        {
            add => _controller.OnInferenceRequested += value;
            remove => _controller.OnInferenceRequested -= value;
        }

        public event Action OnExampleRequested
        {
            add => _controller.OnExampleRequested += value;
            remove => _controller.OnExampleRequested -= value;
        }

        public void SetExampleAvailable(bool available) => _controller.SetExampleAvailable(available);

        public event Action OnRealityRequested
        {
            add => _controller.OnRealityRequested += value;
            remove => _controller.OnRealityRequested -= value;
        }

        public void SetRealityAvailable(bool available) => _controller.SetRealityAvailable(available);

        internal void SetTrainingResultPresentation(bool showing, bool highlightReality) =>
            _controller.SetTrainingResultPresentation(showing, highlightReality);

        public void Initialize(ModelDatasetManager dataset)
        {
            _controller.SetSamples(dataset.Samples);
            if (dataset.PreparationError != null)
            {
                _controller.ShowDatasetPreparationFailure(dataset.PreparationError);
            }
        }

        public void ShowModelValidation(IReadOnlyList<ValidationIssue> issues,
            IReadOnlyList<LayerDefinition> slotLayers, bool isDragging, bool hasValidationResult, bool focus = false)
        {
            _controller.ShowModelValidation(issues, slotLayers, isDragging, hasValidationResult, focus);
        }

        internal void ShowAssemblyOutputSizes(IReadOnlyList<string> sizes) => _controller.ShowAssemblyOutputSizes(sizes);

        public void ShowPlacementGuide(PlacementGuideState state, int slotIndex) =>
            _controller.ShowPlacementGuide(state, slotIndex);

        public void ResetTraining()
        {
            _controller.ResetTraining();
        }

        public void ResetSessionUI()
        {
            _controller.ResetSessionUI();
        }

        public void ShowProgress(TrainingProgress progress)
        {
            _controller.ShowProgress(progress);
        }

        public void ShowCompleted(TrainingResult result)
        {
            _controller.ShowCompleted(result);
        }

        public void ShowInference(InferenceResult result)
        {
            _controller.ShowInference(result);
        }

        public void ShowState(ModelExecutionState state, bool hasTrainedModel, bool canInfer, bool canCancel)
        {
            _controller.ShowState(state, hasTrainedModel, canInfer, canCancel);
        }

        public void ShowError(string message) => _controller.ShowError(message);

        public void ShowRealityEntryFailure() => _controller.ShowRealityEntryFailure();

        public void ShowCancelled()
        {
            _controller.ShowCancelled();
        }

        public void SetModelChanged(bool changed)
        {
            _controller.SetModelChanged(changed);
        }
    }
}
