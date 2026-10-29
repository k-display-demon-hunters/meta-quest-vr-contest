using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    public sealed class PixelSelectionController : System.IDisposable
    {
        private readonly FilterVolumeController _volume;
        private const float FilterDepth = 0.22f;
        private readonly Transform _board;
        private readonly Vector2 _boardSize;
        private readonly XRGrabInteractable _filter;
        private readonly MeshRenderer _lensRenderer;
        private readonly Transform _selectionFrame;
        private readonly Material _lensMaterial;
        private readonly int _columns;
        private readonly int _rows;
        private readonly Vector3 _dockLocalPosition;
        private readonly Quaternion _dockLocalRotation;
        private bool _snapPending;
        private bool _hasSelection;
        private bool _previewValid;
        private Vector2Int _selected;
        private Vector2Int _preview;

        public bool HasSelection => _hasSelection;
        public Vector2Int SelectedPixel => _selected;
        public Vector3 SelectionWorldPosition => _board.TransformPoint(SelectionCenter(_selected));

        public PixelSelectionController(Transform board, Vector2 boardSize, XRGrabInteractable filter,
            MeshRenderer lensRenderer, Transform selectionFrame, Material lensMaterial, int columns, int rows, Color32[] pixels)
        {
            _board = board;
            _boardSize = boardSize;
            _filter = filter;
            _lensRenderer = lensRenderer;
            _selectionFrame = selectionFrame;
            _lensMaterial = lensMaterial;
            _columns = columns;
            _rows = rows;
            _volume = new FilterVolumeController(filter.transform, lensRenderer, pixels, columns);
            // Use the same right-hand ray grab movement as the detail-view objects.
            _filter.trackPosition = true;
            _filter.trackRotation = false;
            _filter.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _dockLocalPosition = _board.InverseTransformPoint(filter.transform.position);
            _dockLocalRotation = Quaternion.Inverse(_board.rotation) * filter.transform.rotation;
        }

        public void ResetSelection()
        {
            _snapPending = false;
            _hasSelection = false;
            _previewValid = false;
            PlaceFilter();
        }

        public void SuspendSelection()
        {
            _snapPending = false;
            _selectionFrame.gameObject.SetActive(false);
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

        public bool UpdateSelection()
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
            _lensRenderer.gameObject.SetActive(_previewValid);
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
            _lensMaterial.mainTextureScale = new Vector2(3f / _columns, 3f / _rows);
            _lensMaterial.mainTextureOffset = new Vector2((float)origin.x / _columns, (float)origin.y / _rows);
            _selectionFrame.localPosition = SelectionCenter(origin) + Vector3.back * 0.015f;
            _lensRenderer.gameObject.SetActive(true);
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
                : _board.TransformPoint(_dockLocalPosition);
            Quaternion rotation = _hasSelection ? _board.rotation : _board.rotation * _dockLocalRotation;
            _filter.transform.SetPositionAndRotation(position, rotation);
            Rigidbody body = _filter.GetComponent<Rigidbody>();
            body.position = position;
            body.rotation = rotation;
            if (_hasSelection) { SetPreview(_selected); }
            else
            {
                _lensRenderer.gameObject.SetActive(false);
                _selectionFrame.gameObject.SetActive(false);
            }
        }

        public void Dispose() { _volume.Dispose(); }

        public void HandleReleased()
        {
            _snapPending = true;
        }

        public void HandleGrabbed()
        {
            _snapPending = false;
        }

    }
}
