using UnityEngine;

namespace CNNFeatureMaps
{
    public sealed class CNNFeatureMapPanel : MonoBehaviour
    {
        [SerializeField] private Renderer _surface;
        [SerializeField] private int _layer = -1;
        [SerializeField] private int _channel;
        [SerializeField] private CNNFeatureMapNode[] _nodes;
        private bool _extracted;
        public bool IsExtracted => _extracted;
        public int Layer => _layer;
        public int Channel => _channel;
        public System.Collections.Generic.IReadOnlyList<CNNFeatureMapNode> Nodes => _nodes;

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
