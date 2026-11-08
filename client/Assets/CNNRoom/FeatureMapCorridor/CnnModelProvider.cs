using UnityEngine;

namespace FeatureMapCorridor
{
    public sealed class CnnModelProvider : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _modelPrefab;
        public ICnnModel Model { get; private set; }

        internal void Initialize() => Model = (ICnnModel)Instantiate(_modelPrefab, transform);
    }
}
