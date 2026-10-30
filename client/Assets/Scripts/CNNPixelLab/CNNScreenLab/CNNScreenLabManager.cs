using System;
using System.Collections;
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
        [SerializeField] private PixelPracticeManager _practice;
        private PixelImageData _imageData;
        private PixelPatchData _selectedPatch;
        private Vector2Int _patchOrigin;
        private Vector2Int? _initialSelection;
        private int _pixelPage = -1;
        private int _rgbPage;
        private bool _expanding;
        private bool _practicing;
        private bool _learningComplete;
        public event Action<PixelPatchData, Vector2Int> ConvolutionRequested;
        public event Action ResetRequested;

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
            _practice.Completed += HandlePracticeCompleted;
            _ui.DetailsRequested += ShowDetails;
            _ui.NextRequested += NextStage;
            _ui.ResetRequested += ResetLab;
            _ui.EnableInput();
        }
        public void ShowDetails()
        {
            if (_pixelPage < 2 || _selection.IsFilterHeld || !_selection.HasSelection || _expanding || _practicing
                || _revealStage.Phase != PixelObservationPhase.Inactive) { return; }
            _patchOrigin = _selection.SelectedPixel;
            _selectedPatch = _imageData.CapturePatch(_patchOrigin);
            _expanding = true;
            _selection.SuspendSelection();
            _ui.ShowMoving();
            StartCoroutine(ExpandSelection());
        }
        private IEnumerator ExpandSelection()
        {
            yield return _board.MoveAside();
            _revealStage.ShowImage(_imageData.Pixels, _imageData.Columns, _imageData.Rows, _patchOrigin);
            _revealStage.RevealPixels(_selectedPatch);
        }
        public void NextStage()
        {
            if (_selection.IsFilterHeld || _expanding || _practicing) { return; }
            if (_learningComplete) { ConvolutionRequested?.Invoke(_selectedPatch, _patchOrigin); return; }
            if (_revealStage.IsReadyForConvolution)
            {
                _rgbPage++;
                if (_rgbPage < 3) { _ui.ShowRgbPage(_rgbPage); return; }
                _practicing = true;
                _revealStage.ResetStage();
                _ui.SetVisible(false);
                _practice.BeginPractice(_selectedPatch);
                return;
            }
            if (_pixelPage >= 0 && _pixelPage < 2)
            {
                _pixelPage++;
                _ui.ShowPixelPage(_pixelPage);
            }
        }
        public void ResetLab()
        {
            if (_selection.IsFilterHeld) { return; }
            StopAllCoroutines();
            _practice.StopPractice();
            _expanding = _practicing = _learningComplete = false;
            _pixelPage = -1;
            _selectedPatch = null;
            _revealStage.ResetStage();
            _board.RestorePresentation();
            _selection.ResetSelection();
            _ui.SetVisible(true);
            _ui.ResetControls();
            ResetRequested?.Invoke();
        }
        public void SetInitialSelection(Vector2Int selected) { _initialSelection = selected; }
        private void HandleSelectionChanged()
        {
            if (!_selection.HasSelection) { _ui.ResetControls(); return; }
            if (_pixelPage < 0) { _pixelPage = 0; }
            _ui.ShowPixelPage(_pixelPage);
        }
        private void HandleFilterGrabbed() { _ui.ShowFilterGrabbed(); }
        private void HandlePixelsRevealed()
        {
            _expanding = false;
            _rgbPage = 0;
            _ui.ShowRgbPage(0);
        }
        private void HandlePracticeCompleted()
        {
            _practicing = false;
            _learningComplete = true;
            _ui.SetVisible(true);
            _ui.ShowExitGuide();
            ConvolutionRequested?.Invoke(_selectedPatch, _patchOrigin);
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            _selection.SelectionChanged -= HandleSelectionChanged;
            _selection.FilterGrabbed -= HandleFilterGrabbed;
            _revealStage.PixelsRevealed -= HandlePixelsRevealed;
            _practice.Completed -= HandlePracticeCompleted;
            _ui.DetailsRequested -= ShowDetails;
            _ui.NextRequested -= NextStage;
            _ui.ResetRequested -= ResetLab;
            _ui.DisableInput();
        }
        private void OnDestroy() { _imageData?.Dispose(); }
    }
}
