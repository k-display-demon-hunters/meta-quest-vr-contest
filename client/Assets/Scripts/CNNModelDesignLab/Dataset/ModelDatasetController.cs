using System;
using System.Collections.Generic;
using System.IO;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Dataset
{
    public sealed class ModelDatasetController : MonoBehaviour
    {
        [SerializeField] private TextAsset _trainingDataset;
        [SerializeField] private TextAsset _testDataset;
        [SerializeField] private Texture2D[] _sampleImages;

        private MnistDataset _dataset;
        private bool _initialized;

        internal string PreparationError { get; private set; }
        internal TrainingData TrainingData => _dataset.Data;
        internal IReadOnlyList<DatasetSample> Samples { get; private set; } = Array.Empty<DatasetSample>();

        internal void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            try
            {
                MnistDataset dataset = MnistDataset.Load(_trainingDataset.bytes, _testDataset.bytes);
                List<DatasetSample> samples = new List<DatasetSample>(_sampleImages.Length);
                foreach (Texture2D image in _sampleImages)
                {
                    if (!dataset.TryGetTestImage(image.name, out _, out int label))
                    {
                        throw new InvalidDataException("내장 데이터에 해당 샘플이 없습니다: " + image.name);
                    }
                    samples.Add(new DatasetSample(image.name, image, label));
                }
                _dataset = dataset;
                Samples = samples.AsReadOnly();
            }
            catch (InvalidDataException exception)
            {
                PreparationError = "MNIST 데이터를 준비하지 못했습니다. " + exception.Message;
            }
        }

        internal bool TryGetInferenceInput(string imageId, out float[] input)
        {
            return _dataset.TryGetTestImage(imageId, out input, out _);
        }
    }
}
