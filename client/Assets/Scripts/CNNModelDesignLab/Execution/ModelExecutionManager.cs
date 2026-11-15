using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ModelDesignScene.Model;
using ModelDesignScene.Dataset;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Execution
{
    public sealed class ModelExecutionManager : MonoBehaviour
    {
        private ModelExecutionController _controller;

        private ModelDatasetManager _dataset;
        private CnnEngineManager _engine;
        private CnnTrainingOptions _options;

        public ModelExecutionState State => _controller.State;
        public ModelExecutionDefinition SubmittedModel => _controller.SubmittedModel;
        public bool IsBusy => _controller.IsBusy;
        public bool CanCancel => _controller.CanCancel;
        public bool HasTrainedModel => _engine != null && _engine.HasTrainedModel;

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

        private void Awake()
        {
            _controller = GetComponent<ModelExecutionController>();
            _controller.OnSessionCloseRequested += CloseSession;
        }

        public void Initialize(ModelDatasetManager dataset)
        {
            _dataset = dataset;
            _options = CnnTrainingSettings.CreateOptions();
        }

        public event Action<IReadOnlyList<ValidationIssue>> OnValidationFailed;

        public bool MatchesSubmittedModel(ModelDefinition model)
        {
            return ModelExecutionDefinitionBuilder.Matches(model, SubmittedModel);
        }

        public bool TryValidateConfiguration(IReadOnlyList<LayerDefinition> slotLayers,
            LayerDefinition output, out ValidationIssue issue)
        {
            ModelExecutionDefinition definition = ModelExecutionDefinitionBuilder.Build(
                slotLayers, output, out int?[] slotIndices);
            return ModelExecutionValidator.TryValidateDefinition(definition, slotIndices, out issue);
        }

        public Task StartTrainingAsync(ModelDefinition model)
        {
            if (!isActiveAndEnabled || !_controller.isActiveAndEnabled || IsBusy)
            {
                return Task.CompletedTask;
            }
            if (_dataset.PreparationError != null)
            {
                _controller.RejectTraining(_dataset.PreparationError);
                return Task.CompletedTask;
            }
            ModelExecutionDefinition definition = ModelExecutionDefinitionBuilder.Build(model, out int?[] slots);
            TrainingData data = _dataset.TrainingData;
            if (!ModelExecutionValidator.TryPrepareTraining(definition, data, _options, slots,
                out CnnTrainingRequest request, out ValidationIssue issue))
            {
                OnValidationFailed?.Invoke(new[] { issue });
                return Task.CompletedTask;
            }
            if (_engine == null)
            {
                _engine = new CnnEngineManager();
            }
            CnnEngineManager engine = _engine;
            return _controller.StartTrainingAsync(definition, progress => engine.TrainAsync(request, progress));
        }

        public Task CancelTrainingAsync()
        {
            CnnEngineManager engine = _engine;
            if (isActiveAndEnabled && _controller.RequestCancellation())
            {
                engine.Cancel();
            }
            return Task.CompletedTask;
        }

        public Task InferAsync(string imageId)
        {
            if (!isActiveAndEnabled || !_controller.isActiveAndEnabled || IsBusy)
            {
                return Task.CompletedTask;
            }
            if (!HasTrainedModel)
            {
                _controller.RejectInference("이미지를 예측하려면 먼저 모델 학습을 완료하세요.");
                return Task.CompletedTask;
            }
            if (!_dataset.TryGetInferenceInput(imageId, out float[] input))
            {
                _controller.RejectInference("내장 데이터에 해당 샘플이 없습니다. 이미지 이름을 확인하세요.");
                return Task.CompletedTask;
            }
            CnnEngineManager engine = _engine;
            return _controller.InferAsync(() => engine.InferAsync(input, imageId));
        }

        public async Task<InferenceResult> InferExternalAsync(float[] input, string requestId)
        {
            if (!isActiveAndEnabled || IsBusy || !HasTrainedModel)
                throw new InvalidOperationException("The trained model is not available for inference.");
            InferenceResult result = null;
            Exception failure = null;
            CnnEngineManager engine = _engine;
            await _controller.InferAsync(async () =>
            {
                try { result = await engine.InferAsync(input, requestId); return result; }
                catch (Exception exception) { failure = exception; throw; }
            });
            if (failure != null) { throw failure; }
            return result;
        }

        public void CloseSession()
        {
            CnnEngineManager engine = _engine;
            _engine = null;
            _controller.CloseSession();
            engine?.Dispose();
        }

        private void OnDisable()
        {
            CloseSession();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnSessionCloseRequested -= CloseSession;
            }
        }
    }
}
