using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Networking
{
    // 요청과 수신 이벤트는 Unity 메인 스레드에서 처리한다. 소켓의 수명은 WebSocketClient가 관리한다.
    public sealed class ModelProtocolClient
    {
        private readonly Func<string, CancellationToken, Task> _sendJson;
        private string _inferenceImageId;
        private int _lastEpoch;
        private int? _requestedEpochs;
        private int? _totalEpochs;
        private int _sessionGeneration;

        public ModelOperation Operation { get; private set; }
        public bool HasTrainedModel { get; private set; }

        public event Action<TrainingProgress> TrainingProgressReceived;
        public event Action<TrainingResult> TrainingCompleted;
        public event Action TrainingCancelled;
        public event Action<InferenceResult> InferenceCompleted;
        public event Action<ModelServerError> ServerErrorReceived;
        public event Action<Exception> ProtocolError;

        public ModelProtocolClient(Func<string, CancellationToken, Task> sendJson)
        {
            _sendJson = sendJson ?? throw new ArgumentNullException(nameof(sendJson));
        }

        public Task TrainAsync(string modelJson, int? epoch = null, CancellationToken cancellationToken = default)
        {
            RequireIdle();
            cancellationToken.ThrowIfCancellationRequested();
            if (epoch.HasValue && epoch.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(epoch), "Epoch must be a positive integer.");
            }

            JObject model = JObject.Parse(modelJson);
            JObject request = new JObject { ["type"] = "train", ["model"] = model };
            if (epoch.HasValue)
            {
                request["epoch"] = epoch.Value;
            }

            // 재학습은 이전 모델을 대체하므로 완료 전까지 추론할 모델이 없다.
            HasTrainedModel = false;
            _lastEpoch = 0;
            _requestedEpochs = epoch;
            _totalEpochs = null;
            Operation = ModelOperation.Training;
            return SendRequestAsync(request, cancellationToken);
        }

        public Task CancelTrainingAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Operation != ModelOperation.Training)
            {
                throw new InvalidOperationException("There is no cancellable training operation.");
            }

            Operation = ModelOperation.CancellingTraining;
            return SendRequestAsync(new JObject { ["type"] = "cancel_training" }, cancellationToken);
        }

        public Task InferAsync(string imageId, CancellationToken cancellationToken = default)
        {
            RequireIdle();
            cancellationToken.ThrowIfCancellationRequested();
            if (!HasTrainedModel)
            {
                throw new InvalidOperationException("Train a model before running inference.");
            }
            if (string.IsNullOrWhiteSpace(imageId))
            {
                throw new ArgumentException("An image ID is required.", nameof(imageId));
            }

            _inferenceImageId = imageId;
            Operation = ModelOperation.Inferring;
            return SendRequestAsync(new JObject { ["type"] = "infer", ["image_id"] = imageId }, cancellationToken);
        }

        public void ResetSession()
        {
            _sessionGeneration++;
            Operation = ModelOperation.Idle;
            HasTrainedModel = false;
            _inferenceImageId = null;
            _lastEpoch = 0;
            _requestedEpochs = null;
            _totalEpochs = null;
        }

        public void HandleJson(string json)
        {
            Action notification;
            try
            {
                // 구독자의 예외를 프로토콜 파싱 오류로 잘못 처리하지 않는다.
                notification = ApplyMessage(JObject.Parse(json));
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException
                                              || exception is OverflowException)
            {
                ProtocolError?.Invoke(exception);
                return;
            }

            notification();
        }

        private async Task SendRequestAsync(JObject request, CancellationToken cancellationToken)
        {
            int generation = _sessionGeneration;
            try
            {
                await _sendJson(request.ToString(Formatting.None), cancellationToken);
            }
            catch
            {
                if (generation == _sessionGeneration)
                {
                    ResetSession();
                }
                throw;
            }
        }

        private void RequireIdle()
        {
            if (Operation != ModelOperation.Idle)
            {
                throw new InvalidOperationException("Only one model operation is allowed at a time.");
            }
        }

        private void RequireTraining()
        {
            if (Operation != ModelOperation.Training && Operation != ModelOperation.CancellingTraining)
            {
                throw new InvalidDataException("Received a training response without an active training operation.");
            }
        }

        private Action ApplyMessage(JObject message)
        {
            switch (ReadString(message, "type"))
            {
                case "training_progress":
                    RequireTraining();
                    TrainingProgress progress = new TrainingProgress
                    {
                        Epoch = ReadPositiveInteger(message, "epoch"),
                        Epochs = ReadPositiveInteger(message, "epochs"),
                        Logs = ReadLogs(message)
                    };
                    if (progress.Epoch <= _lastEpoch || progress.Epoch >= progress.Epochs
                        || (_requestedEpochs.HasValue && progress.Epochs != _requestedEpochs.Value)
                        || (_totalEpochs.HasValue && progress.Epochs != _totalEpochs.Value))
                    {
                        throw new InvalidDataException("Progress must describe a new, non-final epoch.");
                    }
                    _lastEpoch = progress.Epoch;
                    _totalEpochs = progress.Epochs;
                    return () => TrainingProgressReceived?.Invoke(progress);

                case "training_completed":
                    RequireTraining();
                    TrainingResult result = new TrainingResult
                    {
                        Epoch = ReadPositiveInteger(message, "epoch"),
                        Logs = ReadLogs(message)
                    };
                    int? expectedEpochs = _totalEpochs ?? _requestedEpochs;
                    if (result.Epoch <= _lastEpoch || (expectedEpochs.HasValue && result.Epoch != expectedEpochs.Value))
                    {
                        throw new InvalidDataException("Completion must describe the final epoch.");
                    }
                    Operation = ModelOperation.Idle;
                    HasTrainedModel = true;
                    return () => TrainingCompleted?.Invoke(result);

                case "training_cancelled":
                    if (Operation != ModelOperation.CancellingTraining)
                    {
                        throw new InvalidDataException("Received cancellation without a cancellation request.");
                    }
                    ResetSession();
                    return () => TrainingCancelled?.Invoke();

                case "inference_result":
                    if (Operation != ModelOperation.Inferring)
                    {
                        throw new InvalidDataException("Received inference without an active inference operation.");
                    }
                    InferenceResult inference = new InferenceResult
                    {
                        ImageId = ReadString(message, "image_id"),
                        Output = ReadOutput(message)
                    };
                    if (inference.ImageId != _inferenceImageId)
                    {
                        throw new InvalidDataException("Inference image ID does not match the current request.");
                    }
                    _inferenceImageId = null;
                    Operation = ModelOperation.Idle;
                    return () => InferenceCompleted?.Invoke(inference);

                case "error":
                    ModelServerError error = new ModelServerError
                    {
                        RequestType = ReadString(message, "request_type"),
                        Code = ReadString(message, "code"),
                        Message = ReadString(message, "message")
                    };
                    ApplyError(error);
                    return () => ServerErrorReceived?.Invoke(error);

                default:
                    throw new InvalidDataException("Unknown server message type.");
            }
        }

        private void ApplyError(ModelServerError error)
        {
            if (error.RequestType != "train" && error.RequestType != "infer" && error.RequestType != "cancel_training")
            {
                throw new InvalidDataException("Unknown request type in error response.");
            }
            // busy는 추가 요청의 거절이며 이미 실행 중인 작업을 종료하지 않는다.
            if (error.Code == "busy")
            {
                return;
            }
            if (error.RequestType == "train")
            {
                RequireTraining();
                ResetSession();
            }
            else if (error.RequestType == "infer")
            {
                if (Operation != ModelOperation.Inferring)
                {
                    throw new InvalidDataException("Inference error without an active inference operation.");
                }
                Operation = ModelOperation.Idle;
                _inferenceImageId = null;
                if (error.Code == "no_model")
                {
                    HasTrainedModel = false;
                }
            }
            else if (Operation == ModelOperation.CancellingTraining)
            {
                if (error.Code == "no_training")
                {
                    ResetSession();
                }
                else
                {
                    Operation = ModelOperation.Training;
                }
            }
            else if (error.Code != "no_training")
            {
                throw new InvalidDataException("Cancellation error without a cancellation request.");
            }
        }

        private static string ReadString(JObject message, string name)
        {
            JToken token = message[name];
            if (token == null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
            {
                throw new InvalidDataException($"'{name}' must be a non-empty string.");
            }
            return token.Value<string>();
        }

        private static int ReadPositiveInteger(JObject message, string name)
        {
            JToken token = message[name];
            if (token == null || token.Type != JTokenType.Integer || token.Value<long>() <= 0
                || token.Value<long>() > int.MaxValue)
            {
                throw new InvalidDataException($"'{name}' must be a positive integer.");
            }
            return token.Value<int>();
        }

        private static double ReadNumber(JToken token)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
            {
                throw new InvalidDataException("Keras values must be JSON numbers.");
            }
            double value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidDataException("Keras values must be finite.");
            }
            return value;
        }

        private static IReadOnlyDictionary<string, double> ReadLogs(JObject message)
        {
            if (!(message["logs"] is JObject logs))
            {
                throw new InvalidDataException("'logs' must be a Keras metrics object.");
            }
            Dictionary<string, double> values = new Dictionary<string, double>();
            foreach (JProperty property in logs.Properties())
            {
                values.Add(property.Name, ReadNumber(property.Value));
            }
            return values;
        }

        private static IReadOnlyList<double> ReadOutput(JObject message)
        {
            if (!(message["output"] is JArray output) || output.Count != 10)
            {
                throw new InvalidDataException("'output' must contain ten MNIST class values.");
            }
            List<double> values = new List<double>(10);
            foreach (JToken token in output)
            {
                values.Add(ReadNumber(token));
            }
            return values;
        }
    }
}
