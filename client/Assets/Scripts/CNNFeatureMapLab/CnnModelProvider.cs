using UnityEngine;
using UnityEngine.Serialization;

namespace FeatureMapCorridor
{
    public sealed class CnnModelProvider : MonoBehaviour
    {
        [FormerlySerializedAs("_modelPrefab")]
        [SerializeField] private MonoBehaviour _modelInstance;
        public ICnnModel Model { get; private set; }

        internal void Initialize() => Model = (ICnnModel)_modelInstance;
    }
}
