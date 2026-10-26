using System;
using System.Collections.Generic;
using ModelDesignScene.Editing;
using ModelDesignScene.Execution;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Results
{
    public sealed class ModelResultsManager : MonoBehaviour
    {
        [SerializeField] private ModelResultsController _controller;

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

        public void ShowModelValidation(IReadOnlyList<ValidationIssue> issues,
            IReadOnlyList<LayerDefinition> slotLayers, bool isDragging, bool focus = false)
        {
            _controller.ShowModelValidation(issues, slotLayers, isDragging, focus);
        }

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

        public void ShowError(string message)
        {
            _controller.ShowError(message);
        }

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
