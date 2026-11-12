using System;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNScreen
{
    public sealed class PixelSelectionController : MonoBehaviour
    {
        [SerializeField] private SelectedPixelsController _volume;
        private const float FilterDepth = 0.22f;
        [SerializeField] private Transform _board;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private XRGrabInteractable _filter;
        [SerializeField] private Transform _selectionFrame;
        [SerializeField] private Transform _magnifierDock;
        private Vector3 _filterScale;
        private int _columns;
        private int _rows;
        private bool _snapPending;
        private bool _hasSelection;
        private bool _previewValid;
        private Vector2Int _selected;
        private Vector2Int _preview;

        public bool HasSelection => _hasSelection;
        public Vector2Int SelectedPixel => _selected;
        public Vector3 SelectionWorldPosition => _board.TransformPoint(SelectionCenter(_selected));

        private bool _selectionEnabled;
        public bool IsFilterHeld => _filter.isSelected;
        public event Action SelectionChanged;
        public event Action FilterGrabbed;

        public void Initialize(PixelImageData image)
        {
            _columns = image.Columns;
            _rows = image.Rows;
            _volume.Initialize(image.Pixels, _columns);
            _selectionEnabled = true;
            _filterScale = _filter.transform.localScale;
        }

        public void ResetSelection()
        {
            _selectionEnabled = true;
            _filter.transform.SetParent(transform, true);
            _filter.transform.localScale = _filterScale;
            _filter.enabled = true;
            foreach (Collider collider in _filter.colliders) { collider.enabled = true; }
            _filter.gameObject.SetActive(true);
            _snapPending = false;
            _hasSelection = false;
            _previewValid = false;
            PlaceFilter();
        }

        public void SuspendSelection()
        {
            _selectionEnabled = false;
            _filter.enabled = false;
            foreach (Collider collider in _filter.colliders) { collider.enabled = false; }
            _filter.transform.SetParent(_board, true);
            _snapPending = false;
            _selectionFrame.gameObject.SetActive(false);
        }

        public void RestoreSelection(Vector2Int selected)
        {
            _selected = selected;
            _hasSelection = true;
            _snapPending = false;
            PlaceFilter();
        }

        public void EnsureSelection()
        {
            if (!_hasSelection)
            {
                _selected = new Vector2Int((_columns - 3) / 2, (_rows - 3) / 2);
                _hasSelection = true;
            }
            PlaceFilter();
        }

        public void MoveSelection(int dx, int dy)
        {
            _selected = new Vector2Int(Mathf.Clamp(_selected.x + dx, 0, _columns - 3),
                Mathf.Clamp(_selected.y + dy, 0, _rows - 3));
            PlaceFilter();
        }

        private bool UpdateSelection()
        {
            if (_filter.isSelected)
            {
                UpdatePreview();
            }
            if (_snapPending)
            {
                _snapPending = false;
                UpdatePreview();
                if (_previewValid)
                {
                    _selected = _preview;
                    _hasSelection = true;
                }
                PlaceFilter();
                return true;
            }
            return false;
        }

        private void UpdatePreview()
        {
            Vector3 local = _board.InverseTransformPoint(_filter.transform.position);
            _previewValid = Mathf.Abs(local.x) <= _boardSize.x * 0.5f
                && Mathf.Abs(local.y) <= _boardSize.y * 0.5f
                && local.z >= -0.85f && local.z <= 0.1f
                && Mathf.Abs(Vector3.Dot(_filter.transform.forward, _board.forward)) > 0.65f;
            _volume.SetVisible(_previewValid);
            _selectionFrame.gameObject.SetActive(_previewValid);
            if (!_previewValid) { return; }
            _preview = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt((local.x / _boardSize.x + 0.5f) * _columns) - 1, 0, _columns - 3),
                Mathf.Clamp(Mathf.FloorToInt((local.y / _boardSize.y + 0.5f) * _rows) - 1, 0, _rows - 3));
            SetPreview(_preview);
        }

        private void SetPreview(Vector2Int origin)
        {
            _volume.SetOrigin(origin);
            _selectionFrame.localPosition = SelectionCenter(origin) + Vector3.back * 0.015f;
            _volume.SetVisible(true);
            _selectionFrame.gameObject.SetActive(true);
        }

        private Vector3 SelectionCenter(Vector2Int origin)
        {
            return new Vector3(((origin.x + 1.5f) / _columns - 0.5f) * _boardSize.x,
                ((origin.y + 1.5f) / _rows - 0.5f) * _boardSize.y, 0);
        }

        private void PlaceFilter()
        {
            Vector3 position = _hasSelection
                ? _board.TransformPoint(SelectionCenter(_selected) + Vector3.back * FilterDepth)
                : _magnifierDock.position;
            Quaternion rotation = _hasSelection ? _board.rotation : _magnifierDock.rotation;
            _filter.transform.SetPositionAndRotation(position, rotation);
            Rigidbody body = _filter.GetComponent<Rigidbody>();
            body.position = position;
            body.rotation = rotation;
            if (_hasSelection) { SetPreview(_selected); }
            else
            {
                _volume.SetVisible(false);
                _selectionFrame.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _filter.selectEntered.AddListener(HandleGrabbed);
            _filter.selectExited.AddListener(HandleReleased);
        }

        private void LateUpdate()
        {
            if (_selectionEnabled && UpdateSelection()) { SelectionChanged?.Invoke(); }
        }

        private void OnDisable()
        {
            _filter.selectEntered.RemoveListener(HandleGrabbed);
            _filter.selectExited.RemoveListener(HandleReleased);
            if (_filter.isSelected)
            {
                _filter.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_filter);
                _filter.transform.SetParent(transform, true);
                _snapPending = true;
            }
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            _snapPending = true;
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            _snapPending = false;
            _filter.transform.rotation = _board.rotation;
            _filter.GetComponent<Rigidbody>().rotation = _board.rotation;
            FilterGrabbed?.Invoke();
        }

    }
}
