using System.Collections;
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
        [SerializeField, Min(0.01f)] private float _padDuration = 0.4f;
        [SerializeField, Min(0f)] private float _padOverlap = 0.15f;
        [SerializeField, Min(0f)] private float _stagePause = 0.3f;
        [SerializeField, Min(0.01f)] private float _slotDuration = 0.3f;
        [SerializeField, Min(0f)] private float _slotInterval = 0.1f;
        [SerializeField, Min(0f)] private float _repeatPause = 0.8f;
        [SerializeField, Min(1)] private int _repeatCount = 2;

        [Header("Appearance")]
        [SerializeField, ColorUsage(false, true)] private Color _color = new Color(0.498f, 0.961f, 1f);
        [SerializeField, Min(0f)] private float _brightness = 1.4f;
        [SerializeField, Min(0.001f)] private float _lineWidth = 0.018f;

        private static int? _lastEntrySceneHandle;
        private Camera _playerCamera;
        private bool _entryChecked;
        private bool _interactionStarted;
        private bool _canPlay;
        private Coroutine _guide;
        private ModelBlockOutline[] _padOutlines;
        private ModelBlockOutline[] _slotOutlines;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEntryHistory() => _lastEntrySceneHandle = null;

        internal void UpdateAvailability(bool canPlay)
        {
            _canPlay = canPlay && isActiveAndEnabled;
            if (!_canPlay)
            {
                StopGuide();
                return;
            }
            if (_entryChecked) { return; }
            if (_playerCamera == null) { _playerCamera = Camera.main; }
            if (_playerCamera == null ||
                !_entryVolume.Contains(transform.InverseTransformPoint(_playerCamera.transform.position))) { return; }

            _entryChecked = true;
            // The factory can unload and reload this additive room without starting a new visit.
            int sessionScene = SceneManager.GetActiveScene().handle;
            if (_lastEntrySceneHandle == sessionScene) { return; }
            _lastEntrySceneHandle = sessionScene;
            if (!_interactionStarted && !HasOccupiedSlot()) { PlayEntryGuide(); }
        }

        private void OnDisable()
        {
            _canPlay = false;
            StopGuide();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _color;
            Gizmos.DrawWireCube(_entryVolume.center, _entryVolume.size);
        }

        [ContextMenu("Play Entry Guide")]
        private void PlayEntryGuide()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !_canPlay) { return; }
            StopGuide();
            _guide = StartCoroutine(Animate());
        }

        private bool HasOccupiedSlot()
        {
            foreach (SlotSettingsController slot in _conveyor.SettingsControls)
            {
                if (slot.Block != null) { return true; }
            }
            return false;
        }

        internal void CancelForInteraction()
        {
            _interactionStarted = true;
            StopGuide();
        }

        private IEnumerator Animate()
        {
            var displays = _supply.GetExampleDisplays();
            _padOutlines = new ModelBlockOutline[displays.Length];
            _slotOutlines = new ModelBlockOutline[_conveyor.SettingsControls.Count];
            for (int index = 0; index < _padOutlines.Length; index++)
            {
                _padOutlines[index] = CreateOutline(displays[index].Pad);
            }
            for (int index = 0; index < _slotOutlines.Length; index++)
            {
                if (_conveyor.SettingsControls[index].Block != null) { continue; }
                _slotOutlines[index] = CreateOutline(_conveyor.GetSlotRenderer(index));
            }

            float padStep = Mathf.Max(0.01f, _padDuration - _padOverlap);
            for (int pass = 0; pass < _repeatCount; pass++)
            {
                yield return Sweep(_padOutlines, _padDuration, padStep);
                yield return new WaitForSecondsRealtime(_stagePause);
                yield return Sweep(_slotOutlines, _slotDuration, Mathf.Max(0.01f, _slotInterval));
                if (pass + 1 < _repeatCount) { yield return new WaitForSecondsRealtime(_repeatPause); }
            }
            _guide = null;
            ClearOutlines();
        }

        private IEnumerator Sweep(ModelBlockOutline[] outlines, float duration, float step)
        {
            float end = duration + (outlines.Length - 1) * step;
            for (float elapsed = 0f; elapsed < end; elapsed += Time.unscaledDeltaTime)
            {
                for (int index = 0; index < outlines.Length; index++)
                {
                    float t = Mathf.Clamp01((elapsed - index * step) / duration);
                    float pulse = t > 0f && t < 1f ? Mathf.Sin(t * Mathf.PI) : 0f;
                    SetPulse(outlines[index], pulse);
                }
                yield return null;
            }
            for (int index = 0; index < outlines.Length; index++)
            {
                SetPulse(outlines[index], 0f);
            }
        }

        private ModelBlockOutline CreateOutline(Renderer surface)
        {
            ModelBlockOutline outline = new ModelBlockOutline(transform, "Entry guide pulse", _outlineMaterial);
            outline.Show(surface);
            SetPulse(outline, 0f);
            return outline;
        }

        private void SetPulse(ModelBlockOutline outline, float pulse)
        {
            if (outline == null) { return; }
            Color color = _color * (_brightness * pulse);
            color.a = 1f;
            outline.SetAppearance(color, _lineWidth * pulse);
        }

        private void StopGuide()
        {
            if (_guide != null)
            {
                StopCoroutine(_guide);
                _guide = null;
            }
            ClearOutlines();
        }

        private void ClearOutlines()
        {
            RemoveOutlines(ref _padOutlines);
            RemoveOutlines(ref _slotOutlines);
        }

        private static void RemoveOutlines(ref ModelBlockOutline[] outlines)
        {
            if (outlines == null) { return; }
            foreach (ModelBlockOutline outline in outlines) { outline?.Remove(); }
            outlines = null;
        }
    }
}

