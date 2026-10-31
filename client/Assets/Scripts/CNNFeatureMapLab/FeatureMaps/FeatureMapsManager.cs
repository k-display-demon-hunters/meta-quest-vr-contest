using System;
using CNNFeatureMaps.FeatureMaps.Interaction;
using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps
{
    public sealed class FeatureMapsManager : MonoBehaviour
    {
        [SerializeField] private InputMapController _inputMap;
        [SerializeField] private MapComputationController _computation;
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
            Color[,] input = _inputMap.ReadInput();
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
            if (_step == LessonStep.ExploreFeatureMaps && _data != null)
            {
                if (target.TryGetComponent(out MapCell selected) && selected.Layer >= 0)
                {
                    ShowMapCalculation(selected.Layer, selected.Channel, selected.Row, selected.Column);
                }
                else if (target.TryGetComponent(out MapPanel panel) && panel.Layer >= 0)
                {
                    ShowMapCalculation(panel.Layer, panel.Channel, 3, 3);
                }
            }
            if (target.TryGetComponent(out MapLayer layer))
            {
                layer.ToggleSpread();
            }
        }

        private void ShowMapCalculation(int layer, int channel, int row, int column)
        {
            _data.SelectFeatureCell(layer, channel, row, column);
            _display.RefreshAppearance(_data, true);
            _computation.Show(_data, layer, channel, row, column);
        }

        public void CloseCalculation() { _computation.Close(); }

        public float[,] CopyFirstLayerMap(int channel)
        {
            int size = ConvolutionSimulation.Size;
            float[,] values = new float[size, size];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    values[row, column] = _data.ReadValue(0, channel, row, column);
                }
            }
            return values;
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
                if (_step == LessonStep.ExploreFeatureMaps && panel.Layer >= 0)
                {
                    ShowMapCalculation(panel.Layer, panel.Channel, 3, 3);
                }
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
            Color[,] input = _inputMap.ReadInput();
            if (input == null)
            {
                return false;
            }
            _computation.Close();
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
                case LessonStep.ExploreFeatureMaps:
                case LessonStep.TraceReceptiveField:
                    _computation.Close();
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
