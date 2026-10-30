using System;
using UnityEngine;

namespace UI
{
    public sealed class CNNScreenLabManager : MonoBehaviour
    {
        [SerializeField] private PixelImageFactory _imageFactory;
        [SerializeField] private PixelBoardController _board;
        [SerializeField] private PixelSelectionController _selection;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private PixelLabUIController _ui;
        private PixelImageData _imageData;
        private PixelPatchData _selectedPatch;
        private Vector2Int _patchOrigin;
        private Vector2Int? _initialSelection;
        public event Action<PixelPatchData, Vector2Int> ConvolutionRequested;
        public event Action ResetRequested;

        private bool IsRevealing => _revealStage.Phase == PixelObservationPhase.AwaitingClick
            || _revealStage.Phase == PixelObservationPhase.Revealing
            || _revealStage.Phase == PixelObservationPhase.Unfolding;

        private void Start()
        {
            _imageData = _imageFactory.Create();
            _board.ShowPhoto(_imageData.PhotoTexture);
            _selection.Initialize(_imageData);
            _ui.Initialize();
            ResetLab();
            if (_initialSelection.HasValue)
            {
                _selection.RestoreSelection(_initialSelection.Value);
                HandleSelectionChanged();
            }
        }

        private void OnEnable()
        {
            _selection.SelectionChanged += HandleSelectionChanged;
            _selection.FilterGrabbed += HandleFilterGrabbed;
            _revealStage.PixelsRevealed += HandlePixelsRevealed;
            _revealStage.PhaseChanged += HandlePhaseChanged;
            _ui.DetailsRequested += ShowDetails;
            _ui.NextRequested += NextStage;
            _ui.ResetRequested += ResetLab;
            _ui.EnableInput();
        }

        public void ShowDetails()
        {
            if (_selection.IsFilterHeld || !_selection.HasSelection
                || _revealStage.Phase != PixelObservationPhase.Inactive) { return; }
            _patchOrigin = _selection.SelectedPixel;
            _selectedPatch = _imageData.CapturePatch(_patchOrigin);
            _ui.ShowRevealStarted();
            _selection.SuspendSelection();
            _revealStage.ShowImage(_imageData.Pixels, _imageData.Columns, _imageData.Rows, _patchOrigin);
            _revealStage.RevealPixels(_selectedPatch);
        }

        public void NextStage()
        {
            if (IsRevealing) { return; }
            if (!_revealStage.IsReadyForConvolution) { ShowDetails(); return; }
            ConvolutionRequested?.Invoke(_selectedPatch, _patchOrigin);
        }

        public void ResetLab()
        {
            if (_selection.IsFilterHeld) { return; }
            _selectedPatch = null;
            _revealStage.ResetStage();
            _board.SetPhotoVisible(true);
            _selection.ResetSelection();
            _ui.ResetControls();
            UpdateInstruction();
            ResetRequested?.Invoke();
        }

        // Direct stage entry uses the same source image without claiming pixel-lesson completion.
        public PixelPatchData CreateConvolutionInput()
        {
            using (PixelImageData image = _imageFactory.Create())
            {
                return image.CapturePatch(new Vector2Int(image.Columns / 2, image.Rows / 2));
            }
        }

        public void SetInitialSelection(Vector2Int selected)
        {
            _initialSelection = selected;
        }

        private void HandleSelectionChanged()
        {
            _selectedPatch = null;
            _ui.SetSelectionAvailable(_selection.HasSelection);
            UpdateInstruction();
        }
        private void HandleFilterGrabbed() { _ui.ShowFilterGrabbed(); }
        private void HandlePixelsRevealed()
        {
            _ui.ShowPixelsRevealed(_selection.HasSelection);
            UpdateInstruction();
        }
        private void HandlePhaseChanged(PixelObservationPhase phase)
        {
            _board.SetPhotoVisible(phase != PixelObservationPhase.Unfolding && phase != PixelObservationPhase.Revealed);
            _ui.ShowRevealPhase(phase);
        }
        private void UpdateInstruction() { _ui.UpdateInstruction(_revealStage.IsReadyForConvolution, _selection.HasSelection); }

        private void OnDisable()
        {
            _selection.SelectionChanged -= HandleSelectionChanged;
            _selection.FilterGrabbed -= HandleFilterGrabbed;
            _revealStage.PixelsRevealed -= HandlePixelsRevealed;
            _revealStage.PhaseChanged -= HandlePhaseChanged;
            _ui.DetailsRequested -= ShowDetails;
            _ui.NextRequested -= NextStage;
            _ui.ResetRequested -= ResetLab;
            _ui.DisableInput();
        }
        private void OnDestroy() { _imageData?.Dispose(); }
    }
}
