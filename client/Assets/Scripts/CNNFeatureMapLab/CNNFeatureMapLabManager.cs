using CNNFeatureMaps.FeatureMaps;
using CNNFeatureMaps.Interaction;
using CNNFeatureMaps.Lesson;
using UnityEngine;

namespace CNNFeatureMaps
{
    [DefaultExecutionOrder(-100)]
    public sealed class CNNFeatureMapLabManager : MonoBehaviour
    {
        [SerializeField] private Texture2D _inputImage;
        [SerializeField] private Renderer[] _outputs;
        private MapNode[] _nodes;
        private MapPanel[] _panels;
        private readonly InteractionController _interaction = new InteractionController();
        private MapDisplayController _display;
        private GrabTarget[] _targets;
        private MapLayer[] _layers;
        private ExampleData _data;
        private bool _ready;
        private LessonGuide _guide;
        private FilterController _filter;
        private readonly bool[,,] _visited = new bool[2, 8, 8];
        private LessonStep _step = LessonStep.BuildFeatureMap;
        private int _channel;
        private int _spreadLayer;
        private int _editCount;
        private bool _lastStepSkipped;
        public bool HasHeldTargets => _interaction.HasHeldTargets;
        public ExampleData Data => _data;
        public bool ReadyForModelDesign => _ready && _guide != null && _step == LessonStep.EditInput
            && (_editCount > 0 || _lastStepSkipped) && !HasHeldTargets;
        private bool IsFilterStep => _step == LessonStep.BuildFeatureMap || _step == LessonStep.CompareFilters;

        private void Awake()
        {
            // Prefab parts own their local references; the workspace supplies session context.
            _nodes = GetComponentsInChildren<MapNode>(true);
            _panels = GetComponentsInChildren<MapPanel>(true);
            _targets = GetComponentsInChildren<GrabTarget>(true);
            _layers = GetComponentsInChildren<MapLayer>(true);
            _display = new MapDisplayController(_nodes, _outputs);
            foreach (MapPanel panel in _panels)
            {
                panel.Initialize(this);
            }
            foreach (GrabTarget target in _targets)
            {
                target.Initialize(this);
            }
            foreach (MapLayer layer in _layers)
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
            _data = new ExampleData(_inputImage);
            _ready = true;
            _guide = GetComponent<LessonGuide>();
            RefreshAppearance(false);
            if (_guide != null)
            {
                _guide.Initialize(this);
                _filter = new FilterController(transform, _guide.InputRoot,
                    _guide.Filter, _guide.ToolMaterial, _nodes);
                ApplyStep();
            }
        }

        private void LateUpdate()
        {
            if (!_ready || _filter == null || !IsFilterStep)
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
            if (_guide == null)
            {
                SelectReceptiveField(row, column);
                return;
            }
            switch (_step)
            {
                case LessonStep.BuildFeatureMap:
                case LessonStep.CompareFilters:
                    SelectFilterPosition(row, column);
                    break;
                case LessonStep.TraceReceptiveField:
                    SelectReceptiveField(row, column);
                    break;
                case LessonStep.EditInput:
                    EditInputPixel(row, column);
                    break;
            }
        }

        public void Perform(LessonAction action)
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
                case LessonAction.Previous:
                    MoveToPreviousStep();
                    break;
                case LessonAction.Next:
                    MoveToNextStep();
                    break;
                case LessonAction.Scan:
                    _filter.ToggleScanning();
                    UpdateButtons();
                    break;
                case LessonAction.Filter:
                    _channel = 1 - _channel;
                    ApplyStep();
                    break;
                case LessonAction.Spread:
                    SpreadNextLayer();
                    break;
                case LessonAction.Restore:
                    RestoreLesson();
                    break;
                case LessonAction.Enlarge:
                case LessonAction.Shrink:
                    float multiplier = action == LessonAction.Enlarge ? 1.2f : 1f / 1.2f;
                    if (!_interaction.ScaleSelectedTarget(multiplier))
                    {
                        _guide.ShowSelectScaleTarget();
                    }
                    break;
            }
        }

        public bool CanGrab(GrabTarget candidate)
        {
            return _interaction.CanGrab(candidate);
        }

        public bool HasHeldDescendant(Transform ancestor)
        {
            return _interaction.HasHeldDescendant(ancestor);
        }

        public void HoldTarget(GrabTarget target)
        {
            _interaction.HoldTarget(target);
        }

        public void ReleaseTarget(GrabTarget target)
        {
            _interaction.ReleaseTarget(target);
        }

        public void SelectScaleTarget(GrabTarget target)
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
            foreach (MapLayer layer in _layers)
            {
                layer.Collapse();
            }
            foreach (GrabTarget target in _targets)
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

        public void RefreshMaps()
        {
            if (_ready)
            {
                _display.RefreshMaps(_data);
            }
        }

        private void MoveToPreviousStep()
        {
            switch (_step)
            {
                case LessonStep.CompareFilters:
                    _step = LessonStep.BuildFeatureMap;
                    break;
                case LessonStep.TraceReceptiveField:
                    _step = LessonStep.CompareFilters;
                    break;
                case LessonStep.EditInput:
                    _step = LessonStep.TraceReceptiveField;
                    break;
            }
            ApplyStep();
        }

        private void MoveToNextStep()
        {
            switch (_step)
            {
                case LessonStep.BuildFeatureMap:
                    _step = LessonStep.CompareFilters;
                    break;
                case LessonStep.CompareFilters:
                    _step = LessonStep.TraceReceptiveField;
                    break;
                case LessonStep.TraceReceptiveField:
                    _step = LessonStep.EditInput;
                    break;
                case LessonStep.EditInput:
                    _lastStepSkipped = _editCount == 0;
                    _guide.ShowStepFinished(_lastStepSkipped);
                    return;
            }
            ApplyStep();
        }

        private void ApplyStep()
        {
            _filter.StopScanning();
            _spreadLayer = 0;
            foreach (MapLayer layer in _layers)
            {
                if (layer.IsExpanded)
                {
                    layer.ToggleSpread();
                }
            }
            _data.SelectPixel(-1, -1);
            _display.UpdateStepVisibility(_step, _channel, _guide.LayerRoots, _guide.OutputRoots, _panels);
            _guide.ShowStep(_step, _channel);
            _filter.SetVisible(IsFilterStep);
            if (_step == LessonStep.CompareFilters && !_layers[0].IsExpanded)
            {
                _layers[0].ToggleSpread();
            }
            UpdateNodeStates();
            RefreshAppearance(false);
            if (IsFilterStep)
            {
                ApplyPatch();
            }
            UpdateButtons();
        }

        // 1·2단계는 같은 필터 위치 선택과 방문 기록을 사용한다.
        private void SelectFilterPosition(int row, int column)
        {
            _filter.StopScanning();
            _filter.SelectPosition(row, column);
            UpdateButtons();
            ApplyPatch();
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

        private void SelectReceptiveField(int row, int column)
        {
            if (_guide != null)
            {
                int affectedRows = Mathf.Min(7, row + 1) - Mathf.Max(0, row - 1) + 1;
                int affectedColumns = Mathf.Min(7, column + 1) - Mathf.Max(0, column - 1) + 1;
                _guide.ShowAffectedPixel(row, column, affectedRows * affectedColumns);
            }
            _data.SelectPixel(row, column);
            RefreshAppearance(true);
        }

        private void EditInputPixel(int row, int column)
        {
            _data.ToggleInput(row, column);
            _data.SelectPixel(row, column);
            _editCount++;
            RefreshAppearance(false);
            _guide.ShowInputEdited(_editCount);
        }

        private void SpreadNextLayer()
        {
            for (int index = 0; index < _layers.Length; index++)
            {
                bool expand = index == _spreadLayer;
                if (_layers[index].gameObject.activeInHierarchy && _layers[index].IsExpanded != expand)
                {
                    _layers[index].ToggleSpread();
                }
            }
            _spreadLayer = (_spreadLayer + 1) % (_step == LessonStep.CompareFilters ? 1 : _layers.Length);
            UpdateButtons();
        }

        private void RestoreLesson()
        {
            System.Array.Clear(_visited, 0, _visited.Length);
            _editCount = 0;
            _lastStepSkipped = false;
            _data.RestoreInput(_inputImage);
            ResetLayout();
        }

        private void UpdateNodeStates()
        {
            _display.UpdateLearningStates(_step, _channel, _filter.Row, _filter.Column, _visited);
        }

        private void UpdateButtons()
        {
            _guide.UpdateButtons(_step, _spreadLayer, _filter.IsScanning);
        }

        private void RefreshAppearance(bool hasSelection)
        {
            if (_ready)
            {
                _display.RefreshAppearance(_data, hasSelection);
            }
        }
    }
}
