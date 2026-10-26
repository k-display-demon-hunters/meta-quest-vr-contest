using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ModelDesignScene.Execution.Engine.Tests
{
    public sealed class CnnTrainingSessionTests
    {
        [Test]
        public async Task TinyCnnLearnsAndReusesSessionForInferenceAndRetraining()
        {
            ModelExecutionDefinition definition = CreateModel();
            CnnTrainingOptions options = new CnnTrainingOptions(
                epochs: 40, batchSize: 4, learningRate: 0.03f, seed: 42);
            using (CnnTrainingSession session = new CnnTrainingSession())
            {
                double firstValidationLoss = double.NaN;
                TrainingResult result = await WithDeadline(TrainAsync(session,
                    definition, CreateData(), options, progress =>
                    {
                        if (progress.Epoch == 1)
                        {
                            firstValidationLoss = progress.Logs["val_loss"];
                        }
                    }));

                Assert.IsTrue(session.HasTrainedModel);
                Assert.IsFalse(session.IsBusy);
                Assert.AreEqual(options.Epochs, result.Epoch);
                Assert.Less(result.Logs["val_loss"], firstValidationLoss,
                    "The whole CNN must improve on the held-out synthetic examples.");
                Assert.AreEqual(1d, result.Evaluation["accuracy"],
                    "The two constant images are deliberately trivial to distinguish.");

                InferenceResult dark = await WithDeadline(session.InferAsync(CreateImage(0f), "dark"));
                InferenceResult bright = await WithDeadline(session.InferAsync(CreateImage(1f), "bright"));
                InferenceResult repeated = await WithDeadline(session.InferAsync(CreateImage(0f), "dark-again"));
                Assert.AreEqual("dark", dark.ImageId);
                Assert.AreEqual("bright", bright.ImageId);
                Assert.AreEqual("dark-again", repeated.ImageId);
                Assert.AreEqual(10, dark.Output.Count);
                Assert.AreEqual(0, PredictedClass(dark));
                Assert.AreEqual(1, PredictedClass(bright));
                CollectionAssert.AreEqual(dark.Output, repeated.Output,
                    "Inference on another image must not modify learned weights.");

                // Reversed labels make reuse observable: retaining the previous model cannot pass.
                TrainingResult retrained = await WithDeadline(TrainAsync(session,
                    definition, CreateData(reverseLabels: true), options));
                Assert.AreEqual(1d, retrained.Evaluation["accuracy"]);
                InferenceResult newDark = await WithDeadline(session.InferAsync(CreateImage(0f), "new-dark"));
                InferenceResult newBright = await WithDeadline(session.InferAsync(CreateImage(1f), "new-bright"));
                Assert.AreEqual(1, PredictedClass(newDark));
                Assert.AreEqual(0, PredictedClass(newBright));
            }
        }

        [Test]
        public async Task BusySessionRejectsConcurrentRequestsAndCancellationAllowsReuse()
        {
            ModelExecutionDefinition definition = CreateModel();
            TrainingData data = CreateData();
            CnnTrainingOptions options = new CnnTrainingOptions(epochs: 3, batchSize: 4);
            TaskCompletionSource<bool> progressEntered = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using (ManualResetEventSlim releaseProgress = new ManualResetEventSlim(false))
            using (CnnTrainingSession session = new CnnTrainingSession())
            {
                Task<TrainingResult> training = TrainAsync(session, definition, data, options, progress =>
                {
                    progressEntered.TrySetResult(true);
                    if (!releaseProgress.Wait(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("The test did not release the progress callback.");
                    }
                });
                try
                {
                    await WithDeadline(progressEntered.Task);
                    Assert.IsTrue(session.IsBusy);
                    Assert.IsFalse(session.HasTrainedModel);
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        TrainAsync(session, definition, data, options);
                    });
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        session.InferAsync(CreateImage(0f), "busy");
                    });

                    // The worker is paused inside progress, so this cannot race successful completion.
                    session.Cancel();
                    releaseProgress.Set();
                    bool cancelled = false;
                    try
                    {
                        await WithDeadline(training);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                    }
                    Assert.IsTrue(cancelled, "Cancellation must not return a completed training result.");
                    Assert.IsFalse(session.IsBusy);
                    Assert.IsFalse(session.HasTrainedModel, "Partially trained weights cannot be used for inference.");
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        session.InferAsync(CreateImage(0f), "cancelled");
                    });

                    TrainingResult reused = await WithDeadline(TrainAsync(session,
                        definition, data, new CnnTrainingOptions(epochs: 1, batchSize: 4)));
                    Assert.AreEqual(1, reused.Epoch);
                    Assert.IsTrue(session.HasTrainedModel);
                    Assert.IsFalse(session.IsBusy);
                }
                finally
                {
                    session.Cancel();
                    releaseProgress.Set();
                    try
                    {
                        await WithDeadline(training);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ensure the worker has exited before disposing the callback gate.
                    }
                }
            }
        }

        private static Task<TrainingResult> TrainAsync(CnnTrainingSession session,
            ModelExecutionDefinition definition, TrainingData data, CnnTrainingOptions options,
            Action<TrainingProgress> progress = null)
        {
            CnnTrainingRequest request = new CnnTrainingRequest(definition, data, options);
            return session.TrainAsync(request, progress);
        }

        private static ModelExecutionDefinition CreateModel()
        {
            return new ModelExecutionDefinition(4, 4, 1, new[]
            {
                new CnnLayerDefinition(LayerKind.Conv2D, 2, 1, 1, CnnActivation.Tanh),
                new CnnLayerDefinition(LayerKind.MaxPooling2D, height: 2, width: 2),
                new CnnLayerDefinition(LayerKind.Flatten),
                new CnnLayerDefinition(LayerKind.Dense, 10, activation: CnnActivation.Softmax)
            });
        }

        private static TrainingData CreateData(bool reverseLabels = false)
        {
            return new TrainingData(CreateSplit(8, reverseLabels),
                CreateSplit(2, reverseLabels), CreateSplit(2, reverseLabels));
        }

        private static TrainingDataset CreateSplit(int count, bool reverseLabels)
        {
            float[] images = new float[count * 16];
            int[] labels = new int[count];
            for (int row = 0; row < count; row++)
            {
                int value = row % 2;
                labels[row] = reverseLabels ? 1 - value : value;
                for (int pixel = 0; pixel < 16; pixel++)
                {
                    images[row * 16 + pixel] = value;
                }
            }
            return new TrainingDataset(images, labels, 16);
        }

        private static float[] CreateImage(float value)
        {
            float[] pixels = new float[16];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = value;
            }
            return pixels;
        }

        private static int PredictedClass(InferenceResult result)
        {
            int predicted = 0;
            for (int index = 1; index < result.Output.Count; index++)
            {
                if (result.Output[index] > result.Output[predicted])
                {
                    predicted = index;
                }
            }
            return predicted;
        }

        private static async Task<T> WithDeadline<T>(Task<T> operation)
        {
            Task completed = await Task.WhenAny(operation, Task.Delay(TimeSpan.FromSeconds(10)));
            if (!ReferenceEquals(completed, operation))
            {
                throw new TimeoutException("The tiny session operation did not finish within 10 seconds.");
            }
            return await operation;
        }
    }
}
