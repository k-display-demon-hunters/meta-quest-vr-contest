using System;
using System.Collections.Generic;
using System.Threading;

namespace CNNTraining
{
    internal sealed class CnnTrainer
    {
        private readonly CnnModel _model;
        private readonly float[] _sample;

        internal CnnTrainer(CnnModel model)
        {
            _model = model;
            _sample = new float[model.InputSize];
        }

        internal TrainingResult Train(TrainingData data, CnnTrainingOptions options,
            Action<TrainingProgress> progress, CancellationToken cancellation)
        {
            int[] order = new int[data.Training.Count];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }
            Random random = new Random(options.Seed);
            AdamOptimizer optimizer = new AdamOptimizer(options.LearningRate);
            Dictionary<string, double> logs = null;
            for (int epoch = 1; epoch <= options.Epochs; epoch++)
            {
                cancellation.ThrowIfCancellationRequested();
                Shuffle(order, random);
                double loss = 0;
                int correct = 0;
                for (int batchStart = 0; batchStart < order.Length; batchStart += options.BatchSize)
                {
                    int batchCount = Math.Min(options.BatchSize, order.Length - batchStart);
                    for (int index = batchStart; index < batchStart + batchCount; index++)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        int label = data.Training.CopySample(order[index], _sample);
                        _model.Forward(_sample, cancellation);
                        loss += ClassificationLoss.Calculate(_model.Logits, label);
                        if (ClassificationLoss.Predict(_model.Output) == label)
                        {
                            correct++;
                        }
                        _model.Backward(label, cancellation);
                    }
                    cancellation.ThrowIfCancellationRequested();
                    optimizer.BeginStep(batchCount);
                    _model.Update(optimizer);
                }
                Dictionary<string, double> validation = Evaluate(data.Validation, cancellation);
                logs = new Dictionary<string, double>
                {
                    ["loss"] = loss / order.Length,
                    ["accuracy"] = (double)correct / order.Length,
                    ["val_loss"] = validation["loss"],
                    ["val_accuracy"] = validation["accuracy"]
                };
                cancellation.ThrowIfCancellationRequested();
                progress?.Invoke(new TrainingProgress(epoch, options.Epochs, logs));
            }
            Dictionary<string, double> evaluation = Evaluate(data.Test, cancellation);
            cancellation.ThrowIfCancellationRequested();
            return new TrainingResult(options.Epochs, logs, evaluation);
        }

        private Dictionary<string, double> Evaluate(TrainingDataset dataset, CancellationToken cancellation)
        {
            double loss = 0;
            int correct = 0;
            for (int i = 0; i < dataset.Count; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                int label = dataset.CopySample(i, _sample);
                _model.Forward(_sample, cancellation);
                loss += ClassificationLoss.Calculate(_model.Logits, label);
                if (ClassificationLoss.Predict(_model.Output) == label)
                {
                    correct++;
                }
            }
            return new Dictionary<string, double>
            {
                ["loss"] = loss / dataset.Count,
                ["accuracy"] = (double)correct / dataset.Count
            };
        }

        private static void Shuffle(int[] order, Random random)
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                int value = order[i];
                order[i] = order[other];
                order[other] = value;
            }
        }
    }
}
