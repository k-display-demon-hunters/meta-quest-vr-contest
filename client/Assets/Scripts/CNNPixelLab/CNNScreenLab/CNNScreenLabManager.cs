using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    public sealed class CNNScreenLabManager : MonoBehaviour
    {
        // Keep the scene's serialized bindings; compose runtime modules from them in Awake.
        [SerializeField] private Texture2D _sourceImage;
        [SerializeField] private Transform _board;
        [SerializeField] private MeshRenderer _boardRenderer;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private XRGrabInteractable _filter;
        [SerializeField] private MeshRenderer _lensRenderer;
        [SerializeField] private Transform _selectionFrame;
        [SerializeField] private Camera _camera;
        [SerializeField] private Button _detailButton;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private Font _font;
        [SerializeField] private Button _nextButton;

        private PixelImageManager _image;
        private PixelLabUIController _ui;
        private readonly PixelUIInputController _input = new PixelUIInputController();
        public event System.Action<PixelPatchData, Vector2Int> ConvolutionRequested;
        public event System.Action ResetRequested;

        private void Awake()
        {
            _camera = Camera.main;
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = _camera; }
            _image = new PixelImageManager(_sourceImage, _board, _boardRenderer, _boardSize,
                _filter, _lensRenderer, _selectionFrame, _revealStage);
            _ui = new PixelLabUIController(_detailButton, _nextButton, _font);
            ResetLab();
        }

        private void OnEnable()
        {
            _image.SelectionChanged += HandleSelectionChanged;
            _image.FilterGrabbed += HandleFilterGrabbed;
            _image.PixelsRevealed += HandlePixelsRevealed;
            _image.EnableInput();
            _input.Enable(transform);
        }

        private void LateUpdate() { _image.UpdateSelection(); }

        public void ShowDetails()
        {
            if (!_image.CanBeginReveal || !_image.HasSelection) { return; }
            _ui.ShowRevealStarted();
            _image.BeginReveal();
        }

        public void NextStage()
        {
            if (_image.IsRevealing) { return; }
            if (!_image.IsReadyForConvolution) { ShowDetails(); return; }
            ConvolutionRequested?.Invoke(_image.CapturePatch(), _image.SelectedPixel);
        }

        public void ResetLab()
        {
            if (_image.IsFilterHeld) { return; }
            _image.Reset();
            _ui.ResetControls();
            UpdateInstruction();
            ResetRequested?.Invoke();
        }

        public void RestoreSelection(Vector2Int selected)
        {
            _image.RestoreSelection(selected);
            HandleSelectionChanged();
        }

        private void HandleSelectionChanged()
        {
            _ui.SetSelectionAvailable(_image.HasSelection);
            UpdateInstruction();
        }
        private void HandleFilterGrabbed() { _ui.ShowFilterGrabbed(); }
        private void HandlePixelsRevealed()
        {
            _ui.ShowPixelsRevealed(_image.HasSelection);
            UpdateInstruction();
        }
        private void UpdateInstruction() { _ui.UpdateInstruction(_image.IsReadyForConvolution, _image.HasSelection); }
        private void OnDisable()
        {
            _image.SelectionChanged -= HandleSelectionChanged;
            _image.FilterGrabbed -= HandleFilterGrabbed;
            _image.PixelsRevealed -= HandlePixelsRevealed;
            _image.DisableInput(transform);
            _input.Dispose();
        }
        private void OnDestroy() { _image?.Dispose(); }
    }
}
