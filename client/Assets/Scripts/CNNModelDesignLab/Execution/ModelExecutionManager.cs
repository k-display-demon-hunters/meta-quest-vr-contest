using System;
using System.Threading.Tasks;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Execution
{
    public sealed class ModelExecutionManager : MonoBehaviour
    {
        [SerializeField] private ModelExecutionController _controller;

        public ModelExecutionState State => _controller.State;
        public ModelExecutionDefinition SubmittedModel => _controller.SubmittedModel;
        public CnnTrainingOptions TrainingOptions => _controller.TrainingOptions;
        public bool IsBusy => _controller.IsBusy;
        public bool CanCancel => _controller.CanCancel;
        public bool HasTrainedModel => _controller.HasTrainedModel;

        public event Action OnStateChanged
        {
            add => _controller.OnStateChanged += value;
            remove => _controller.OnStateChanged -= value;
        }

        public event Action<TrainingProgress> OnTrainingProgress
        {
            add => _controller.OnTrainingProgress += value;
            remove => _controller.OnTrainingProgress -= value;
        }

        public event Action<TrainingResult> OnTrainingCompleted
        {
            add => _controller.OnTrainingCompleted += value;
            remove => _controller.OnTrainingCompleted -= value;
        }

        public event Action OnTrainingCancelled
        {
            add => _controller.OnTrainingCancelled += value;
            remove => _controller.OnTrainingCancelled -= value;
        }

        public event Action<InferenceResult> OnInferenceCompleted
        {
            add => _controller.OnInferenceCompleted += value;
            remove => _controller.OnInferenceCompleted -= value;
        }

        public event Action<string> OnError
        {
            add => _controller.OnError += value;
            remove => _controller.OnError -= value;
        }

        public Task StartTrainingAsync(ModelExecutionDefinition definition)
        {
            return _controller.StartTrainingAsync(definition);
        }

        public Task CancelTrainingAsync()
        {
            return _controller.CancelTrainingAsync();
        }

        public Task InferAsync(string imageId)
        {
            return _controller.InferAsync(imageId);
        }

        public void CloseSession()
        {
            _controller.CloseSession();
        }
    }
}
