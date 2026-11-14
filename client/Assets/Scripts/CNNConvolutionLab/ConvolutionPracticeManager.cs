using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns the practice's internal objects and their lifetime; the scene chooses stage order.
    internal sealed class ConvolutionPracticeManager
    {
        private readonly LessonUIController _ui;
        private readonly LessonNarrationController _narration;
        private readonly WorkbenchBuilder _spatial;
        private readonly Font _font;
        private readonly Texture2D _pictureSource;
        private readonly LessonPlayback _playback = new LessonPlayback();
        private readonly ResultSummaryController _summary;
        private FocusLayoutController _focus;
        private WorkbenchKernelController _kernel;
        private FeatureMapDisplayController _map;
        private bool _initialized;
        private PictureConvolution _convolution;
        // Where the filter is first placed and worked out: x = column, y = row.
        private Vector2Int _start;
        private KernelInteractionController _placement;
        private FormulaController _formula;
        private FilterSweepController _sweep;

        public event Action FinishRequested;
        public event Action RestartRequested;

        public ConvolutionPracticeManager(LessonUIController ui, LessonNarrationController narration,
            WorkbenchBuilder spatial, Font font, Texture2D picture)
        {
            _ui = ui;
            _narration = narration;
            _spatial = spatial;
            _font = font;
            _pictureSource = picture;
            _summary = new ResultSummaryController(ui, narration);
        }

        public void Subscribe()
        {
            _summary.FinishRequested += HandleFinish;
            _ui.RestartRequested += HandleRestart;
            _ui.PlaybackToggleRequested += TogglePlayback;
        }

        public void Unsubscribe()
        {
            _summary.FinishRequested -= HandleFinish;
            _ui.RestartRequested -= HandleRestart;
            _ui.PlaybackToggleRequested -= TogglePlayback;
        }

        private void HandleFinish() => FinishRequested?.Invoke();
        private void HandleRestart() => RestartRequested?.Invoke();
        private void TogglePlayback() => SetPaused(!_playback.IsPaused);

        public void Prepare()
        {
            if (!_initialized)
            {
                _ui.Initialize();
                InitializeWorkbench();
                _initialized = true;
            }

            _ui.BeginCalculation();
            SetPaused(false);
            _spatial.gameObject.SetActive(true);
            _focus.Hide();
            // The first scene grows in with the filter already at its start position.
            _kernel.ResetToHome();
            _sweep.Prepare();
        }

        private void SetPaused(bool paused)
        {
            _playback.IsPaused = paused;
            _narration.SetPaused(paused);
            _placement.SetPaused(paused);
            _sweep.SetPaused(paused);
            _ui.SetPaused(paused);
        }

        public void Cancel()
        {
            _narration.Stop();
            if (_initialized)
            {
                _summary.Cancel();
                _placement.Cancel();
                _formula.Cancel();
                _sweep.Cancel();
            }

            _ui.gameObject.SetActive(false);
            _spatial.gameObject.SetActive(false);
        }

        private void InitializeWorkbench()
        {
            var workbench = _spatial.Build(_font, _playback);
            _focus = workbench.Focus;
            _kernel = workbench.Kernel;
            _map = workbench.Map;
            Color32[] pixels = PictureSampler.Sample(_pictureSource);
            workbench.Picture.Show(pixels);
            _convolution = new PictureConvolution(pixels);
            _start = _convolution.StrongestWindow();
            _kernel.SetWeights(_convolution);
            _kernel.SetTarget(workbench.Picture.WindowPose(_start.y, _start.x));
            _placement = new KernelInteractionController(workbench.Factory, _kernel, _spatial.PlacementTarget,
                _spatial.PlacementHint, _ui, _narration, _playback);
            _formula = new FormulaController(workbench.Factory, _kernel, _spatial.FormulaAnchor, _ui, _narration, _playback);
            _sweep = new FilterSweepController(_kernel, workbench.Picture, _map, _convolution, _ui, _narration, _playback);
        }

        public IEnumerator PlaceFilter()
        {
            yield return _focus.Show(FocusLayout.Picture);
            yield return _placement.Run();
        }

        // One filter position is worked out as a formula; its sum then drops into the same cell of the
        // feature map that appears beside the picture.
        public IEnumerator CalculateFirstOutput()
        {
            yield return _focus.Show(FocusLayout.PictureAndFormula);
            yield return _formula.Run(_convolution, _start.y, _start.x);
            yield return _focus.Show(FocusLayout.PictureAndMap);
            yield return _formula.SendSum(_map.CellPosition(_start.y, _start.x));
            _sweep.Record(_start.y, _start.x);
            yield return _narration.WaitUntilDone();
        }

        // The learner sweeps the filter over the picture and the feature map fills in.
        public IEnumerator SweepPicture() => _sweep.Run();

        // Picture and finished feature map stay side by side while the result is summed up.
        public IEnumerator ExplainOutput() => _summary.Run();
    }
}
