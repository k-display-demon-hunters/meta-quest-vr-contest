using System;
using System.Threading;
using System.Threading.Tasks;

namespace CNNTraining
{
    /// <summary>
    /// Reusable owner of one trained model. One operation occupies a thread-pool worker at a time.
    /// Progress runs on that worker; Unity callers must queue it to their main thread.
    /// Cancellation is cooperative and never reports partially trained weights as completed.
    /// </summary>
    public sealed class CnnTrainingSession : IDisposable
    {
        public const long MaxTrainingMultiplyAdds = 2000000000L;
        private readonly object _gate = new object();
        private CancellationTokenSource _operationCancellation;
        private CnnModel _trainedModel;
        private bool _isBusy;
        private bool _isDisposed;

        public bool IsBusy
        {
            get
            {
                lock (_gate)
                {
                    return _isBusy;
                }
            }
        }

        public bool HasTrainedModel
        {
            get
            {
                lock (_gate)
                {
                    return !_isDisposed && _trainedModel != null;
                }
            }
        }

        public Task<TrainingResult> TrainAsync(ModelExecutionDefinition definition, TrainingData data,
            CnnTrainingOptions options, Action<TrainingProgress> progress = null)
        {
            if (definition == null || data == null || options == null)
            {
                throw new ArgumentNullException("A model definition, dataset and options are required.");
            }
            long multiplyAdds = definition.CalculateMultiplyAdds(options);
            if (data.Training.InputSize != definition.InputSize)
            {
                throw new ArgumentException("Dataset shape does not match model input.");
            }
            // Forward plus weight/input gradients cost roughly three forward passes.
            // This is a safety bound, not a prediction of wall-clock duration on a headset.
            long passes = ((long)data.Training.Count * 3 + data.Validation.Count) * options.Epochs + data.Test.Count;
            if (multiplyAdds > MaxTrainingMultiplyAdds / passes)
            {
                throw new ArgumentException("Training workload is too large. Reduce the model, sample count or epochs.");
            }
            lock (_gate)
            {
                BeginOperation();
                _trainedModel = null;
                CancellationTokenSource operation = _operationCancellation;
                // Do not pass the token to Task.Run: its finally must run even when already cancelled.
                return Task.Run(() =>
                {
                    try
                    {
                        operation.Token.ThrowIfCancellationRequested();
                        CnnModel model = new CnnModel(definition, options);
                        CnnTrainer trainer = new CnnTrainer(model);
                        TrainingResult result = trainer.Train(data, options, progress, operation.Token);
                        lock (_gate)
                        {
                            operation.Token.ThrowIfCancellationRequested();
                            if (_isDisposed)
                            {
                                throw new OperationCanceledException(operation.Token);
                            }
                            _trainedModel = model;
                        }
                        return result;
                    }
                    finally
                    {
                        EndOperation(operation);
                    }
                });
            }
        }

        public Task<InferenceResult> InferAsync(float[] input, string imageId)
        {
            lock (_gate)
            {
                EnsureAvailable();
                if (_trainedModel == null)
                {
                    throw new InvalidOperationException("Complete training before inference.");
                }
                if (input == null || input.Length != _trainedModel.InputSize)
                {
                    throw new ArgumentException("Inference input shape does not match the model.");
                }
                for (int i = 0; i < input.Length; i++)
                {
                    if (float.IsNaN(input[i]) || float.IsInfinity(input[i]) || input[i] < 0 || input[i] > 1)
                    {
                        throw new ArgumentException("Inference pixels must be finite and normalized to [0,1].");
                    }
                }
                float[] snapshot = (float[])input.Clone();
                CnnModel model = _trainedModel;
                BeginOperation();
                CancellationTokenSource operation = _operationCancellation;
                return Task.Run(() =>
                {
                    try
                    {
                        model.Forward(snapshot, operation.Token);
                        double[] probabilities = new double[model.Output.Length];
                        for (int i = 0; i < probabilities.Length; i++)
                        {
                            probabilities[i] = model.Output[i];
                        }
                        operation.Token.ThrowIfCancellationRequested();
                        return new InferenceResult(imageId, probabilities);
                    }
                    finally
                    {
                        EndOperation(operation);
                    }
                });
            }
        }

        public void Cancel()
        {
            lock (_gate)
            {
                _operationCancellation?.Cancel();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
                _trainedModel = null;
                _operationCancellation?.Cancel();
                // Worker owns its arrays and CTS until finally. Do not block the Unity thread.
            }
        }

        private void EnsureAvailable()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CnnTrainingSession));
            }
            if (_isBusy)
            {
                throw new InvalidOperationException("A training or inference operation is already running.");
            }
        }

        private void BeginOperation()
        {
            EnsureAvailable();
            _operationCancellation = new CancellationTokenSource();
            _isBusy = true;
        }

        private void EndOperation(CancellationTokenSource operation)
        {
            lock (_gate)
            {
                if (ReferenceEquals(_operationCancellation, operation))
                {
                    _operationCancellation = null;
                    _isBusy = false;
                }
                operation.Dispose();
            }
        }
    }
}
