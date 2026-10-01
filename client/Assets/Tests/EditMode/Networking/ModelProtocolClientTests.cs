using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Networking.Tests
{
    public sealed class ModelProtocolClientTests
    {
        private ModelProtocolClient _client;
        private List<JObject> _requests;
        private int _protocolErrors;

        [SetUp]
        public void SetUp()
        {
            _requests = new List<JObject>();
            _protocolErrors = 0;
            _client = new ModelProtocolClient((json, token) =>
            {
                _requests.Add(JObject.Parse(json));
                return Task.CompletedTask;
            });
            _client.ProtocolError += exception => _protocolErrors++;
        }

        [Test]
        public void DefaultEpochIsOmittedAndModelRemainsAnObject()
        {
            _client.TrainAsync("{\"layers\":[{\"name\":\"dense\"}]}").GetAwaiter().GetResult();
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
            Assert.Throws<ArgumentOutOfRangeException>(() => _client.TrainAsync("{}", epoch));
            Assert.That(_requests, Is.Empty);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void InvalidModelDoesNotStartTraining()
        {
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => _client.TrainAsync("[]"));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void OneEpochCompletesWithoutProgressAndKeepsOnlyActualMetrics()
        {
            TrainingResult result = null;
            _client.TrainingCompleted += value => result = value;
            Train();
            Complete();
            Assert.That(result.Epoch, Is.EqualTo(1));
            Assert.That(result.Logs["loss"], Is.EqualTo(0.2));
            Assert.That(result.Logs.ContainsKey("accuracy"), Is.False);
            Assert.That(_client.HasTrainedModel, Is.True);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void ProgressPreservesCustomKerasMetrics()
        {
            TrainingProgress progress = null;
            _client.TrainingProgressReceived += value => progress = value;
            Train(2);
            _client.HandleJson("{\"type\":\"training_progress\",\"epoch\":1,\"epochs\":2,\"logs\":{\"loss\":0.3,\"sparse_categorical_accuracy\":0.8}}");
            Assert.That(progress.Logs["sparse_categorical_accuracy"], Is.EqualTo(0.8));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
            Complete(2);
            Assert.That(_client.HasTrainedModel, Is.True);
        }

        [Test]
        public void FinalEpochProgressIsRejected()
        {
            Train(1);
            _client.HandleJson("{\"type\":\"training_progress\",\"epoch\":1,\"epochs\":1,\"logs\":{}}");
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void AdditionalTrainingAndInferenceAreRejectedDuringTraining()
        {
            Train();
            Assert.Throws<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.Throws<InvalidOperationException>(() => _client.InferAsync("42"));
            Assert.That(_requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void CancellationWaitsForConfirmationThenDeletesModel()
        {
            int cancelled = 0;
            _client.TrainingCancelled += () => cancelled++;
            Train(2);
            _client.CancelTrainingAsync().GetAwaiter().GetResult();
            Assert.That(_requests[1].Count, Is.EqualTo(1));
            Assert.That(_requests[1]["type"].Value<string>(), Is.EqualTo("cancel_training"));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.CancellingTraining));
            Assert.Throws<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.Throws<InvalidOperationException>(() => _client.CancelTrainingAsync());
            _client.HandleJson("{\"type\":\"training_progress\",\"epoch\":1,\"epochs\":2,\"logs\":{}}");
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.CancellingTraining));
            _client.HandleJson("{\"type\":\"training_cancelled\"}");
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.Throws<InvalidOperationException>(() => _client.InferAsync("42"));
            Train();
        }

        [Test]
        public void CompletionWinningCancellationRaceKeepsTheModel()
        {
            Train();
            _client.CancelTrainingAsync().GetAwaiter().GetResult();
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
            _client.InferAsync("mnist-test-42").GetAwaiter().GetResult();
            Assert.That(_requests[1].Count, Is.EqualTo(2));
            Assert.That(_requests[1]["image_id"].Value<string>(), Is.EqualTo("mnist-test-42"));
            Assert.Throws<InvalidOperationException>(() => _client.TrainAsync("{}"));
            Assert.Throws<InvalidOperationException>(() => _client.InferAsync("43"));
            _client.HandleJson("{\"type\":\"inference_result\",\"image_id\":\"mnist-test-42\",\"output\":[-2,3,0,0,0,0,0,0,0,0]}");
            Assert.That(result.Output[0], Is.EqualTo(-2));
            Assert.That(_client.HasTrainedModel, Is.True);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
        }

        [Test]
        public void InferenceRequiresTrainedModel()
        {
            Assert.Throws<InvalidOperationException>(() => _client.InferAsync("42"));
        }

        [Test]
        public void WrongImageResponseDoesNotCompleteInference()
        {
            Train();
            Complete();
            _client.InferAsync("42").GetAwaiter().GetResult();
            _client.HandleJson("{\"type\":\"inference_result\",\"image_id\":\"43\",\"output\":[0,0,0,0,0,0,0,0,0,0]}");
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
            _client.InferAsync("42").GetAwaiter().GetResult();
            Error("infer", code);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(_client.HasTrainedModel, Is.EqualTo(hasModel));
        }

        [Test]
        public void FailedCancellationKeepsTrainingActive()
        {
            Train();
            _client.CancelTrainingAsync().GetAwaiter().GetResult();
            Error("cancel_training", "cancellation_failed");
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
        }

        [Test]
        public void MissingServerTrainingDoesNotLeaveCancellationStuck()
        {
            Train();
            _client.CancelTrainingAsync().GetAwaiter().GetResult();
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
            _client.HandleJson(json);
            Assert.That(_protocolErrors, Is.EqualTo(1));
            Assert.That(_client.HasTrainedModel, Is.False);
            Assert.That(_client.Operation, Is.EqualTo(ModelOperation.Training));
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
            Assert.Throws<InvalidOperationException>(() => _client.InferAsync("42"));
            Train();
            Assert.That(_client.HasTrainedModel, Is.False);
        }

        [Test]
        public void FailedSendClearsOperationAndModel()
        {
            ModelProtocolClient failing = new ModelProtocolClient((json, token) => Task.FromException(new Exception("send failed")));
            Assert.Throws<Exception>(() => failing.TrainAsync("{}").GetAwaiter().GetResult());
            Assert.That(failing.Operation, Is.EqualTo(ModelOperation.Idle));
            Assert.That(failing.HasTrainedModel, Is.False);
        }

        [Test]
        public void AlreadyCancelledTokenDoesNotInvalidateCompletedModel()
        {
            Train();
            Complete();
            Assert.Throws<OperationCanceledException>(() => _client.TrainAsync("{}", cancellationToken: new CancellationToken(true)));
            Assert.That(_client.HasTrainedModel, Is.True);
        }

        [Test]
        public void LateFailedSendCannotResetANewSession()
        {
            TaskCompletionSource<bool> pending = new TaskCompletionSource<bool>();
            int sends = 0;
            ModelProtocolClient client = new ModelProtocolClient((json, token) => ++sends == 1 ? pending.Task : Task.CompletedTask);
            Task first = client.TrainAsync("{}");
            client.ResetSession();
            client.TrainAsync("{}").GetAwaiter().GetResult();
            pending.SetException(new Exception("old send failed"));
            Assert.Throws<Exception>(() => first.GetAwaiter().GetResult());
            Assert.That(client.Operation, Is.EqualTo(ModelOperation.Training));
        }

        private void Train(int? epoch = null)
        {
            _client.TrainAsync("{}", epoch).GetAwaiter().GetResult();
        }

        private void Complete(int epoch = 1)
        {
            _client.HandleJson(new JObject
            {
                ["type"] = "training_completed", ["epoch"] = epoch,
                ["logs"] = new JObject { ["loss"] = 0.2 }
            }.ToString());
        }

        private void Error(string requestType, string code)
        {
            _client.HandleJson(new JObject
            {
                ["type"] = "error", ["request_type"] = requestType,
                ["code"] = code, ["message"] = "Server error"
            }.ToString());
        }
    }
}
