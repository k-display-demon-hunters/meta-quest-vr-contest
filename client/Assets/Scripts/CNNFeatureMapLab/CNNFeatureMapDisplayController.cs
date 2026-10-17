using UnityEngine;

namespace CNNFeatureMaps
{
    public sealed class CNNFeatureMapDisplayController
    {
        private readonly CNNFeatureMapNode[] _nodes;
        private readonly Renderer[] _outputs;

        public CNNFeatureMapDisplayController(CNNFeatureMapNode[] nodes, Renderer[] outputs)
        {
            _nodes = nodes;
            _outputs = outputs;
        }

        public void RefreshMaps(CNNFeatureMapExampleData data)
        {
            foreach (CNNFeatureMapNode node in _nodes)
            {
                node.RefreshAppearance(data);
            }
        }

        public void UpdateLearningStates(int step, int channel, int row, int column, bool[,,] visited)
        {
            foreach (CNNFeatureMapNode node in _nodes)
            {
                bool computed = step != 0 || node.Layer != 0 || visited[node.Channel, node.Row, node.Column];
                bool spotlight = step < 2 && node.Layer == 0 && node.Channel == channel && node.Row == row && node.Column == column;
                node.SetLearningState(computed, spotlight);
            }
        }

        public void RefreshAppearance(CNNFeatureMapExampleData data, bool hasSelection)
        {
            RefreshMaps(data);
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            for (int index = 0; index < _outputs.Length; index++)
            {
                float value = data.ReadOutput(index);
                _outputs[index].transform.localScale = new Vector3(.12f, .1f + value * .5f, .06f);
                properties.SetColor("_BaseColor", hasSelection ? new Color(1f, .83f, .22f) : new Color(.15f, .35f + value * .5f, .55f));
                _outputs[index].SetPropertyBlock(properties);
            }
        }
    }
}
