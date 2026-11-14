using System;
using System.Collections.Generic;
using System.IO;
using ModelDesignScene.Model;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Dataset
{
    public sealed class ModelDatasetController : MonoBehaviour
    {
        [SerializeField] private TextAsset _trainingDataset;
        [SerializeField] private TextAsset _testDataset;

        private MnistDataset _dataset;
        private bool _initialized;

        internal bool HasPreparationError { get; private set; }
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
                List<DatasetSample> samples = new List<DatasetSample>();
                foreach (string id in dataset.TestImageIds)
                {
                    dataset.TryGetTestImage(id, out float[] pixels, out int label);
                    Texture2D image = new Texture2D(LayerLimits.InputWidth, LayerLimits.InputHeight,
                        TextureFormat.RGB24, false);
                    image.name = id;
                    image.filterMode = FilterMode.Point;
                    image.wrapMode = TextureWrapMode.Clamp;
                    Color[] colors = new Color[LayerLimits.InputWidth * LayerLimits.InputHeight];
                    for (int y = 0; y < LayerLimits.InputHeight; y++)
                    {
                        for (int x = 0; x < LayerLimits.InputWidth; x++)
                        {
                            int source = (y * LayerLimits.InputWidth + x) * LayerLimits.InputChannels;
                            int destination = (LayerLimits.InputHeight - 1 - y) * LayerLimits.InputWidth + x;
                            colors[destination] = new Color(pixels[source], pixels[source + 1], pixels[source + 2]);
                        }
                    }
                    image.SetPixels(colors);
                    image.Apply(false, true);
                    samples.Add(new DatasetSample(id, image, label));
                }
                _dataset = dataset;
                Samples = samples.AsReadOnly();
            }
            catch (InvalidDataException exception)
            {
                HasPreparationError = true;
                Debug.LogError($"[ERR-DATA] {exception}");
            }
        }

        private void OnDestroy()
        {
            foreach (DatasetSample sample in Samples)
            {
                Destroy(sample.Image);
            }
        }

        internal bool TryGetInferenceInput(string imageId, out float[] input)
        {
            return _dataset.TryGetTestImage(imageId, out input, out _);
        }
    }
}
