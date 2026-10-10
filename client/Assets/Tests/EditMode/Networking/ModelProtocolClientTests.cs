using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Networking.Tests
{
    public sealed class ModelProtocolClientTests
    {
        private ModelProtocolClient _client;
        private Action<string> _receiveJson;
        private List<JObject> _requests;
        private int _protocolErrors;
        private GameObject _gameObject;
        private WebSocketClient _webSocketClient;
        private WebSocketConnectionTests.LoopbackServer _server;
        private WebSocket _peer;
        private SynchronizationContext _previousContext;

        [SetUp]
        public void SetUp()
        {
            _requests = new List<JObject>();
            _protocolErrors = 0;
            // 동기식 테스트 대기가 Unity의 메인 스레드 continuation을 막지 않도록 한다.
            _previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            _server = new WebSocketConnectionTests.LoopbackServer();
            _client = new ModelProtocolClient();
            // 수신 처리 메서드는 공개하지 않고 테스트에서만 응답을 주입한다.
            _receiveJson = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), _client,
                typeof(ModelProtocolClient).GetMethod("HandleJson", BindingFlags.Instance | BindingFlags.NonPublic));
            _webSocketClient = (WebSocketClient)typeof(ModelProtocolClient)
                .GetField("_webSocketClient", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            _gameObject = _webSocketClient.gameObject;
            // EditMode에서는 일반 MonoBehaviour의 OnEnable이 자동 실행되지 않는다.
            typeof(WebSocketClient).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_webSocketClient, null);
            Task<WebSocket> accept = _server.AcceptAsync();
            // 테스트에서만 내부 접속 대상을 로컬 서버로 바꾼다.
            WebSocketConnection connection = (WebSocketConnection)typeof(WebSocketClient)
                .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_webSocketClient);
            WebSocketConnectionTests.SetServerUrl(connection, _server.Url);
            Wait(_client.ConnectAsync());
            Wait(accept);
            _peer = accept.GetAwaiter().GetResult();
            _client.ProtocolError += exception => _protocolErrors++;
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                typeof(WebSocketClient).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_webSocketClient, null);
                _client.Dispose();
            }
            _peer?.Dispose();
            _server?.Dispose();
            SynchronizationContext.SetSynchronizationContext(_previousContext);
        }

        [Test]
        public void DefaultEpochIsOmittedAndModelRemainsAnObject()
        {
            SendAndRecord(_client.TrainAsync("{\"layers\":[{\"name\":\"dense\"}]}"));
            Assert.That(_requests[0]["epoch"], Is.Null);
            Assert.That(_requests[0]["model"], Is.TypeOf<JObject>());
            Assert.That(_requests[0]["model"]["layers"][0]["name"].Value<string>(), Is.EqualTo("dense"));
            Assert.That(_requests[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void ExplicitEpochIsSent()
        {
            Train(3);
            Assert.That(_requests[0]["epoch"].Value<int>(), Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidEpochDoesNotStartTraining(int epoch)
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _client.TrainAsync("{}", epoch));
            Assert.That(_requests, Is.Empty);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void InvalidModelDoesNotStartTraining()
        {
            Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(() => _client.TrainAsync("[]"));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void OneEpochCompletesWithoutProgressAndIncludesFixedMetricsAndEvaluation()
        {
            TrainingResult result = null;
            _client.TrainingCompleted += value => result = value;
            Train();
            Complete();
            Assert.That(result.Epoch, Is.EqualTo(1));
            Assert.That(result.Logs["loss"], Is.EqualTo(0.2));
            Assert.That(result.Logs.Count, Is.EqualTo(4));
            Assert.That(result.Logs["accuracy"], Is.EqualTo(0.9));
            Assert.That(result.Logs["val_loss"], Is.EqualTo(0.3));
            Assert.That(result.Logs["val_accuracy"], Is.EqualTo(0.8));
            Assert.That(result.Evaluation.Count, Is.EqualTo(2));
            Assert.That(result.Evaluation["loss"], Is.EqualTo(0.4));
            Assert.That(result.Evaluation["accuracy"], Is.EqualTo(0.75));
            Assert.That(_client.HasTrainedModel, Is.True);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void ProgressIncludesFixedTrainingAndValidationMetrics()
        {
            TrainingProgress progress = null;
            _client.TrainingProgressReceived += value => progress = value;
            Train(2);
            Progress(1, 2);
            Assert.That(progress.Logs.Count, Is.EqualTo(4));
            Assert.That(progress.Logs["accuracy"], Is.EqualTo(0.9));
            Assert.That(progress.Logs["val_loss"], Is.EqualTo(0.3));
            Assert.That(progress.Logs["val_accuracy"], Is.EqualTo(0.8));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
            Complete(2);
            Assert.That(_client.HasTrainedModel, Is.True);
        }

        [Test]
        public void FinalEpochProgressIsRejected()
        {
            Train(1);
            Progress(1, 1);
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void AdditionalTrainingAndInferenceAreRejectedDuringTraining()
        {
            Train();
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("42"));
            Assert.That(_requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void CancellationWaitsForConfirmationThenDeletesModel()
        {
            int cancelled = 0;
            _client.TrainingCancelled += () => cancelled++;
            Train(2);
            SendAndRecord(_client.CancelTrainingAsync());
            Assert.That(_requests[1].Count, Is.EqualTo(1));
            Assert.That(_requests[1]["type"].Value<string>(), Is.EqualTo("cancel_training"));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.CancellingTraining));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.CancelTrainingAsync());
            Progress(1, 2);
            Assert.That(_protocolErrors, Is.Zero);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.CancellingTraining));
            _receiveJson("{\"type\":\"training_cancelled\"}");
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("42"));
            Train();
        }

        [Test]
        public void CompletionWinningCancellationRaceKeepsTheModel()
        {
            Train();
            SendAndRecord(_client.CancelTrainingAsync());
            Complete();
            Error("cancel_training", "no_training");
            Assert.That(_protocolErrors, Is.Zero);
            Assert.That(_client.HasTrainedModel, Is.True);
        }

        [Test]
        public void RetrainingInvalidatesPreviousModelImmediately()
        {
            Train();
            Complete();
            Train();
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void InferenceSendsOnlyImageIdAndPreservesRawOutput()
        {
            InferenceResult result = null;
            _client.InferenceCompleted += value => result = value;
            Train();
            Complete();
            SendAndRecord(_client.InferAsync("mnist-test-42"));
            Assert.That(_requests[1].Count, Is.EqualTo(2));
            Assert.That(_requests[1]["image_id"].Value<string>(), Is.EqualTo("mnist-test-42"));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("43"));
            _receiveJson("{\"type\":\"inference_result\",\"image_id\":\"mnist-test-42\",\"output\":[-2,3,0,0,0,0,0,0,0,0]}");
            Assert.That(result.Output[0], Is.EqualTo(-2));
            Assert.That(_client.HasTrainedModel, Is.True);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void InferenceRequiresTrainedModel()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("42"));
        }

        [Test]
        public void WrongImageResponseDoesNotCompleteInference()
        {
            Train();
            Complete();
            SendAndRecord(_client.InferAsync("42"));
            _receiveJson("{\"type\":\"inference_result\",\"image_id\":\"43\",\"output\":[0,0,0,0,0,0,0,0,0,0]}");
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Inferring));
        }

        [TestCase("busy", true)]
        [TestCase("training_failed", false)]
        [TestCase("invalid_request", false)]
        public void TrainingErrorsDistinguishRejectionFromFailure(string code, bool remainsTraining)
        {
            Train();
            Error("train", code);
            Assert.That(_client.Operation, Is.EqualTo(remainsTraining ? ModelOperation.Training : ModelOperation.Idle));
        }

        [TestCase("image_not_found", true)]
        [TestCase("inference_failed", true)]
        [TestCase("no_model", false)]
        public void InferenceErrorReleasesOperationAndPreservesAvailableModel(string code, bool hasModel)
        {
            Train();
            Complete();
            SendAndRecord(_client.InferAsync("42"));
            Error("infer", code);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(_client.HasTrainedModel, Is.EqualTo(hasModel));
        }

        [Test]
        public void FailedCancellationKeepsTrainingActive()
        {
            Train();
            SendAndRecord(_client.CancelTrainingAsync());
            Error("cancel_training", "cancellation_failed");
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
        }

        [Test]
        public void MissingServerTrainingDoesNotLeaveCancellationStuck()
        {
            Train();
            SendAndRecord(_client.CancelTrainingAsync());
            Error("cancel_training", "no_training");
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [TestCase("{broken")]
        [TestCase("{\"type\":\"unknown\"}")]
        [TestCase("{\"type\":\"training_completed\",\"epoch\":1}")]
        [TestCase("{\"type\":\"training_completed\",\"epoch\":1,\"logs\":{\"loss\":null}}")]
        [TestCase("{\"type\":\"training_completed\",\"epoch\":1,\"logs\":{\"loss\":\"0.2\"}}")]
        public void MalformedResponseCannotMarkTrainingCompleted(string json)
        {
            Train();
            _receiveJson(json);
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
        }

        [TestCase("logs", "loss")]
        [TestCase("logs", "accuracy")]
        [TestCase("logs", "val_loss")]
        [TestCase("logs", "val_accuracy")]
        [TestCase("evaluation", "loss")]
        [TestCase("evaluation", "accuracy")]
        public void MissingRequiredMetricCannotMakeModelReady(string field, string metric)
        {
            Train();
            JObject message = CompletionMessage();
            ((JObject)message[field]).Remove(metric);
            _receiveJson(message.ToString());
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("42"));
        }

        [Test]
        public void CompletionMustIncludeTestEvaluation()
        {
            Train();
            JObject message = CompletionMessage();
            message.Remove("evaluation");
            _receiveJson(message.ToString());
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
        }

        [TestCase("logs")]
        [TestCase("evaluation")]
        public void ExtraMetricCannotCompleteTraining(string field)
        {
            Train();
            JObject message = CompletionMessage();
            message[field]["precision"] = 0.9;
            _receiveJson(message.ToString());
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [TestCase("logs", "accuracy", -0.1)]
        [TestCase("logs", "val_accuracy", 95)]
        [TestCase("evaluation", "accuracy", 1.1)]
        [TestCase("evaluation", "loss", double.NaN)]
        public void InvalidMetricValueCannotCompleteTraining(string field, string metric, double value)
        {
            Train();
            JObject message = CompletionMessage();
            message[field][metric] = value;
            _receiveJson(message.ToString());
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void ProgressRequiresValidationMetricsAndLeavesTrainingActiveOnError()
        {
            Train(2);
            JObject logs = TrainingLogs();
            logs.Remove("val_accuracy");
            _receiveJson(new JObject
            {
                ["type"] = "training_progress", ["epoch"] = 1, ["epochs"] = 2,
                ["logs"] = logs
            }.ToString());
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
            SendAndRecord(_client.CancelTrainingAsync());
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.CancellingTraining));
        }

        [Test]
        public void DisconnectResetsModelAndRejectsStaleCompletion()
        {
            Train();
            _client.ResetSession();
            Complete();
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void DisconnectAfterCompletionRequiresFreshTraining()
        {
            Train();
            Complete();
            _client.ResetSession();
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("42"));
            Train();
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void FailedSendClearsOperationAndModel()
        {
            Train();
            Complete();
            // 비활성화된 전송 객체로 송신하면 실패하고 작업 상태를 폐기해야 한다.
            typeof(WebSocketClient).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_webSocketClient, null);
            Assert.Throws<InvalidOperationException>(() => _client.TrainAsync("{}").GetAwaiter().GetResult());
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void AlreadyCancelledTokenDoesNotInvalidateCompletedModel()
        {
            Train();
            Complete();
            Assert.CatchAsync<OperationCanceledException>(() => _client.TrainAsync("{}", cancellationToken: new CancellationToken(true)));
            Assert.That(_client.HasTrainedModel, Is.True);
        }

        [Test]
        public void ConstructorCreatesWebSocketClientInternally()
        {
            Assert.That(_webSocketClient, Is.Not.Null);
            Assert.That(_client.IsConnected, Is.True);
        }

        private static void Wait(Task task)
        {
            if (!task.Wait(TimeSpan.FromSeconds(10)))
            {
                Assert.Fail("WebSocket test operation timed out.");
            }
            task.GetAwaiter().GetResult();
        }

        private void SendAndRecord(Task task)
        {
            JObject request = ReceiveRequest();
            if (request["type"].Value<string>() == "train")
            {
                Assert.That(task.IsCompleted, Is.False);
                Assert.That(_client.Operation, Is.EqualTo(ModelOperation.StartingTraining));
                _receiveJson(new JObject
                {
                    ["type"] = "training_started", ["epochs"] = request["epoch"]?.Value<int>() ?? 1
                }.ToString());
            }
            Wait(task);
        }

        private JObject ReceiveRequest()
        {
            byte[] buffer = new byte[8192];
            Task<WebSocketReceiveResult> receive = _peer.ReceiveAsync(new ArraySegment<byte>(buffer), _server.Token);
            Wait(receive);
            WebSocketReceiveResult result = receive.GetAwaiter().GetResult();
            Assert.That(result.EndOfMessage, Is.True);
            JObject request = JObject.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count));
            _requests.Add(request);
            return request;
        }

        [Test]
        public void TrainingWaitsForServerStartAndBlocksAdditionalRequests()
        {
            Task training = _client.TrainAsync("{}", 2);
            ReceiveRequest();
            Assert.That(training.IsCompleted, Is.False);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.StartingTraining));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.InferAsync("42"));
            _receiveJson("{\"type\":\"training_started\",\"epochs\":2}");
            Wait(training);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [TestCase("busy")]
        [TestCase("invalid_request")]
        [TestCase("training_failed")]
        public void RejectedTrainingFaultsStartWait(string code)
        {
            Task training = _client.TrainAsync("{}");
            ReceiveRequest();
            Error("train", code);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await training);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void SessionResetReleasesTrainingStartWait()
        {
            Task training = _client.TrainAsync("{}");
            ReceiveRequest();
            _client.ResetSession();
            Assert.ThrowsAsync<System.IO.IOException>(async () => await training);
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        private void Train(int? epoch = null)
        {
            SendAndRecord(_client.TrainAsync("{}", epoch));
        }

        private void Complete(int epoch = 1)
        {
            _receiveJson(CompletionMessage(epoch).ToString());
        }

        private void Progress(int epoch, int epochs)
        {
            _receiveJson(new JObject
            {
                ["type"] = "training_progress", ["epoch"] = epoch,
                ["epochs"] = epochs, ["logs"] = TrainingLogs()
            }.ToString());
        }

        private static JObject TrainingLogs()
        {
            return new JObject
            {
                ["loss"] = 0.2, ["accuracy"] = 0.9,
                ["val_loss"] = 0.3, ["val_accuracy"] = 0.8
            };
        }

        private static JObject CompletionMessage(int epoch = 1)
        {
            return new JObject
            {
                ["type"] = "training_completed", ["epoch"] = epoch,
                ["logs"] = TrainingLogs(),
                ["evaluation"] = new JObject { ["loss"] = 0.4, ["accuracy"] = 0.75 }
            };
        }

        private void Error(string requestType, string code)
        {
            _receiveJson(new JObject
            {
                ["type"] = "error", ["request_type"] = requestType,
                ["code"] = code, ["message"] = "Server error"
            }.ToString());
        }
    }
}
