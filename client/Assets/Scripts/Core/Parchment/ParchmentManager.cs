using System;
using System.Collections;
using UnityEngine;
using XR;

namespace Parchment
{
    // Shared session: pickups and tablet UI use this entry point independently.
    public sealed class ParchmentManager : MonoBehaviour
    {
        private readonly bool[] _collected = new bool[ParchmentCatalog.All.Count];
        private ParchmentReaderController _prefab;
        private ParchmentReaderController _reader;
        private Camera _camera;
        private XRPlayerInputController _playerInput;
        private Coroutine _opening;
        private Action<bool> _pickupFinished;
        private int _index;
        private bool _collecting;
        public bool IsOpen { get; private set; }
        public event Action Opened;
        public event Action Closed;
        public event Action<int> Collected;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnDisable() => Close();
        public bool HasCollected(int index) => index >= 0 && index < _collected.Length && _collected[index];
        private bool Collect(int index)
        {
            if (index < 0 || index >= _collected.Length || _collected[index]) { return false; }
            _collected[index] = true;
            Collected?.Invoke(index);
            return true;
        }
        public bool OpenPickup(int index, Pose origin, Camera camera, ParchmentReaderController prefab, Action<bool> finished)
        {
            if (IsOpen || HasCollected(index)) { return false; }
            _prefab = prefab;
            _camera = camera;
            _playerInput = camera.GetComponentInParent<XRPlayerInputController>();
            _pickupFinished = finished;
            Open(index, origin, true);
            return true;
        }
        public void OpenCollected(int index, Pose origin)
        {
            if (!IsOpen && HasCollected(index)) { Open(index, origin, false); }
        }
        private void Open(int index, Pose origin, bool collecting)
        {
            _index = index;
            _collecting = collecting;
            IsOpen = true;
            _playerInput.SetParchmentMovementBlocked(true);
            Opened?.Invoke();
            _opening = StartCoroutine(PresentAfterInteraction(origin));
        }
        private IEnumerator PresentAfterInteraction(Pose origin)
        {
            yield return null;
            _reader = Instantiate(_prefab, transform);
            _reader.Completed += Complete;
            _reader.CloseRequested += Close;
            _reader.Present(_index, _camera, origin);
            _opening = null;
        }
        private void Complete()
        {
            if (_collecting) { Collect(_index); }
            Close();
        }
        public void CancelPickup(int index)
        {
            if (IsOpen && _collecting && _index == index) { Close(); }
        }
        private void Close()
        {
            if (!IsOpen) { return; }
            if (_opening != null) { StopCoroutine(_opening); _opening = null; }
            if (_reader != null)
            {
                _reader.Completed -= Complete;
                _reader.CloseRequested -= Close;
                _reader.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(_reader.gameObject);
                _reader = null;
            }
            IsOpen = false;
            // The player may already be destroyed during application shutdown.
            if (_playerInput != null) { _playerInput.SetParchmentMovementBlocked(false); }
            Action<bool> finished = _pickupFinished;
            _pickupFinished = null;
            finished?.Invoke(HasCollected(_index));
            Closed?.Invoke();
        }
    }
}
