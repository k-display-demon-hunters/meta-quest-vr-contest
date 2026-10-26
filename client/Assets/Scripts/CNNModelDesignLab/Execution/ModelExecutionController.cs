using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Execution
{
    public enum ModelExecutionState
    {
        Idle,
        StartingTraining,
        Training,
        CancellingTraining,
        Completed,
        Inferring,
        Cancelled,
        Failed
    }

    public sealed class ModelExecutionController : MonoBehaviour
    {
        private ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private int _sessionGeneration;
        private int _operationGeneration;

        internal ModelExecutionState State { get; private set; }
        internal ModelExecutionDefinition SubmittedModel { get; private set; }
        internal bool IsBusy => State == ModelExecutionState.StartingTraining
                              || State == ModelExecutionState.Training
                              || State == ModelExecutionState.CancellingTraining
                              || State == ModelExecutionState.Inferring;
        internal bool CanCancel => State == ModelExecutionState.StartingTraining
                                 || State == ModelExecutionState.Training;

        internal event Action OnStateChanged;
        internal event Action<TrainingProgress> OnTrainingProgress;
        internal event Action<TrainingResult> OnTrainingCompleted;
        internal event Action OnTrainingCancelled;
        internal event Action<InferenceResult> OnInferenceCompleted;
        internal event Action<string> OnError;

        internal event Action OnSessionCloseRequested;

        private void Update()
        {
            // Worker callbacks only enqueue immutable results. Unity events run on this thread.
            while (_pending.TryDequeue(out Action notification))
            {
                notification();
            }
        }

        internal async Task StartTrainingAsync(ModelExecutionDefinition definition,
            Func<Action<TrainingProgress>, Task<TrainingResult>> startTraining)
        {
            ConcurrentQueue<Action> pending = _pending;
            int generation = _sessionGeneration;
            int operation = ++_operationGeneration;
            ModelExecutionDefinition previousModel = SubmittedModel;
            bool accepted = false;
            SubmittedModel = definition;
            SetState(ModelExecutionState.StartingTraining);
            if (!IsCurrent(generation, operation))
            {
                return;
            }
            if (State == ModelExecutionState.CancellingTraining)
            {
                // A state listener can cancel before the worker has been scheduled.
                SubmittedModel = previousModel;
                CompleteCancellation(generation, operation);
                return;
            }

            try
            {
                Task<TrainingResult> training = startTraining(
                    progress => pending.Enqueue(() =>
                    {
                        if (IsCurrent(generation, operation) && State != ModelExecutionState.CancellingTraining)
                        {
                            OnTrainingProgress?.Invoke(progress);
                        }
                    }));
                accepted = true;
                SetState(ModelExecutionState.Training);
                TrainingResult result = await training.ConfigureAwait(false);
                pending.Enqueue(() =>
                {
                    if (!IsCurrent(generation, operation))
                    {
                        return;
                    }
                    SetState(ModelExecutionState.Completed);
                    if (IsCurrent(generation, operation))
                    {
                        OnTrainingCompleted?.Invoke(result);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                pending.Enqueue(() => CompleteCancellation(generation, operation));
            }
            catch (Exception exception)
            {
                pending.Enqueue(() =>
                {
                    if (!accepted && IsCurrent(generation, operation))
                    {
                        SubmittedModel = previousModel;
                    }
                    ReportFailure(generation, operation,
                        "학습 또는 평가를 완료하지 못했습니다. " + exception.Message);
                });
            }
        }

        internal bool RequestCancellation()
        {
            if (!isActiveAndEnabled || !CanCancel)
            {
                return false;
            }
            SetState(ModelExecutionState.CancellingTraining);
            return true;
        }

        internal async Task InferAsync(Func<Task<InferenceResult>> startInference)
        {
            ConcurrentQueue<Action> pending = _pending;
            int generation = _sessionGeneration;
            int operation = ++_operationGeneration;
            SetState(ModelExecutionState.Inferring);
            if (!IsCurrent(generation, operation))
            {
                return;
            }
            try
            {
                InferenceResult result = await startInference().ConfigureAwait(false);
                pending.Enqueue(() =>
                {
                    if (!IsCurrent(generation, operation))
                    {
                        return;
                    }
                    SetState(ModelExecutionState.Completed);
                    if (IsCurrent(generation, operation))
                    {
                        OnInferenceCompleted?.Invoke(result);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                pending.Enqueue(() => CompleteCancellation(generation, operation));
            }
            catch (Exception exception)
            {
                pending.Enqueue(() => ReportFailure(generation, operation,
                    "이미지를 예측하지 못했습니다. " + exception.Message));
            }
        }

        internal void CloseSession()
        {
            _sessionGeneration++;
            _operationGeneration++;
            // The old worker retains its own queue until it exits, never the next session's queue.
            _pending = new ConcurrentQueue<Action>();
            SubmittedModel = null;
            SetState(ModelExecutionState.Idle);
        }

        internal void RejectTraining(string message)
        {
            SetState(ModelExecutionState.Failed);
            OnError?.Invoke(message);
        }

        internal void RejectInference(string message)
        {
            // Release the sample UI that was disabled while submitting the request.
            OnStateChanged?.Invoke();
            OnError?.Invoke(message);
        }

        private bool IsCurrent(int generation, int operation)
        {
            return generation == _sessionGeneration && operation == _operationGeneration
                && this != null && isActiveAndEnabled;
        }

        private void CompleteCancellation(int generation, int operation)
        {
            if (!IsCurrent(generation, operation))
            {
                return;
            }
            SetState(ModelExecutionState.Cancelled);
            if (IsCurrent(generation, operation))
            {
                OnTrainingCancelled?.Invoke();
            }
        }

        private void ReportFailure(int generation, int operation, string message)
        {
            if (!IsCurrent(generation, operation))
            {
                return;
            }
            SetState(ModelExecutionState.Failed);
            if (IsCurrent(generation, operation))
            {
                OnError?.Invoke(message);
            }
        }

        private void SetState(ModelExecutionState state)
        {
            if (State == state)
            {
                return;
            }
            State = state;
            OnStateChanged?.Invoke();
        }

        private void OnDisable()
        {
            OnSessionCloseRequested?.Invoke();
        }
    }
}
