using Core;
using System;
using Learning;
using System.Collections;
using UnityEngine;

namespace CNNScreen
{
    public sealed class CNNScreenLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private PixelImageFactory _imageFactory;
        [SerializeField] private PixelBoardController _board;
        [SerializeField] private PixelSelectionController _selection;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private ScreenLabUIController _ui;
        private PixelImageData _imageData;
        private PixelPatchData _selectedPatch;
        private Vector2Int _patchOrigin;
        private int _pixelPage = -1;
        private int _rgbPage;
        private bool _expanding;
        private bool _learningComplete;
        public LearningStageId StageId => LearningStageId.ScreenLab;
        public float Progress => _learningComplete ? 1f : 0f;
        public bool CanExit => isActiveAndEnabled && _learningComplete;
        public PixelPatchData SelectedPatch => _selectedPatch;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;
        public IEnumerator EndSession()
        {
            StopAllCoroutines();
            _ui.DisableInput();
            yield break;
        }
        private void PublishState()
        {
            ProgressChanged?.Invoke(Progress);
            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void Start()
        {
            _imageData = _imageFactory.Create();
            _board.ShowPhoto(_imageData.PhotoTexture);
            _selection.Initialize(_imageData);
            _ui.Initialize();
            ResetLab();
        }
        private void OnEnable()
        {
            _selection.SelectionChanged += HandleSelectionChanged;
            _selection.FilterGrabbed += HandleFilterGrabbed;
            _revealStage.PixelsRevealed += HandlePixelsRevealed;
            _ui.DetailsRequested += ShowDetails;
            _ui.NextRequested += NextStage;
            _ui.ResetRequested += ResetLab;
            _ui.EnableInput();
        }
        public void ShowDetails()
        {
            if (_pixelPage < 2 || _selection.IsFilterHeld || !_selection.HasSelection || _expanding
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
            if (_selection.IsFilterHeld || _expanding) { return; }
            if (_learningComplete) { return; }
            if (_revealStage.IsReadyForConvolution)
            {
                _rgbPage++;
                if (_rgbPage < 3) { _ui.ShowRgbPage(_rgbPage); return; }
                _learningComplete = true;
                _ui.ShowExitGuide();
                PublishState();
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
            _expanding = _learningComplete = false;
            _pixelPage = -1;
            _selectedPatch = null;
            _revealStage.ResetStage();
            _board.RestorePresentation();
            _selection.ResetSelection();
            _ui.SetVisible(true);
            _ui.ResetControls();
            PublishState();
        }
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
        private void OnDisable()
        {
            StopAllCoroutines();
            _selection.SelectionChanged -= HandleSelectionChanged;
            _selection.FilterGrabbed -= HandleFilterGrabbed;
            _revealStage.PixelsRevealed -= HandlePixelsRevealed;
            _ui.DetailsRequested -= ShowDetails;
            _ui.NextRequested -= NextStage;
            _ui.ResetRequested -= ResetLab;
            _ui.DisableInput();
            PublishState();
        }
        private void OnDestroy() { _imageData?.Dispose(); }
    }
}
