using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using XR;

namespace Parchment
{
    // Shared session: pickups and tablet UI use this entry point independently.
    public sealed class ParchmentManager : MonoBehaviour
    {
        public bool HasCollectedAll(IReadOnlyList<int> documentIds)
        {
            foreach (int id in documentIds)
            {
                if (!HasCollected(id)) { return false; }
            }
            return true;
        }
        public ParchmentInventoryItem GetInventoryItem(int id) =>
            new ParchmentInventoryItem(id, ParchmentCatalog.Get(id).Title, HasCollected(id));
        private const float CollectionDistance = 3f;
        private readonly PickupRegistration[] _pickups = new PickupRegistration[ParchmentCatalog.All.Count];
        private readonly bool[] _collected = new bool[ParchmentCatalog.All.Count];
        private ParchmentReaderController _prefab;
        private ParchmentReaderController _reader;
        private Camera _camera;
        private XRPlayerInputController _playerInput;
        private Coroutine _opening;
        private int _index;
        public bool IsOpen { get; private set; }
        public event Action Opened;
        public event Action Closed;
        public event Action<int> Collected;

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

        public void CollectDocuments(IReadOnlyList<int> documentIds)
        {
            foreach (int id in documentIds)
            {
                if (!Collect(id)) { continue; }
                // A pickup can be absent while its document remains available in the session.
                _pickups[id]?.Collected();
            }
        }

        public void RegisterPickup(int index, Transform source,
            ParchmentReaderController prefab, Action collected)
        {
            _pickups[index] = new PickupRegistration(source, prefab, collected);
            _prefab = prefab;
            if (HasCollected(index)) { collected(); }
        }

        public void UnregisterPickup(int index, Transform source)
        {
            if (_pickups[index]?.Source != source) { return; }
            _pickups[index] = null;
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
            pickup.Collected();
            Collect(index);
            Open(index, new Pose(pickup.Source.position, pickup.Source.rotation));
        }

        public void OpenCollected(int index, Camera camera, Pose origin)
        {
            if (IsOpen || !HasCollected(index)) return;
            _camera = camera;
            _playerInput = camera.GetComponentInParent<XRPlayerInputController>();
            Open(index, origin);
        }
        private void Open(int index, Pose origin)
        {
            _index = index;
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
        private void Complete() => Close();
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
            Closed?.Invoke();
        }

        private sealed class PickupRegistration
        {
            public Transform Source { get; }
            public ParchmentReaderController Prefab { get; }
            public Action Collected { get; }

            public PickupRegistration(Transform source,
                ParchmentReaderController prefab, Action collected)
            {
                Source = source;
                Prefab = prefab;
                Collected = collected;
            }
        }
    }
}
