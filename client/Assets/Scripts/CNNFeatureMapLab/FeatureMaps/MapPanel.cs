using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class MapPanel : MonoBehaviour
    {
        [SerializeField] private Renderer _surface;
        [SerializeField] private int _channel;
        private int _layer = -1;
        private MapCell[] _nodes;
        private bool _extracted;
        public bool IsExtracted => _extracted;
        public int Layer => _layer;
        public int Channel => _channel;
        public System.Collections.Generic.IReadOnlyList<MapCell> Nodes => _nodes;

        public void Initialize()
        {
            MapLayer layer = GetComponentInParent<MapLayer>();
            _layer = layer != null ? layer.Index : -1;
            _nodes = GetComponentsInChildren<MapCell>(true);
            foreach (MapCell node in _nodes)
            {
                node.Initialize(_layer, _channel);
            }
        }

        private void Awake()
        {
            // The map is now made of physical neuron meshes. Its former flat backing
            // stays serialized for scene compatibility but is not shown during Play.
            if (_surface != null)
            {
                _surface.enabled = false;
            }
        }

        public void Extract() => _extracted = true;
        public void ReturnToStack() => _extracted = false;
    }
}
