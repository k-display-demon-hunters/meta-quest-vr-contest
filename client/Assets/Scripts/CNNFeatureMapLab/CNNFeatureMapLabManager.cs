using UnityEngine;

namespace CNNFeatureMaps
{
    [DefaultExecutionOrder(-100)]
    public sealed class CNNFeatureMapLabManager : MonoBehaviour
    {
        [SerializeField] private Texture2D _inputImage;
        [SerializeField] private Renderer[] _outputs;
        private CNNFeatureMapNode[] _nodes;
        private readonly CNNFeatureMapInteractionController _interaction = new CNNFeatureMapInteractionController();
        private CNNFeatureMapDisplayController _display;
        private CNNFeatureMapGrabTarget[] _targets;
        private CNNFeatureMapLayer[] _layers;
        private CNNFeatureMapExampleData _data;
        private bool _ready;
        private CNNFeatureMapGuide _guide;
        private CNNFeatureMapFilterController _filter;
        private readonly bool[,,] _visited = new bool[2, 8, 8];
        private int _step;
        private int _channel;
        private int _spreadLayer;
        private int _editCount;
        private bool _lastStepSkipped;
        public bool HasHeldTargets => _interaction.HasHeldTargets;
        public CNNFeatureMapExampleData Data => _data;
        public bool ReadyForModelDesign => _ready && _guide != null && _step == 3 && (_editCount > 0 || _lastStepSkipped) && !HasHeldTargets;

        private void Awake()
        {
            // Prefab parts own their local references; the workspace supplies session context.
            _nodes = GetComponentsInChildren<CNNFeatureMapNode>(true);
            _targets = GetComponentsInChildren<CNNFeatureMapGrabTarget>(true);
            _layers = GetComponentsInChildren<CNNFeatureMapLayer>(true);
            _display = new CNNFeatureMapDisplayController(_nodes, _outputs);
            foreach (CNNFeatureMapPanel panel in GetComponentsInChildren<CNNFeatureMapPanel>(true))
            {
                panel.Initialize(this);
            }
            foreach (CNNFeatureMapGrabTarget target in _targets)
            {
                target.Initialize(this);
            }
            foreach (CNNFeatureMapLayer layer in _layers)
            {
                layer.Initialize(this);
            }
        }

        private void Start()
        {
            if (_inputImage == null || !_inputImage.isReadable || _inputImage.width != 8 || _inputImage.height != 8)
            {
                Debug.LogError("CNNFeatureMapLabManager requires the readable 8 x 8 sample image.", this);
                return;
            }
            _data = new CNNFeatureMapExampleData(_inputImage);
            _ready = true;
            _guide = GetComponent<CNNFeatureMapGuide>();
            RefreshAppearance(false);
            if (_guide != null)
            {
                _guide.Initialize(this);
                _filter = new CNNFeatureMapFilterController(transform, _guide.InputRoot,
                    _guide.Filter, _guide.ToolMaterial, _nodes);
                ApplyStep();
            }
        }

        private void LateUpdate()
        {
            if (!_ready || _filter == null || _step >= 2)
            {
                return;
            }
            bool wasScanning = _filter.IsScanning;
            if (_filter.UpdatePosition())
            {
                ApplyPatch();
            }
            if (wasScanning != _filter.IsScanning)
            {
                UpdateButtons();
            }
            _filter.UpdateConnection(_channel);
        }

        public void RefreshLearningAppearance()
        {
            RefreshAppearance(false);
        }

        public void SelectPixel(int row, int column)
        {
            if (!_ready)
            {
                return;
            }
            if (_guide != null && _step < 2)
            {
                _filter.StopScanning();
                _filter.SelectPosition(row, column);
                UpdateButtons();
                ApplyPatch();
                return;
            }
            if (_guide != null && _step == 3)
            {
                _data.ToggleInput(row, column);
                _data.SelectPixel(row, column);
                _editCount++;
                RefreshAppearance(false);
                _guide.ShowInputEdited(_editCount);
                return;
            }
            if (_guide != null)
            {
                int affectedRows = Mathf.Min(7, row + 1) - Mathf.Max(0, row - 1) + 1;
                int affectedColumns = Mathf.Min(7, column + 1) - Mathf.Max(0, column - 1) + 1;
                _guide.ShowAffectedPixel(row, column, affectedRows * affectedColumns);
            }
            _data.SelectPixel(row, column);
            RefreshAppearance(true);
        }

        public void Perform(CNNFeatureMapAction action)
        {
            if (!_ready || _guide == null)
            {
                return;
            }
            if (HasHeldTargets)
            {
                _guide.ShowReleaseBeforeAction();
                return;
            }
            switch (action)
            {
                case CNNFeatureMapAction.Previous:
                    _step = Mathf.Max(0, _step - 1);
                    ApplyStep();
                    break;
                case CNNFeatureMapAction.Next:
                    if (_step == 3)
                    {
                        _lastStepSkipped = _editCount == 0;
                        _guide.ShowStepFinished(_lastStepSkipped);
                        break;
                    }
                    _step = Mathf.Min(3, _step + 1);
                    ApplyStep();
                    break;
                case CNNFeatureMapAction.Scan:
                    _filter.ToggleScanning();
                    UpdateButtons();
                    break;
                case CNNFeatureMapAction.Filter:
                    _channel = 1 - _channel;
                    ApplyStep();
                    break;
                case CNNFeatureMapAction.Spread:
                    for (int index = 0; index < _layers.Length; index++)
                    {
                        bool expand = index == _spreadLayer;
                        if (_layers[index].gameObject.activeInHierarchy && _layers[index].IsExpanded != expand)
                        {
                            _layers[index].ToggleSpread();
                        }
                    }
                    _spreadLayer = (_spreadLayer + 1) % (_step == 1 ? 1 : _layers.Length);
                    UpdateButtons();
                    break;
                case CNNFeatureMapAction.Restore:
                    System.Array.Clear(_visited, 0, _visited.Length);
                    _editCount = 0;
                    _lastStepSkipped = false;
                    _data.RestoreInput(_inputImage);
                    ResetLayout();
                    break;
                case CNNFeatureMapAction.Enlarge:
                case CNNFeatureMapAction.Shrink:
                    float multiplier = action == CNNFeatureMapAction.Enlarge ? 1.2f : 1f / 1.2f;
                    if (!_interaction.ScaleSelectedTarget(multiplier))
                    {
                        _guide.ShowSelectScaleTarget();
                    }
                    break;
            }
        }

        public bool CanGrab(CNNFeatureMapGrabTarget candidate)
        {
            return _interaction.CanGrab(candidate);
        }

        public bool HasHeldDescendant(Transform ancestor)
        {
            return _interaction.HasHeldDescendant(ancestor);
        }

        public void HoldTarget(CNNFeatureMapGrabTarget target)
        {
            _interaction.HoldTarget(target);
        }

        public void ReleaseTarget(CNNFeatureMapGrabTarget target)
        {
            _interaction.ReleaseTarget(target);
        }

        public void SelectScaleTarget(CNNFeatureMapGrabTarget target)
        {
            _interaction.SelectScaleTarget(target);
        }

        public bool ScaleSelectedTarget(float multiplier)
        {
            return _interaction.ScaleSelectedTarget(multiplier);
        }

        public void ResetLayout()
        {
            if (!_ready || _interaction.HasHeldTargets)
            {
                return;
            }
            foreach (CNNFeatureMapLayer layer in _layers)
            {
                layer.Collapse();
            }
            foreach (CNNFeatureMapGrabTarget target in _targets)
            {
                target.ResetPose();
            }
            _data.SelectPixel(-1, -1);
            RefreshAppearance(false);
            if (_guide != null)
            {
                ApplyStep();
            }
        }

        private void ApplyStep()
        {
            _filter.StopScanning();
            _spreadLayer = 0;
            foreach (CNNFeatureMapLayer layer in _layers)
            {
                if (layer.IsExpanded)
                {
                    layer.ToggleSpread();
                }
            }
            _data.SelectPixel(-1, -1);
            _guide.ShowStep(_step, _channel);
            _filter.SetVisible(_step < 2);
            if (_step == 1 && !_layers[0].IsExpanded)
            {
                _layers[0].ToggleSpread();
            }
            UpdateNodeStates();
            RefreshAppearance(false);
            if (_step < 2)
            {
                ApplyPatch();
            }
            UpdateButtons();
        }

        private void ApplyPatch()
        {
            int row = _filter.Row;
            int column = _filter.Column;
            _visited[_channel, row, column] = true;
            _data.SelectWindow(row, column);
            UpdateNodeStates();
            RefreshAppearance(false);
            int count = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (_visited[_channel, y, x])
                    {
                        count++;
                    }
                }
            }
            _guide.ShowPatch(row, column, _data.ReadFirstLayerSum(_channel, row, column),
                _data.ReadValue(0, _channel, row, column), count);
        }

        private void UpdateNodeStates()
        {
            _display.UpdateLearningStates(_step, _channel, _filter.Row, _filter.Column, _visited);
        }

        private void UpdateButtons()
        {
            _guide.UpdateButtons(_step, _spreadLayer, _filter.IsScanning);
        }

        public void RefreshMaps()
        {
            if (_ready) { _display.RefreshMaps(_data); }
        }

        private void RefreshAppearance(bool hasSelection)
        {
            if (_ready) { _display.RefreshAppearance(_data, hasSelection); }
        }
    }
}
