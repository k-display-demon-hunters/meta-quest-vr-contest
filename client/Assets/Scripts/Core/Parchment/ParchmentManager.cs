using System;
using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;
using XR;

namespace Parchment
{
    // Shared session: pickups and tablet UI use this entry point independently.
    public sealed class ParchmentManager : MonoBehaviour
    {
        private const float CollectionDistance = 3f;
        private readonly PickupRegistration[] _pickups = new PickupRegistration[ParchmentCatalog.All.Count];
        private Action _pickupOpened;
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
        public event Action LocationsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeSession()
        {
            new GameObject("Parchment Session").AddComponent<ParchmentManager>();
        }

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
        public ParchmentRoom GetPlacementRoom(int index)
        {
            return _pickups[index]?.Room ?? ParchmentRoom.None;
        }

        public void RegisterPickup(int index, ParchmentRoom room, Transform source,
            ParchmentReaderController prefab, Action opened, Action<bool> finished)
        {
            _pickups[index] = new PickupRegistration(room, source, prefab, opened, finished);
            LocationsChanged?.Invoke();
            if (HasCollected(index)) { finished(true); }
        }

        public void UnregisterPickup(int index, Transform source)
        {
            if (_pickups[index]?.Source != source) { return; }
            _pickups[index] = null;
            if (IsOpen && _collecting && _index == index) { Close(); }
            LocationsChanged?.Invoke();
        }

        public void RequestPickup(int index, Transform interactor)
        {
            PickupRegistration pickup = _pickups[index];
            // Input can arrive while a source is being disabled or another reader is open.
            if (IsOpen || HasCollected(index) || pickup == null || interactor == null) { return; }
            if ((interactor.position - pickup.Source.position).sqrMagnitude > CollectionDistance * CollectionDistance)
            {
                return;
            }
            XROrigin rig = interactor.GetComponentInParent<XROrigin>();
            if (rig == null || rig.Camera == null) { return; }
            _prefab = pickup.Prefab;
            _camera = rig.Camera;
            _playerInput = _camera.GetComponentInParent<XRPlayerInputController>();
            _pickupOpened = pickup.Opened;
            _pickupFinished = pickup.Finished;
            Open(index, new Pose(pickup.Source.position, pickup.Source.rotation), true);
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
            _pickupOpened?.Invoke();
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
            _pickupOpened = null;
            _pickupFinished = null;
            finished?.Invoke(HasCollected(_index));
            Closed?.Invoke();
        }

        private sealed class PickupRegistration
        {
            public ParchmentRoom Room { get; }
            public Transform Source { get; }
            public ParchmentReaderController Prefab { get; }
            public Action Opened { get; }
            public Action<bool> Finished { get; }

            public PickupRegistration(ParchmentRoom room, Transform source,
                ParchmentReaderController prefab, Action opened, Action<bool> finished)
            {
                Room = room;
                Source = source;
                Prefab = prefab;
                Opened = opened;
                Finished = finished;
            }
        }
    }
}
