using System.Collections.Generic;
using CNNFeatureMaps.Interaction;
using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class MapDisplayController : MonoBehaviour
    {
        [SerializeField] private Renderer[] _outputs;
        [SerializeField] private Transform[] _layerRoots;
        [SerializeField] private Transform[] _outputRoots;
        private MapCell[] _nodes;
        private MapPanel[] _panels;
        private MapLayer[] _layers;
        private int _spreadLayer;

        public IReadOnlyList<MapCell> Cells => _nodes;
        public int NextSpreadLayer => _spreadLayer;

        public void Initialize(CNNFeatureMapLabManager scene, InteractionController interaction)
        {
            _nodes = GetComponentsInChildren<MapCell>(true);
            _panels = GetComponentsInChildren<MapPanel>(true);
            foreach (MapPanel panel in _panels)
            {
                panel.Initialize(scene);
            }
            _layers = new MapLayer[_layerRoots.Length];
            for (int index = 0; index < _layerRoots.Length; index++)
            {
                _layers[index] = _layerRoots[index].GetComponent<MapLayer>();
                _layers[index].Initialize(interaction);
            }
        }

        public void RefreshMaps(ConvolutionSimulation data)
        {
            foreach (MapCell node in _nodes)
            {
                node.RefreshAppearance(data);
            }
        }

        public void UpdateStepVisibility(LessonStep step, int channel)
        {
            bool showAllLayers = step == LessonStep.TraceReceptiveField || step == LessonStep.EditInput;
            for (int index = 0; index < _layerRoots.Length; index++)
            {
                _layerRoots[index].gameObject.SetActive(index == 0 || showAllLayers);
            }
            foreach (MapPanel panel in _panels)
            {
                if (panel.Layer == 0)
                {
                    panel.gameObject.SetActive(step != LessonStep.BuildFeatureMap || panel.Channel == channel);
                }
            }
            foreach (Transform output in _outputRoots)
            {
                output.gameObject.SetActive(step == LessonStep.EditInput);
            }
        }

        public void ApplyStepLayout(bool compareFilters)
        {
            _spreadLayer = 0;
            for (int index = 0; index < _layers.Length; index++)
            {
                bool expand = compareFilters && index == 0;
                if (_layers[index].IsExpanded != expand)
                {
                    _layers[index].ToggleSpread();
                }
            }
        }

        public void SpreadNextLayer(bool firstLayerOnly)
        {
            for (int index = 0; index < _layers.Length; index++)
            {
                bool expand = index == _spreadLayer;
                if (_layers[index].gameObject.activeInHierarchy && _layers[index].IsExpanded != expand)
                {
                    _layers[index].ToggleSpread();
                }
            }
            _spreadLayer = (_spreadLayer + 1) % (firstLayerOnly ? 1 : _layers.Length);
        }

        public void ResetLayout()
        {
            _spreadLayer = 0;
            foreach (MapLayer layer in _layers)
            {
                layer.Collapse();
            }
        }

        public void UpdateLearningStates(LessonStep step, int channel, int row, int column, bool[,,] visited)
        {
            bool isFilterStep = step == LessonStep.BuildFeatureMap || step == LessonStep.CompareFilters;
            foreach (MapCell node in _nodes)
            {
                bool computed = step != LessonStep.BuildFeatureMap || node.Layer != 0 || visited[node.Channel, node.Row, node.Column];
                bool spotlight = isFilterStep && node.Layer == 0 && node.Channel == channel && node.Row == row && node.Column == column;
                node.SetLearningState(computed, spotlight);
            }
        }

        public void RefreshAppearance(ConvolutionSimulation data, bool hasSelection)
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
