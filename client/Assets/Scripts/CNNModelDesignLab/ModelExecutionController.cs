using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CNNTraining;
using UnityEngine;

namespace ModelDesignScene
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
        [SerializeField] private CnnTrainingSettings _trainingSettings;

        private CnnTrainingSession _session;
        private MnistDataset _dataset;
        private CnnTrainingOptions _options;
        private string _preparationError;
        private ConcurrentQueue<Action> _pending = new ConcurrentQueue<Action>();
        private int _sessionGeneration;
        private int _operationGeneration;

        public ModelExecutionState State { get; private set; }
        public ModelExecutionDefinition SubmittedModel { get; private set; }
        public CnnTrainingOptions TrainingOptions => _options ?? new CnnTrainingOptions();
        public bool IsBusy => State == ModelExecutionState.StartingTraining
                              || State == ModelExecutionState.Training
                              || State == ModelExecutionState.CancellingTraining
                              || State == ModelExecutionState.Inferring;
        public bool CanCancel => State == ModelExecutionState.StartingTraining
                                 || State == ModelExecutionState.Training;
        public bool HasTrainedModel => _session != null && _session.HasTrainedModel;

        public event Action OnStateChanged;
        public event Action<TrainingProgress> OnTrainingProgress;
        public event Action<TrainingResult> OnTrainingCompleted;
        public event Action OnTrainingCancelled;
        public event Action<InferenceResult> OnInferenceCompleted;
        public event Action<string> OnError;

        private void OnEnable()
        {
            PrepareSession();
        }

        private void Update()
        {
            // Worker callbacks only enqueue immutable results. Unity events run on this thread.
            while (_pending.TryDequeue(out Action notification))
            {
                notification();
            }
        }

        public async Task StartTrainingAsync(ModelExecutionDefinition definition)
        {
            if (!isActiveAndEnabled || IsBusy)
            {
                return;
            }
            if (_session == null)
            {
                PrepareSession();
            }
            if (_preparationError != null || definition == null)
            {
                SetState(ModelExecutionState.Failed);
                OnError?.Invoke(_preparationError ?? "학습할 모델이 없습니다.");
                return;
            }

            CnnTrainingSession session = _session;
            ConcurrentQueue<Action> pending = _pending;
            int generation = _sessionGeneration;
            int operation = ++_operationGeneration;
            ModelExecutionDefinition previousModel = SubmittedModel;
            bool accepted = false;
            SubmittedModel = definition;
            SetState(ModelExecutionState.StartingTraining);
            if (!IsCurrent(session, generation, operation))
            {
                return;
            }
            if (State == ModelExecutionState.CancellingTraining)
            {
                // A state listener can cancel before the worker has been scheduled.
                SubmittedModel = previousModel;
                CompleteCancellation(session, generation, operation);
                return;
            }

            try
            {
                Task<TrainingResult> training = session.TrainAsync(definition, _dataset.Data, _options,
                    progress => pending.Enqueue(() =>
                    {
                        if (IsCurrent(session, generation, operation) && State != ModelExecutionState.CancellingTraining)
                        {
                            OnTrainingProgress?.Invoke(progress);
                        }
                    }));
                accepted = true;
                SetState(ModelExecutionState.Training);
                TrainingResult result = await training.ConfigureAwait(false);
                pending.Enqueue(() =>
                {
                    if (!IsCurrent(session, generation, operation))
                    {
                        return;
                    }
                    SetState(ModelExecutionState.Completed);
                    if (IsCurrent(session, generation, operation) && HasTrainedModel)
                    {
                        OnTrainingCompleted?.Invoke(result);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                pending.Enqueue(() => CompleteCancellation(session, generation, operation));
            }
            catch (Exception exception)
            {
                pending.Enqueue(() =>
                {
                    if (!accepted && IsCurrent(session, generation, operation))
                    {
                        SubmittedModel = previousModel;
                    }
                    ReportFailure(session, generation, operation,
                        "학습 또는 평가를 완료하지 못했습니다. " + exception.Message);
                });
            }
        }

        public Task CancelTrainingAsync()
        {
            if (isActiveAndEnabled && CanCancel)
            {
                CnnTrainingSession session = _session;
                SetState(ModelExecutionState.CancellingTraining);
                // State listeners may close this session and create another one.
                session?.Cancel();
            }
            // Completion is reported only after the worker acknowledges cancellation.
            return Task.CompletedTask;
        }

        public async Task InferAsync(string imageId)
        {
            if (!isActiveAndEnabled || IsBusy)
            {
                return;
            }
            if (!HasTrainedModel)
            {
                OnError?.Invoke("이미지를 예측하려면 먼저 모델 학습을 완료하세요.");
                return;
            }
            if (!_dataset.TryGetTestImage(imageId, out float[] input, out _))
            {
                // The sample UI disables selection while submitting; release it on local rejection.
                OnStateChanged?.Invoke();
                OnError?.Invoke("내장 데이터에 해당 샘플이 없습니다. 이미지 이름을 확인하세요.");
                return;
            }

            CnnTrainingSession session = _session;
            ConcurrentQueue<Action> pending = _pending;
            int generation = _sessionGeneration;
            int operation = ++_operationGeneration;
            SetState(ModelExecutionState.Inferring);
            if (!IsCurrent(session, generation, operation))
            {
                return;
            }
            try
            {
                InferenceResult result = await session.InferAsync(input, imageId).ConfigureAwait(false);
                pending.Enqueue(() =>
                {
                    if (!IsCurrent(session, generation, operation))
                    {
                        return;
                    }
                    SetState(ModelExecutionState.Completed);
                    if (IsCurrent(session, generation, operation))
                    {
                        OnInferenceCompleted?.Invoke(result);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                pending.Enqueue(() => CompleteCancellation(session, generation, operation));
            }
            catch (Exception exception)
            {
                pending.Enqueue(() => ReportFailure(session, generation, operation,
                    "이미지를 예측하지 못했습니다. " + exception.Message));
            }
        }

        public void CloseSession()
        {
            _sessionGeneration++;
            _operationGeneration++;
            CnnTrainingSession session = _session;
            _session = null;
            // The old worker retains its own queue until it exits, never the next session's queue.
            _pending = new ConcurrentQueue<Action>();
            session?.Dispose();
            SubmittedModel = null;
            SetState(ModelExecutionState.Idle);
        }

        private void PrepareSession()
        {
            if (_session != null)
            {
                return;
            }
            _preparationError = null;
            try
            {
                if (_trainingSettings == null)
                {
                    throw new InvalidOperationException("로컬 학습 설정 에셋이 연결되지 않았습니다.");
                }
                _options = _trainingSettings.CreateOptions();
                // TextAsset access and preparation happen once on scene entry, before user training.
                if (_dataset == null)
                {
                    _dataset = _trainingSettings.LoadDataset();
                }
                _session = new CnnTrainingSession();
            }
            catch (Exception exception)
            {
                _preparationError = "학습 데이터를 준비하지 못했습니다. " + exception.Message;
            }
        }

        private bool IsCurrent(CnnTrainingSession session, int generation, int operation)
        {
            return generation == _sessionGeneration && operation == _operationGeneration
                && ReferenceEquals(session, _session) && this != null && isActiveAndEnabled;
        }

        private void CompleteCancellation(CnnTrainingSession session, int generation, int operation)
        {
            if (!IsCurrent(session, generation, operation))
            {
                return;
            }
            SetState(ModelExecutionState.Cancelled);
            if (IsCurrent(session, generation, operation))
            {
                OnTrainingCancelled?.Invoke();
            }
        }

        private void ReportFailure(CnnTrainingSession session, int generation, int operation, string message)
        {
            if (!IsCurrent(session, generation, operation))
            {
                return;
            }
            SetState(ModelExecutionState.Failed);
            if (IsCurrent(session, generation, operation))
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
            CloseSession();
        }
    }
}
