using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace ModelDesignScene.Execution
{
    [CreateAssetMenu(menuName = "CNN/Local Training Settings")]
    public sealed class CnnTrainingSettings : ScriptableObject
    {
        [SerializeField] private TextAsset _trainingDataset;
        [SerializeField] private TextAsset _testDataset;
        [SerializeField, Range(1, 100)] private int _epochs = 3;
        [SerializeField, Range(1, 256)] private int _batchSize = 16;
        [SerializeField] private float _learningRate = 0.001f;
        [SerializeField] private int _seed = 42;
        [SerializeField, Min(1)] private int _maxParameters = 100000;
        [SerializeField, Min(1)] private int _maxMultiplyAdds = 2000000;
        [SerializeField, Min(1)] private int _maxActivationElements = 32768;
        [SerializeField, Range(3, 32)] private int _maxLayers = 16;

        internal CnnTrainingOptions CreateOptions()
        {
            return new CnnTrainingOptions(_epochs, _batchSize, _learningRate, _seed,
                _maxParameters, _maxMultiplyAdds, _maxActivationElements, _maxLayers);
        }

        // TextAsset access and decoding happen once on the Unity main thread.
        internal MnistDataset LoadDataset()
        {
            return MnistDataset.Load(_trainingDataset, _testDataset);
        }
    }
}
