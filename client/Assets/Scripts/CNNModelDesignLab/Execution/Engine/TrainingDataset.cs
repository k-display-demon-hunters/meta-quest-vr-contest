using System;

namespace ModelDesignScene.Execution.Engine
{
    public sealed class TrainingDataset
    {
        private readonly float[] _images;
        private readonly int[] _labels;
        public int Count => _labels.Length;
        public int InputSize { get; }

        public TrainingDataset(float[] images, int[] labels, int inputSize)
        {
            if (images == null || labels == null)
            {
                throw new ArgumentNullException("Dataset arrays are required.");
            }
            if (inputSize < 1 || inputSize > 200000 || labels.Length < 1 || labels.Length > 100000 ||
                (long)labels.Length * inputSize != images.Length || images.Length > 80000000)
            {
                throw new ArgumentException("Invalid dataset dimensions.");
            }
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] < 0 || labels[i] > 9)
                {
                    throw new ArgumentException("Labels must be in [0,9].");
                }
            }
            for (int i = 0; i < images.Length; i++)
            {
                if (float.IsNaN(images[i]) || float.IsInfinity(images[i]) || images[i] < 0 || images[i] > 1)
                {
                    throw new ArgumentException("Input pixels must be finite and normalized to [0,1].");
                }
            }
            _images = (float[])images.Clone();
            _labels = (int[])labels.Clone();
            InputSize = inputSize;
        }

        internal int CopySample(int index, float[] destination)
        {
            Array.Copy(_images, index * InputSize, destination, 0, InputSize);
            return _labels[index];
        }
    }

    public sealed class TrainingData
    {
        public TrainingDataset Training { get; }
        public TrainingDataset Validation { get; }
        public TrainingDataset Test { get; }

        public TrainingData(TrainingDataset training, TrainingDataset validation, TrainingDataset test)
        {
            Training = training ?? throw new ArgumentNullException(nameof(training));
            Validation = validation ?? throw new ArgumentNullException(nameof(validation));
            Test = test ?? throw new ArgumentNullException(nameof(test));
            if (training.InputSize != validation.InputSize || training.InputSize != test.InputSize)
            {
                throw new ArgumentException("All dataset splits must have the same input size.");
            }
            if (ReferenceEquals(training, validation) || ReferenceEquals(training, test) || ReferenceEquals(validation, test))
            {
                throw new ArgumentException("Training, validation and test must be separate datasets.");
            }
        }
    }
}
