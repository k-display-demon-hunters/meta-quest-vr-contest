using System.Collections.Generic;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Dataset
{
    public sealed class ModelDatasetManager : MonoBehaviour
    {
        [SerializeField] private ModelDatasetController _controller;

        public bool HasPreparationError => _controller.HasPreparationError;
        public IReadOnlyList<DatasetSample> Samples => _controller.Samples;
        public TrainingData TrainingData => _controller.TrainingData;

        public void Initialize()
        {
            _controller.Initialize();
        }

        public bool TryGetInferenceInput(string imageId, out float[] input)
        {
            return _controller.TryGetInferenceInput(imageId, out input);
        }
    }
}
