using System;
using CNNFeatureMaps.FeatureMaps.Interaction;
using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class FeatureMapsManager : MonoBehaviour
    {
        [SerializeField] private InputMapController _inputMap;
        [SerializeField] private MapDisplayController _display;
        [SerializeField] private InteractionManager _interaction;
        private ConvolutionSimulation _data;
        private readonly bool[,,] _visited = new bool[2, 8, 8];
        private readonly Transform[,,] _firstCells = new Transform[2, 8, 8];
        private LessonStep _step;
        private int _channel;
        private int _row = 3;
        private int _column = 3;

        public int EditCount { get; private set; }
        public int NextSpreadLayer => _display.NextSpreadLayer;
        public bool HasHeldTargets => _interaction.HasHeldTargets;
        public bool IsScanning => _interaction.IsScanning;

        public event Action<LessonStep, int, int, bool> OnStepApplied;
        public event Action OnScanningChanged;
        public event Action<int, int, float, float, int> OnPatchEvaluated;
        public event Action<int, int, int> OnAffectedPixelSelected;
        public event Action<int> OnInputEdited;

        public void Initialize()
        {
            _interaction.Initialize();
            _display.Initialize(_interaction.HasHeldDescendant);
            foreach (MapCell cell in _display.Cells)
            {
                if (cell.Layer == 0)
                {
                    _firstCells[cell.Channel, cell.Row, cell.Column] = cell.transform;
                }
            }
        }

        private void OnEnable()
        {
            _interaction.OnTargetHeld += ExtractTarget;
            _interaction.OnTargetActivated += ActivateTarget;
            _interaction.OnPosesReset += _display.ResetExtractedStates;
            _interaction.OnFilterPositionChanged += ApplyPatch;
            _interaction.OnScanningChanged += HandleScanningChanged;
        }

        public bool TryStart()
        {
            float[,] input = _inputMap.ReadInput();
            if (input == null)
            {
                return false;
            }
            _data = new ConvolutionSimulation(input);
            return true;
        }

        public void ShowStep(LessonStep step, int channel)
        {
            _step = step;
            _channel = channel;
            _interaction.StopScanning();
            _data.SelectPixel(-1, -1);
            _display.UpdateStepVisibility(step, channel);
            _display.ApplyStepLayout(step == LessonStep.CompareFilters);
            _display.UpdateLearningStates(step, channel, _row, _column, _visited);
            _display.RefreshAppearance(_data, false);
            bool isFilterStep = step == LessonStep.BuildFeatureMap || step == LessonStep.CompareFilters;
            _interaction.SetFilterVisible(isFilterStep);
            OnStepApplied?.Invoke(step, channel, NextSpreadLayer, IsScanning);
            if (isFilterStep)
            {
                ApplyPatch(_interaction.Row, _interaction.Column);
            }
        }

        private void ApplyPatch(int row, int column)
        {
            _row = row;
            _column = column;
            _visited[_channel, row, column] = true;
            _data.SelectWindow(row, column);
            _display.UpdateLearningStates(_step, _channel, row, column, _visited);
            _display.RefreshAppearance(_data, false);
            int count = 0;
            for (int y = 0; y < ConvolutionSimulation.Size; y++)
            {
                for (int x = 0; x < ConvolutionSimulation.Size; x++)
                {
                    if (_visited[_channel, y, x])
                    {
                        count++;
                    }
                }
            }
            _interaction.SetConnectionTarget(_firstCells[_channel, row, column]);
            OnPatchEvaluated?.Invoke(row, column, _data.ReadFirstLayerSum(_channel, row, column),
                _data.ReadValue(0, _channel, row, column), count);
        }

        private void ActivateTarget(Transform target)
        {
            if (target.TryGetComponent(out MapCell cell) && cell.Layer < 0 && _data != null)
            {
                SelectPixel(cell.Row, cell.Column);
            }
            if (target.TryGetComponent(out MapLayer layer))
            {
                layer.ToggleSpread();
            }
        }

        private void ExtractTarget(Transform target)
        {
            if (target.TryGetComponent(out MapCell cell))
            {
                cell.Extract();
            }
            if (target.TryGetComponent(out MapPanel panel))
            {
                panel.Extract();
            }
        }

        public void SpreadNextLayer()
        {
            _display.SpreadNextLayer(_step == LessonStep.CompareFilters);
        }

        public void ToggleScanning()
        {
            _interaction.ToggleScanning();
        }

        public bool ScaleSelectedTarget(float multiplier)
        {
            return _interaction.ScaleSelectedTarget(multiplier);
        }

        public bool TryRestore()
        {
            float[,] input = _inputMap.ReadInput();
            if (input == null)
            {
                return false;
            }
            _data.RestoreInput(input);
            Array.Clear(_visited, 0, _visited.Length);
            EditCount = 0;
            _display.ResetLayout();
            _interaction.ResetPoses();
            return true;
        }

        private void SelectPixel(int row, int column)
        {
            switch (_step)
            {
                case LessonStep.BuildFeatureMap:
                case LessonStep.CompareFilters:
                    _interaction.SelectFilterPosition(row, column);
                    break;
                case LessonStep.TraceReceptiveField:
                    _data.SelectPixel(row, column);
                    _display.RefreshAppearance(_data, true);
                    OnAffectedPixelSelected?.Invoke(row, column, _data.CountAffectedPositions(0));
                    break;
                case LessonStep.EditInput:
                    _data.ToggleInput(row, column);
                    _data.SelectPixel(row, column);
                    EditCount++;
                    _display.RefreshAppearance(_data, false);
                    OnInputEdited?.Invoke(EditCount);
                    break;
            }
        }

        private void HandleScanningChanged()
        {
            OnScanningChanged?.Invoke();
        }

        private void OnDisable()
        {
            _interaction.OnTargetHeld -= ExtractTarget;
            _interaction.OnTargetActivated -= ActivateTarget;
            _interaction.OnPosesReset -= _display.ResetExtractedStates;
            _interaction.OnFilterPositionChanged -= ApplyPatch;
            _interaction.OnScanningChanged -= HandleScanningChanged;
        }
    }
}
