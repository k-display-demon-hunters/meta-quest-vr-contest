using System;
using System.Threading;
using System.Threading.Tasks;
using Networking;
using UnityEngine;

namespace ModelDesignScene
{
    public enum ModelExecutionState
    {
        Idle,
        Connecting,
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
        private ModelProtocolClient _protocol;
        private CancellationTokenSource _sessionCancellation;
        private int _sessionGeneration;
        private int _operationGeneration;

        public ModelExecutionState State { get; private set; }
        public string SubmittedModelJson { get; private set; }
        public bool IsBusy => State == ModelExecutionState.Connecting
                              || State == ModelExecutionState.StartingTraining
                              || State == ModelExecutionState.Training
                              || State == ModelExecutionState.CancellingTraining
                              || State == ModelExecutionState.Inferring;
        public bool CanCancel => State == ModelExecutionState.Connecting
                                 || State == ModelExecutionState.StartingTraining
                                 || State == ModelExecutionState.Training;
        public bool HasTrainedModel => _protocol != null && _protocol.IsConnected && _protocol.HasTrainedModel;

        public event Action OnStateChanged;
        public event Action<TrainingProgress> OnTrainingProgress;
        public event Action<TrainingResult> OnTrainingCompleted;
        public event Action OnTrainingCancelled;
        public event Action<InferenceResult> OnInferenceCompleted;
        public event Action<string> OnError;

        public async Task StartTrainingAsync(string modelJson)
        {
            if (!isActiveAndEnabled || IsBusy)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(modelJson))
            {
                OnError?.Invoke("학습할 모델이 없습니다.");
                return;
            }

            if (_protocol != null && !_protocol.IsConnected)
            {
                ReleaseSession();
            }

            int session = _sessionGeneration;
            int operation = ++_operationGeneration;
            ModelProtocolClient protocol = _protocol;
            SubmittedModelJson = modelJson;

            try
            {
                if (protocol == null)
                {
                    protocol = CreateSession();
                    SetState(ModelExecutionState.Connecting);
                    if (!IsCurrent(protocol, session, operation))
                    {
                        return;
                    }
                    await protocol.ConnectAsync(_sessionCancellation.Token);
                    if (!IsCurrent(protocol, session, operation))
                    {
                        return;
                    }
                }

                SetState(ModelExecutionState.StartingTraining);
                if (!IsCurrent(protocol, session, operation))
                {
                    return;
                }
                await protocol.TrainAsync(modelJson, epoch: null, cancellationToken: _sessionCancellation.Token);

                // 시작 확인 뒤의 진행·완료 응답이 await 재개보다 먼저 도착할 수 있다.
                if (IsCurrent(protocol, session, operation)
                    && State == ModelExecutionState.StartingTraining
                    && protocol.Operation == ModelOperation.Training)
                {
                    SetState(ModelExecutionState.Training);
                }
            }
            catch (Exception exception)
            {
                if (IsCurrent(protocol, session, operation))
                {
                    FailSession(State == ModelExecutionState.Connecting
                        ? "학습 서버에 연결할 수 없습니다. 서버 주소와 실행 상태를 확인하세요."
                        : "학습을 시작하지 못했습니다. 다시 시도하세요.", exception);
                }
            }
        }

        public async Task CancelTrainingAsync()
        {
            if (!isActiveAndEnabled || !CanCancel)
            {
                return;
            }

            if (State == ModelExecutionState.Connecting || State == ModelExecutionState.StartingTraining)
            {
                // 시작 확인 전에는 접수 여부를 알 수 없어 연결 종료로 서버 작업도 폐기한다.
                ReleaseSession();
                SetState(ModelExecutionState.Cancelled);
                if (isActiveAndEnabled && State == ModelExecutionState.Cancelled)
                {
                    OnTrainingCancelled?.Invoke();
                }
                return;
            }

            ModelProtocolClient protocol = _protocol;
            int session = _sessionGeneration;
            int operation = _operationGeneration;
            SetState(ModelExecutionState.CancellingTraining);
            if (!IsCurrent(protocol, session, operation))
            {
                return;
            }

            try
            {
                await protocol.CancelTrainingAsync(_sessionCancellation.Token);
                // 송신 완료는 취소 완료가 아니다. 서버의 종료 응답을 기다린다.
            }
            catch (Exception exception)
            {
                if (IsCurrent(protocol, session, operation))
                {
                    FailSession("학습 취소 요청을 보내지 못해 연결을 종료했습니다.", exception);
                }
            }
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
            if (string.IsNullOrWhiteSpace(imageId))
            {
                OnError?.Invoke("예측할 샘플 이미지를 선택하세요.");
                return;
            }

            ModelProtocolClient protocol = _protocol;
            int session = _sessionGeneration;
            int operation = ++_operationGeneration;
            SetState(ModelExecutionState.Inferring);
            if (!IsCurrent(protocol, session, operation))
            {
                return;
            }

            try
            {
                await protocol.InferAsync(imageId, _sessionCancellation.Token);
            }
            catch (Exception exception)
            {
                if (IsCurrent(protocol, session, operation))
                {
                    FailSession("예측 요청을 보내지 못해 연결을 종료했습니다. 다시 학습하세요.", exception);
                }
            }
        }

        public void CloseSession()
        {
            ReleaseSession();
            SubmittedModelJson = null;
            SetState(ModelExecutionState.Idle);
        }

        private ModelProtocolClient CreateSession()
        {
            _sessionCancellation = new CancellationTokenSource();
            _protocol = new ModelProtocolClient();
            _protocol.TrainingProgressReceived += HandleTrainingProgress;
            _protocol.TrainingCompleted += HandleTrainingCompleted;
            _protocol.TrainingCancelled += HandleTrainingCancelled;
            _protocol.InferenceCompleted += HandleInferenceCompleted;
            _protocol.ServerErrorReceived += HandleServerError;
            _protocol.ProtocolError += HandleProtocolError;
            _protocol.ConnectionError += HandleConnectionError;
            _protocol.Disconnected += HandleDisconnected;
            return _protocol;
        }

        private bool IsCurrent(ModelProtocolClient protocol, int session, int operation)
        {
            // A cancelled continuation may resume after Unity has destroyed this component.
            return session == _sessionGeneration && operation == _operationGeneration
                   && ReferenceEquals(protocol, _protocol) && this != null && isActiveAndEnabled;
        }

        private void ReleaseSession()
        {
            _sessionGeneration++;
            _operationGeneration++;
            ModelProtocolClient protocol = _protocol;
            CancellationTokenSource cancellation = _sessionCancellation;
            _protocol = null;
            _sessionCancellation = null;

            if (protocol != null)
            {
                protocol.TrainingProgressReceived -= HandleTrainingProgress;
                protocol.TrainingCompleted -= HandleTrainingCompleted;
                protocol.TrainingCancelled -= HandleTrainingCancelled;
                protocol.InferenceCompleted -= HandleInferenceCompleted;
                protocol.ServerErrorReceived -= HandleServerError;
                protocol.ProtocolError -= HandleProtocolError;
                protocol.ConnectionError -= HandleConnectionError;
                protocol.Disconnected -= HandleDisconnected;
            }
            cancellation?.Cancel();
            protocol?.Dispose();
            cancellation?.Dispose();
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

        private void FailSession(string message, Exception exception = null)
        {
            ReleaseSession();
            SetState(ModelExecutionState.Failed);
            if (isActiveAndEnabled && State == ModelExecutionState.Failed)
            {
                OnError?.Invoke(message);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (exception != null)
            {
                Debug.LogWarning($"[CNNModelDesignLab] {message}\n{exception}", this);
            }
#endif
        }

        private void HandleTrainingProgress(TrainingProgress progress)
        {
            if (State == ModelExecutionState.StartingTraining)
            {
                SetState(ModelExecutionState.Training);
            }
            if (isActiveAndEnabled && _protocol != null)
            {
                OnTrainingProgress?.Invoke(progress);
            }
        }

        private void HandleTrainingCompleted(TrainingResult result)
        {
            _operationGeneration++;
            SetState(ModelExecutionState.Completed);
            if (isActiveAndEnabled && HasTrainedModel)
            {
                OnTrainingCompleted?.Invoke(result);
            }
        }

        private void HandleTrainingCancelled()
        {
            _operationGeneration++;
            SetState(ModelExecutionState.Cancelled);
            if (isActiveAndEnabled && State == ModelExecutionState.Cancelled)
            {
                OnTrainingCancelled?.Invoke();
            }
        }

        private void HandleInferenceCompleted(InferenceResult result)
        {
            _operationGeneration++;
            SetState(ModelExecutionState.Completed);
            if (isActiveAndEnabled && HasTrainedModel)
            {
                OnInferenceCompleted?.Invoke(result);
            }
        }

        private void HandleServerError(ModelServerError error)
        {
            if (error.Code == "busy")
            {
                if (State == ModelExecutionState.StartingTraining && _protocol.Operation == ModelOperation.Idle)
                {
                    _operationGeneration++;
                    SetState(ModelExecutionState.Failed);
                }
                OnError?.Invoke("서버가 다른 작업을 처리 중입니다. 진행 중인 작업이 끝날 때까지 기다리세요.");
                return;
            }

            if (error.RequestType == "cancel_training")
            {
                // 완료가 취소보다 먼저 확정됐다면 늦은 no_training은 완료 모델에 영향을 주지 않는다.
                if (error.Code == "no_training" && HasTrainedModel)
                {
                    return;
                }
                if (_protocol.Operation == ModelOperation.Training)
                {
                    SetState(ModelExecutionState.Training);
                    OnError?.Invoke("취소하지 못했습니다. 학습이 계속 진행됩니다.");
                    return;
                }
                if (State != ModelExecutionState.CancellingTraining)
                {
                    return;
                }
            }

            _operationGeneration++;
            SetState(HasTrainedModel ? ModelExecutionState.Completed : ModelExecutionState.Failed);
            OnError?.Invoke(GetServerErrorMessage(error));
        }

        private static string GetServerErrorMessage(ModelServerError error)
        {
            switch (error.Code)
            {
                case "training_failed":
                    return "서버에서 학습 또는 평가에 실패했습니다. 모델을 확인하고 다시 학습하세요.";
                case "no_model":
                    return "서버에 학습된 모델이 없습니다. 다시 학습하세요.";
                case "image_not_found":
                    return "서버에 같은 ID의 샘플 이미지가 없습니다. 이미지 파일명을 확인하세요.";
                case "inference_failed":
                    return "이미지 예측에 실패했습니다. 다른 샘플을 선택하거나 다시 시도하세요.";
                case "no_training":
                    return "서버에 진행 중인 학습이 없습니다. 다시 학습하세요.";
                case "invalid_request":
                    return "서버가 요청을 거절했습니다. 모델과 샘플 설정을 확인하세요.";
                default:
                    return "서버에서 작업을 처리하지 못했습니다. 다시 시도하세요.";
            }
        }

        private void HandleProtocolError(Exception exception)
        {
            FailSession("서버 응답 형식이 올바르지 않아 연결을 종료했습니다. 통신 규격을 확인하세요.", exception);
        }

        private void HandleConnectionError(Exception exception)
        {
            FailSession("학습 서버와 통신할 수 없습니다. 서버 주소와 실행 상태를 확인하세요.", exception);
        }

        private void HandleDisconnected()
        {
            FailSession("서버 연결이 종료되었습니다. 다시 학습하면 새로 연결합니다.");
        }

        private void OnDisable()
        {
            CloseSession();
        }
    }
}
