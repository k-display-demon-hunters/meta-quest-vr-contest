using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UI
{
    public sealed class CNNPixelLabManager : MonoBehaviour
    {
        private const int Columns = 32;
        private const int Rows = 18;
        [SerializeField] private Texture2D _sourceImage;
        [SerializeField] private Transform _board;
        [SerializeField] private MeshRenderer _boardRenderer;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private XRGrabInteractable _filter;
        [SerializeField] private MeshRenderer _lensRenderer;
        [SerializeField] private Transform _selectionFrame;
        [SerializeField] private Camera _camera;
        [SerializeField] private Button _detailButton;
        [SerializeField] private GameObject _detail;
        [SerializeField] private Image[] _cells;
        [SerializeField] private Text[] _values;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private Button _nextButton;
        private RgbConvolutionView _rgbView;
        private PixelSelectionController _selection;
        private PixelLabUIController _ui;
        private Color32[] _pixels;
        private Texture2D _smoothTexture;
        private Texture2D _pixelTexture;
        private Material _boardMaterial;
        private Material _lensMaterial;
        private bool _isRevealStage;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedUIInputModule;
        private bool _initialized;

        public Vector3 SelectionWorldPosition => _selection.SelectionWorldPosition;
        public bool HasSelection => _selection != null && _selection.HasSelection;
        public bool ConvolutionCompleted { get; private set; }
        public void MarkConvolutionCompleted() { ConvolutionCompleted = true; }
        public Vector2Int SelectedPixel => _selection.SelectedPixel;

#if UNITY_EDITOR
        public bool EditorDetailsVisible => _detail.activeSelf;
        public Vector3 EditorPreviewFocus => _detail.activeSelf ? _detail.transform.position + _detail.transform.right * 0.35f : _board.position;

        public bool EditorCanDragFilter(XRGrabInteractable target)
        {
            return target == _filter && !_isRevealStage && !_detail.activeSelf;
        }

        public void EditorDragFilter(Ray ray)
        {
            _selection.EditorDragFilter(ray);
        }

        public void EditorReleaseFilter()
        {
            _selection.EditorReleaseFilter();
        }
#endif

        private void Awake()
        {
            // Resolve the surviving rig, since the scene's duplicate may have been removed.
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[CNNPixelLab] An active XRPlayerRig camera is required.", this);
                enabled = false;
                return;
            }
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
            {
                canvas.worldCamera = _camera;
            }
            _smoothTexture = PixelImageFactory.CreateSmoothPhoto(_sourceImage);
            _pixels = PixelImageFactory.SamplePixels(_smoothTexture, Columns, Rows);
            _pixelTexture = PixelImageFactory.CreateTexture(_pixels, Columns, Rows, FilterMode.Point);
            _boardMaterial = _boardRenderer.material;
            _boardMaterial.mainTexture = _smoothTexture;
            _lensMaterial = _lensRenderer.material;
            _lensMaterial.mainTexture = _pixelTexture;
            _selection = new PixelSelectionController(_board, _boardSize, _filter,
                _lensRenderer, _selectionFrame, _lensMaterial, Columns, Rows, _pixels);
            _filter.selectExited.AddListener(HandleReleased);
            _filter.selectEntered.AddListener(HandleGrabbed);
            _rgbView = _detail.AddComponent<RgbConvolutionView>();
            _rgbView.Initialize(this, _values[0].font);
            _ui = new PixelLabUIController(_detailButton, _nextButton, _values[0].font,
                _board.gameObject, _filter.gameObject);
            if (_revealStage != null) { _revealStage.PixelsRevealed += HandlePixelsRevealed; }
            _initialized = true;
            ResetLab();
        }

        private void OnEnable()
        {
            if (!_initialized) { return; }
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("CNN Pixel Lab UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedUIInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void LateUpdate()
        {
            if (!_initialized || _isRevealStage || (_revealStage != null && _revealStage.IsReadyForConvolution)) { return; }
            if (_selection.UpdateSelection())
            {
                _ui.SetSelectionAvailable(_selection.HasSelection);
                UpdateInstruction();
            }
        }

        public void ShowDetails()
        {
            if (_isRevealStage || !_selection.HasSelection || _filter.isSelected) { return; }
            if (_revealStage == null || _revealStage.Phase != PixelRevealStage.RevealPhase.Inactive) { return; }
            _isRevealStage = true;
            _ui.ShowRevealStarted();
            _selection.SuspendSelection();
            _filter.gameObject.SetActive(false);
            _revealStage.ShowImage(_pixels, Columns, Rows, _selection.SelectedPixel);
            _revealStage.RevealPixels();
        }

        public void BeginConvolution()
        {
            if (_revealStage == null || !_revealStage.IsReadyForConvolution || !_selection.HasSelection
                || _filter.isSelected) { return; }
            _rgbView.Show(_pixels, Columns, Rows, _selection.SelectedPixel, true);
            SetBackgroundHidden(true);
        }

        public void CloseDetails()
        {
            foreach (SpatialConvolutionController spatial in _detail.GetComponentsInChildren<SpatialConvolutionController>(true))
            {
                spatial.ReleaseInteractions();
            }
            _detail.SetActive(false);
            SetBackgroundHidden(false);
        }

        public void SetBackgroundHidden(bool hidden)
        {
            _ui.SetBackgroundHidden(hidden);
        }

        public void ResetLab()
        {
            if (_filter.isSelected) { return; }
            ConvolutionCompleted = false;
            CloseDetails();
            _isRevealStage = false;
            if (_revealStage != null) { _revealStage.ResetStage(); }
            _ui.ResetControls();
            _filter.gameObject.SetActive(true);
            _selection.ResetSelection();
            _detail.SetActive(false);
            UpdateInstruction();
        }

        public void MoveSelection(int dx, int dy)
        {
            if (_isRevealStage || !_selection.HasSelection || _filter.isSelected) { return; }
            _selection.MoveSelection(dx, dy);
            _rgbView.Show(_pixels, Columns, Rows, _selection.SelectedPixel, _revealStage != null && _revealStage.IsReadyForConvolution);
        }

        private void HandlePixelsRevealed()
        {
            _isRevealStage = false;
            _ui.ShowPixelsRevealed(_selection.HasSelection);
            UpdateInstruction();
        }

        public void NextStage()
        {
            if (_isRevealStage) { return; }
            if (_revealStage != null && _revealStage.IsReadyForConvolution)
            {
                BeginConvolution();
                return;
            }
            ShowDetails();
        }

        private void HandleReleased(SelectExitEventArgs args)
        {
            _selection.HandleReleased(args);
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            _ui.ShowFilterGrabbed();
            _selection.HandleGrabbed(args);
            _detail.SetActive(false);
        }

        private void UpdateInstruction()
        {
            _ui.UpdateInstruction(_revealStage != null && _revealStage.IsReadyForConvolution,
                _selection.HasSelection);
        }

        private void OnDisable()
        {
            if (_ownedEventSystem != null)
            {
                _ownedEventSystem.SetActive(false);
                Destroy(_ownedEventSystem);
                _ownedEventSystem = null;
            }
            if (_addedUIInputModule != null)
            {
                Destroy(_addedUIInputModule);
                _addedUIInputModule = null;
            }
            if (_initialized && _filter != null && _filter.isSelected)
            {
                // Keep a held filter in the content scene when the persistent rig survives.
                if (_filter.interactionManager != null)
                {
                    _filter.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_filter);
                }
                _filter.transform.SetParent(transform, true);
            }
        }

        private void OnDestroy()
        {
            if (_filter != null)
            {
                _filter.selectExited.RemoveListener(HandleReleased);
                _filter.selectEntered.RemoveListener(HandleGrabbed);
            }
            if (_revealStage != null) { _revealStage.PixelsRevealed -= HandlePixelsRevealed; }
            _selection?.Dispose();
            Destroy(_smoothTexture);
            Destroy(_pixelTexture);
            Destroy(_boardMaterial);
            Destroy(_lensMaterial);
        }
    }
}
