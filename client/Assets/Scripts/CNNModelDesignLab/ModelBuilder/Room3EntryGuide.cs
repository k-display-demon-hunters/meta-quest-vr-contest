using System;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class Room3EntryGuide : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private ModelSupplyController _supply;
        [SerializeField] private ModelConveyorController _conveyor;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Bounds _entryVolume =
            new Bounds(new Vector3(-1.45f, 1.05f, -0.85f), new Vector3(4.05f, 2.7f, 2.35f));

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float _pulseDuration = 1.2f;
        [SerializeField, Min(0.01f)] private float _successDuration = 3f;

        [Header("Appearance")]
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(0.498f, 0.961f, 1f);
        [SerializeField, Min(0f)] private float _brightness = 1.4f;
        [SerializeField, Min(0.001f)] private float _lineWidth = 0.018f;

        private static int? _lastCompletedSceneHandle;
        private Camera _playerCamera;
        private bool _entered;
        private bool _retryPlacement;
        private bool _completed;
        private float _successUntil;
        private ModelBlockOutline _outline;
        private Renderer _highlightedSurface;
        private Renderer _supplySurface;

        internal PlacementGuideState State { get; private set; }
        internal int TargetSlotIndex { get; private set; } = -1;
        internal event Action OnChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEntryHistory() => _lastCompletedSceneHandle = null;

        internal void Initialize()
        {
            // The first supply display is the existing CONV example.
            _supplySurface = _supply.GetExampleDisplays()[0].Pad;
        }

        internal void UpdateGuide(bool available, bool isDragging, int placementSlot)
        {
            if (!available || !isActiveAndEnabled)
            {
                Hide();
                return;
            }
            if (_completed)
            {
                SetState(Time.unscaledTime < _successUntil
                    ? PlacementGuideState.Completed : PlacementGuideState.Hidden, -1);
                return;
            }
            // Remember success across additive room reloads in the same factory visit.
            if (_lastCompletedSceneHandle == SceneManager.GetActiveScene().handle)
            {
                Hide();
                return;
            }
            if (!_entered)
            {
                if (_playerCamera == null) { _playerCamera = Camera.main; }
                if (_playerCamera == null ||
                    !_entryVolume.Contains(transform.InverseTransformPoint(_playerCamera.transform.position))) { return; }
                _entered = true;
            }

            int slot = placementSlot >= 0 ? placementSlot : FindFirstEmptySlot();
            PlacementGuideState state = isDragging
                ? placementSlot >= 0 ? PlacementGuideState.ReleaseBlock : PlacementGuideState.MoveToSlot
                : _retryPlacement ? PlacementGuideState.RetryPlacement : PlacementGuideState.GrabBlock;
            SetState(state, slot);
            ShowHighlight(state, slot);
        }

        internal void BlockGrabbed()
        {
            _entered = true;
            _retryPlacement = false;
        }

        internal void BlockReleased(bool placed)
        {
            if (_completed || _lastCompletedSceneHandle == SceneManager.GetActiveScene().handle) { return; }
            _entered = true;
            if (placed)
            {
                _completed = true;
                _lastCompletedSceneHandle = SceneManager.GetActiveScene().handle;
                _successUntil = Time.unscaledTime + _successDuration;
                ClearHighlight();
                SetState(PlacementGuideState.Completed, -1);
            }
            else
            {
                _retryPlacement = true;
            }
        }

        internal void Hide()
        {
            ClearHighlight();
            SetState(PlacementGuideState.Hidden, -1);
        }

        private void SetState(PlacementGuideState state, int slot)
        {
            if (State == state && TargetSlotIndex == slot) { return; }
            State = state;
            TargetSlotIndex = slot;
            OnChanged?.Invoke();
        }

        private int FindFirstEmptySlot()
        {
            for (int index = 0; index < _conveyor.SettingsControls.Count; index++)
            {
                if (_conveyor.SettingsControls[index].Block == null) { return index; }
            }
            return -1;
        }

        private void ShowHighlight(PlacementGuideState state, int slot)
        {
            Renderer surface;
            if (state == PlacementGuideState.GrabBlock || state == PlacementGuideState.RetryPlacement)
            {
                surface = _supplySurface;
            }
            else
            {
                if (slot < 0) { ClearHighlight(); return; }
                surface = _conveyor.GetSlotRenderer(slot);
            }
            if (_highlightedSurface != surface)
            {
                ClearHighlight();
                _highlightedSurface = surface;
                _outline = new ModelBlockOutline(transform, "First placement guide", _outlineMaterial);
                _outline.Show(surface);
            }
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / _pulseDuration);
            Color color = _color * (_brightness * pulse);
            color.a = 1f;
            _outline.SetAppearance(color, _lineWidth);
        }

        private void ClearHighlight()
        {
            _outline?.Remove();
            _outline = null;
            _highlightedSurface = null;
        }

        private void OnDisable() => Hide();

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_entryVolume.center, _entryVolume.size);
        }
    }
}
