using CNNFeatureMaps.Lesson;
using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class MapDisplayController
    {
        private readonly MapNode[] _nodes;
        private readonly Renderer[] _outputs;

        public MapDisplayController(MapNode[] nodes, Renderer[] outputs)
        {
            _nodes = nodes;
            _outputs = outputs;
        }

        public void RefreshMaps(ExampleData data)
        {
            foreach (MapNode node in _nodes)
            {
                node.RefreshAppearance(data);
            }
        }

        public void UpdateStepVisibility(LessonStep step, int channel,
            Transform[] layerRoots, Transform[] outputRoots, MapPanel[] panels)
        {
            bool showAllLayers = step == LessonStep.TraceReceptiveField || step == LessonStep.EditInput;
            for (int index = 0; index < layerRoots.Length; index++)
            {
                layerRoots[index].gameObject.SetActive(index == 0 || showAllLayers);
            }
            foreach (MapPanel panel in panels)
            {
                if (panel.Layer == 0)
                {
                    panel.gameObject.SetActive(step != LessonStep.BuildFeatureMap || panel.Channel == channel);
                }
            }
            foreach (Transform output in outputRoots)
            {
                output.gameObject.SetActive(step == LessonStep.EditInput);
            }
        }

        public void UpdateLearningStates(LessonStep step, int channel, int row, int column, bool[,,] visited)
        {
            bool isFilterStep = step == LessonStep.BuildFeatureMap || step == LessonStep.CompareFilters;
            foreach (MapNode node in _nodes)
            {
                bool computed = step != LessonStep.BuildFeatureMap || node.Layer != 0 || visited[node.Channel, node.Row, node.Column];
                bool spotlight = isFilterStep && node.Layer == 0 && node.Channel == channel && node.Row == row && node.Column == column;
                node.SetLearningState(computed, spotlight);
            }
        }

        public void RefreshAppearance(ExampleData data, bool hasSelection)
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
