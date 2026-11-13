using System;
using Learning;
using System.Collections;
using UnityEngine;

namespace CNNScreen
{
    public sealed class CNNScreenLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private PixelImageFactory _imageFactory;
        [SerializeField] private PixelGridData _pixelGrid;
        [SerializeField] private PixelBoardController _board;
        [SerializeField] private PixelSelectionController _selection;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private ScreenLabUIController _ui;
        private PixelImageData _imageData;
        private PixelPatchData _selectedPatch;
        private Vector2Int _patchOrigin;
        private float _dwell;
        private Vector2Int _dwellOrigin;
        private bool _expanding;
        private bool _learningComplete;
        public LearningStageId StageId => LearningStageId.ScreenLab;
        public float Progress => _learningComplete ? 1f : 0f;
        public bool CanExit => isActiveAndEnabled && _learningComplete;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;
        public IEnumerator EndSession()
        {
            StopAllCoroutines();
            _ui.StopGuidance();
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
            _selection.Initialize(_pixelGrid);
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
        }
        private void Update()
        {
            if (_revealStage.Phase != PixelObservationPhase.Inactive || _expanding) { return; }
            if (!_selection.IsOnImage) { _dwell = 0; return; }
            if (_dwellOrigin != _selection.SelectedPixel)
            {
                _dwellOrigin = _selection.SelectedPixel;
                _dwell = 0;
            }
            _dwell += Time.unscaledDeltaTime;
            if (_dwell >= 1f) { ShowDetails(); }
        }
        public void ShowDetails()
        {
            if (!_selection.HasSelection || _expanding
                || _revealStage.Phase != PixelObservationPhase.Inactive) { return; }
            _patchOrigin = _selection.SelectedPixel;
            if (!_pixelGrid.TryCapturePatch(_selection.SelectionWorldPosition, out _selectedPatch)) { return; }
            _expanding = true;
            _ui.ShowMoving();
            _ui.RequestGuidance(1);
            StartCoroutine(ExpandSelection());
        }
        private IEnumerator ExpandSelection()
        {
            yield return null;
            _revealStage.ShowImage(_imageData.Pixels, _pixelGrid.gridWidth, _pixelGrid.gridHeight, _patchOrigin);
            _revealStage.RevealPixels(_selectedPatch);
        }
        public void NextStage()
        {
            if (_expanding) { return; }
            if (_learningComplete) { return; }
            if (_revealStage.IsReadyForConvolution)
            {
                _learningComplete = true;
                _ui.ShowExitGuide();
                PublishState();
                return;
            }
        }
        public void ResetLab()
        {
            if (_selection.IsFilterHeld) { return; }
            StopAllCoroutines();
            _expanding = _learningComplete = false;
            _dwell = 0;
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
            if (_revealStage.Phase != PixelObservationPhase.Inactive)
            {
                if (_pixelGrid.TryCapturePatch(_selection.SelectionWorldPosition, out _selectedPatch))
                {
                    _revealStage.UpdatePatch(_selectedPatch);
                    if (!_expanding) { _ui.RequestGuidance(2); }
                }
                return;
            }
            _ui.ShowObservationHint();
        }
        private void HandleFilterGrabbed()
        {
            if (_revealStage.Phase == PixelObservationPhase.Inactive)
            {
                _ui.ShowFilterGrabbed();
                _ui.RequestGuidance(0);
            }
        }
        private void HandlePixelsRevealed()
        {
            HandleSelectionChanged();
            _expanding = false;
            _ui.ShowExplorationHint();
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
            PublishState();
        }
        private void OnDestroy() { _imageData?.Dispose(); }
    }
}
