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

    public enum ModelExecutionFailure
    {
        Training,
        SampleInference,
        DatasetPreparation
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
        internal event Action<ModelExecutionFailure> OnError;

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
            Func<Action<TrainingProgress>, Task<TrainingResult>> startTraining, Action cancelTraining)
        {
            ConcurrentQueue<Action> pending = _pending;
            int generation = _sessionGeneration;
            int operation = ++_operationGeneration;
            ModelExecutionDefinition previousModel = SubmittedModel;
            bool accepted = false;
            SubmittedModel = definition;
            try
            {
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

                Task<TrainingResult> training = startTraining(
                    progress => pending.Enqueue(() =>
                    {
                        if (IsCurrent(generation, operation) && State != ModelExecutionState.CancellingTraining)
                        {
                            OnTrainingProgress?.Invoke(progress);
                        }
                    }));
                accepted = true;
                try
                {
                    SetState(ModelExecutionState.Training);
                }
                catch (Exception)
                {
                    // The worker owns the engine until its task ends, even if Unity UI setup fails.
                    SetRecoveryState(ModelExecutionState.CancellingTraining);
                    cancelTraining();
                    try
                    {
                        await training.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception workerException) { Debug.LogError(workerException); }
                    throw;
                }
                TrainingResult result = await training.ConfigureAwait(false);
                pending.Enqueue(() =>
                {
                    if (!IsCurrent(generation, operation))
                    {
                        return;
                    }
                    SetRecoveryState(ModelExecutionState.Completed);
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
                Debug.LogError($"[ERR-TRAIN] {exception}");
                pending.Enqueue(() =>
                {
                    if (!accepted && IsCurrent(generation, operation))
                    {
                        SubmittedModel = previousModel;
                    }
                    ReportFailure(generation, operation, ModelExecutionFailure.Training);
                });
            }
        }

        internal bool RequestCancellation()
        {
            if (!isActiveAndEnabled || !CanCancel)
            {
                return false;
            }
            SetRecoveryState(ModelExecutionState.CancellingTraining);
            return true;
        }

        internal async Task<InferenceResult> InferAsync(Func<Task<InferenceResult>> startInference,
            bool propagateFailure = false)
        {
            ConcurrentQueue<Action> pending = _pending;
            int generation = _sessionGeneration;
            int operation = ++_operationGeneration;
            try
            {
                SetState(ModelExecutionState.Inferring);
                if (!IsCurrent(generation, operation))
                {
                    if (propagateFailure) { throw new OperationCanceledException(); }
                    return null;
                }

                InferenceResult result = await startInference().ConfigureAwait(false);
                pending.Enqueue(() =>
                {
                    if (!IsCurrent(generation, operation))
                    {
                        return;
                    }
                    SetRecoveryState(ModelExecutionState.Completed);
                    if (IsCurrent(generation, operation))
                    {
                        OnInferenceCompleted?.Invoke(result);
                    }
                });
                return result;
            }
            catch (OperationCanceledException)
            {
                pending.Enqueue(() => CompleteCancellation(generation, operation));
                if (propagateFailure) { throw; }
                return null;
            }
            catch (Exception exception)
            {
                if (!propagateFailure) { Debug.LogError($"[ERR-INFER] {exception}"); }
                pending.Enqueue(() => ReportFailure(generation, operation,
                    propagateFailure ? (ModelExecutionFailure?)null : ModelExecutionFailure.SampleInference));
                if (propagateFailure) { throw; }
                return null;
            }
        }

        internal void CloseSession()
        {
            _sessionGeneration++;
            _operationGeneration++;
            // The old worker retains its own queue until it exits, never the next session's queue.
            _pending = new ConcurrentQueue<Action>();
            SubmittedModel = null;
            SetRecoveryState(ModelExecutionState.Idle);
        }

        internal void RejectTrainingForDataset()
        {
            SetRecoveryState(ModelExecutionState.Failed);
            OnError?.Invoke(ModelExecutionFailure.DatasetPreparation);
        }

        internal void RejectInference(string message)
        {
            // Restore a locally disabled sample list without replaying an active operation's start.
            if (!IsBusy) { NotifyRecoveryState(); }
            Debug.LogError($"[ERR-INFER] {message}");
            OnError?.Invoke(ModelExecutionFailure.SampleInference);
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
            SetRecoveryState(ModelExecutionState.Cancelled);
            if (IsCurrent(generation, operation))
            {
                OnTrainingCancelled?.Invoke();
            }
        }

        private void ReportFailure(int generation, int operation, ModelExecutionFailure? failure)
        {
            if (!IsCurrent(generation, operation))
            {
                return;
            }
            SetRecoveryState(ModelExecutionState.Failed);
            if (IsCurrent(generation, operation) && failure.HasValue)
            {
                OnError?.Invoke(failure.Value);
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

        private void SetRecoveryState(ModelExecutionState state)
        {
            if (State == state) { return; }
            State = state;
            NotifyRecoveryState();
        }

        private void NotifyRecoveryState()
        {
            try { OnStateChanged?.Invoke(); }
            catch (Exception exception) { Debug.LogError(exception); }
        }

        private void OnDisable()
        {
            OnSessionCloseRequested?.Invoke();
        }
    }
}
