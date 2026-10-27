using UnityEngine;

namespace CNNFeatureMaps.FeatureMaps.Interaction
{
    public sealed class InteractionManager : MonoBehaviour
    {
        [SerializeField] private FilterController _filter;
        private readonly InteractionController _interaction = new InteractionController();
        private bool _filterVisible;

        public bool HasHeldTargets => _interaction.HasHeldTargets;
        public int Row => _filter.Row;
        public int Column => _filter.Column;
        public bool IsScanning => _filter.IsScanning;

        public event System.Action<Transform> OnTargetHeld;
        public event System.Action<Transform> OnTargetActivated;
        public event System.Action OnPosesReset;
        public event System.Action<int, int> OnFilterPositionChanged;
        public event System.Action OnScanningChanged;

        public void Initialize()
        {
            _interaction.Initialize(transform);
            _filter.Initialize();
        }

        private void OnEnable()
        {
            _interaction.OnTargetHeld += HandleTargetHeld;
            _interaction.OnTargetActivated += HandleTargetActivated;
        }

        private void OnDisable()
        {
            _interaction.OnTargetHeld -= HandleTargetHeld;
            _interaction.OnTargetActivated -= HandleTargetActivated;
        }

        private void LateUpdate()
        {
            if (!_filterVisible)
            {
                return;
            }
            bool wasScanning = IsScanning;
            if (_filter.UpdatePosition())
            {
                OnFilterPositionChanged?.Invoke(Row, Column);
            }
            if (wasScanning != IsScanning)
            {
                OnScanningChanged?.Invoke();
            }
            _filter.UpdateConnection();
        }

        public bool HasHeldDescendant(Transform ancestor)
        {
            return _interaction.HasHeldDescendant(ancestor);
        }

        public bool ScaleSelectedTarget(float multiplier)
        {
            return _interaction.ScaleSelectedTarget(multiplier);
        }

        public void ResetPoses()
        {
            if (_interaction.ResetPoses())
            {
                OnPosesReset?.Invoke();
            }
        }

        public void SetFilterVisible(bool visible)
        {
            _filterVisible = visible;
            _filter.SetVisible(visible);
        }

        public void ToggleScanning()
        {
            _filter.ToggleScanning();
            OnScanningChanged?.Invoke();
        }

        public void StopScanning()
        {
            if (!IsScanning)
            {
                return;
            }
            _filter.StopScanning();
            OnScanningChanged?.Invoke();
        }

        public void SelectFilterPosition(int row, int column)
        {
            bool wasScanning = IsScanning;
            _filter.StopScanning();
            _filter.SelectPosition(row, column);
            OnFilterPositionChanged?.Invoke(row, column);
            if (wasScanning)
            {
                OnScanningChanged?.Invoke();
            }
        }

        public void SetConnectionTarget(Transform target)
        {
            _filter.SetConnectionTarget(target);
        }

        private void HandleTargetHeld(Transform target)
        {
            OnTargetHeld?.Invoke(target);
        }

        private void HandleTargetActivated(Transform target)
        {
            OnTargetActivated?.Invoke(target);
        }
    }
}
