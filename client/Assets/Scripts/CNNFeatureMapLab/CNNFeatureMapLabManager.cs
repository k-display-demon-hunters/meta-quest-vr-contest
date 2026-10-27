using CNNFeatureMaps.FeatureMaps;
using CNNFeatureMaps.Interaction;
using CNNFeatureMaps.Lesson;
using UnityEngine;

namespace CNNFeatureMaps
{
    [DefaultExecutionOrder(-100)]
    public sealed class CNNFeatureMapLabManager : MonoBehaviour
    {
        [SerializeField] private InputMapController _inputMap;
        [SerializeField] private MapDisplayController _display;
        [SerializeField] private FilterController _filter;
        [SerializeField] private LessonUIManager _lessonUI;
        private readonly InteractionController _interaction = new InteractionController();
        private ConvolutionSimulation _data;
        private bool _ready;
        private readonly bool[,,] _visited = new bool[2, 8, 8];
        private LessonStep _step = LessonStep.BuildFeatureMap;
        private int _channel;
        private int _editCount;
        private bool _lastStepSkipped;
        public bool HasHeldTargets => _interaction.HasHeldTargets;
        public ConvolutionSimulation Data => _data;
        public bool ReadyForModelDesign => _ready && _step == LessonStep.EditInput
            && (_editCount > 0 || _lastStepSkipped) && !HasHeldTargets;
        private bool IsFilterStep => _step == LessonStep.BuildFeatureMap || _step == LessonStep.CompareFilters;

        private void Awake()
        {
            // Wire interaction before child grab components begin receiving XR events.
            _interaction.Initialize(transform);
            _display.Initialize(this, _interaction);
        }

        private void OnEnable()
        {
            _lessonUI.OnActionRequested += Perform;
        }

        private void OnDisable()
        {
            _lessonUI.OnActionRequested -= Perform;
        }

        private void Start()
        {
            if (_inputMap == null)
            {
                Debug.LogError("CNNFeatureMapLabManager requires an InputMapController.", this);
                return;
            }
            if (!_inputMap.TryReadInput(out float[,] input))
            {
                return;
            }
            _data = new ConvolutionSimulation(input);
            _ready = true;
            RefreshAppearance(false);
            _filter.Initialize(_inputMap.transform, _display.Cells);
            ApplyStep();
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
            if (!_ready)
            {
                return;
            }
            if (HasHeldTargets)
            {
                _lessonUI.ShowReleaseBeforeAction();
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
                    _display.SpreadNextLayer(_step == LessonStep.CompareFilters);
                    UpdateButtons();
                    break;
                case LessonAction.Restore:
                    RestoreLesson();
                    break;
                case LessonAction.Enlarge:
                case LessonAction.Shrink:
                    float multiplier = action == LessonAction.Enlarge ? 1.2f : 1f / 1.2f;
                    if (!_interaction.ScaleSelectedTarget(multiplier))
                    {
                        _lessonUI.ShowSelectScaleTarget();
                    }
                    break;
            }
        }

        public void ResetLayout()
        {
            if (!_ready || _interaction.HasHeldTargets)
            {
                return;
            }
            _display.ResetLayout();
            _interaction.ResetPoses();
            _data.SelectPixel(-1, -1);
            RefreshAppearance(false);
            ApplyStep();
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
                    _lessonUI.ShowStepFinished(_lastStepSkipped);
                    return;
            }
            ApplyStep();
        }

        private void ApplyStep()
        {
            _filter.StopScanning();
            _data.SelectPixel(-1, -1);
            _display.UpdateStepVisibility(_step, _channel);
            _display.ApplyStepLayout(_step == LessonStep.CompareFilters);
            _lessonUI.ShowLesson(_step, _channel, _display.NextSpreadLayer, _filter.IsScanning);
            _filter.SetVisible(IsFilterStep);
            UpdateNodeStates();
            RefreshAppearance(false);
            if (IsFilterStep)
            {
                ApplyPatch();
            }
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
            _lessonUI.ShowPatch(row, column, _data.ReadFirstLayerSum(_channel, row, column),
                _data.ReadValue(0, _channel, row, column), count);
        }

        private void SelectReceptiveField(int row, int column)
        {
            _data.SelectPixel(row, column);
            _lessonUI.ShowAffectedPixel(row, column, _data.CountAffectedPositions(0));
            RefreshAppearance(true);
        }

        private void EditInputPixel(int row, int column)
        {
            _data.ToggleInput(row, column);
            _data.SelectPixel(row, column);
            _editCount++;
            RefreshAppearance(false);
            _lessonUI.ShowInputEdited(_editCount);
        }

        private void RestoreLesson()
        {
            if (!_inputMap.TryReadInput(out float[,] input))
            {
                return;
            }
            _data.RestoreInput(input);
            System.Array.Clear(_visited, 0, _visited.Length);
            _editCount = 0;
            _lastStepSkipped = false;
            ResetLayout();
        }

        private void UpdateNodeStates()
        {
            _display.UpdateLearningStates(_step, _channel, _filter.Row, _filter.Column, _visited);
        }

        private void UpdateButtons()
        {
            _lessonUI.UpdateButtons(_step, _display.NextSpreadLayer, _filter.IsScanning);
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
