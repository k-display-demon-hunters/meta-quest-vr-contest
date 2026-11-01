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
                foreach (int scrollId in GetScrollIdsForDocument(id))
                {
                    if (!HasCollected(scrollId)) { return false; }
                }
            }
            return true;
        }

        private static readonly int[] Room1ScrollIds = { 0 };
        private static readonly int[] Room2ScrollIds = { 1, 2 };

        // The learning catalog still requests its original five document IDs.
        // Translate only at that boundary; pickups, the reader and tablet use the three scroll IDs directly.
        private static IReadOnlyList<int> GetScrollIdsForDocument(int documentId) => documentId switch
        {
            0 or 4 => Room1ScrollIds,
            1 or 2 or 3 => Room2ScrollIds,
            _ => throw new ArgumentOutOfRangeException(nameof(documentId), documentId, "Unknown learning document ID.")
        };
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
        private PickupRegistration _activePickup;
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
        // Acquisition is committed only by the reader's final-page completion, not by the initial click.
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
                foreach (int scrollId in GetScrollIdsForDocument(id))
                {
                    if (!Collect(scrollId)) { continue; }
                    // A pickup can be absent while its document remains available in the session.
                    _pickups[scrollId]?.Collected();
                }
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
            _activePickup = pickup;
            Pose origin = new Pose(pickup.Source.position, pickup.Source.rotation);
            pickup.Collected();
            Open(index, origin);
        }

        public void OpenCollected(int index, Camera camera, Pose origin)
        {
            if (IsOpen || !HasCollected(index)) return;
            _activePickup = null;
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
        private void Complete()
        {
            // A parchment is acquired only after its final page has been completed.
            Collect(_index);
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
            PickupRegistration pickup = _activePickup;
            _activePickup = null;
            if (!HasCollected(_index) && pickup != null && pickup.Source != null)
            {
                pickup.Source.gameObject.SetActive(true);
            }
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
