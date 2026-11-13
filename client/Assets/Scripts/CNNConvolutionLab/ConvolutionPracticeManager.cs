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
        private readonly LessonPlayback _playback = new LessonPlayback();
        private FocusLayoutController _focus;
        private InputGridController _input;
        private WorkbenchKernelController _kernel;
        private ResultGridController _result;
        private ResultGridController _products;
        private EquationDisplayController _equations;
        private bool _initialized;
        private ConvolutionCalculation _calculation;
        private KernelInteractionController _placement;
        private MultiplicationController _multiplication;
        private ChannelSumController _channelSum;
        private OutputRecordingController _outputRecording;
        private WindowMovementController _windowMovement;
        private ConvolutionQuizController _quiz;
        private FeatureMeaningController _meaning;
        private ResultSummaryController _summary;
        private LessonStepController _steps;

        public event Action FinishRequested;
        public event Action RestartRequested;
        public event Action FirstOutputRecorded;

        public ConvolutionPracticeManager(LessonUIController ui, LessonNarrationController narration,
            WorkbenchBuilder spatial, Font font)
        {
            _ui = ui;
            _narration = narration;
            _spatial = spatial;
            _font = font;
            _summary = new ResultSummaryController(ui, narration, _playback);
            _steps = new LessonStepController(ui, narration, _playback);
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

            _calculation = ConvolutionCalculation.CreateExample();
            _playback.Speed = 1f;
            _ui.BeginCalculation();
            SetPaused(false);
            _spatial.gameObject.SetActive(true);
            _focus.Hide();
            _input.Prepare(_calculation);
            _kernel.SetWeights(_calculation);
            // The first scene grows in with the filter already at its start position.
            _kernel.RestoreParent();
            _kernel.SetPose(_kernel.HomePose);
            _result.Clear();
            _products.Clear();
            _equations.Clear();
        }

        private void SetPaused(bool paused)
        {
            _playback.IsPaused = paused;
            _narration.SetPaused(paused);
            _placement.SetPaused(paused);
            _quiz.SetPaused(paused);
            _windowMovement.SetPaused(paused);
            _ui.SetPaused(paused);
        }

        public void Cancel()
        {
            _narration.Stop();
            if (_initialized)
            {
                _summary.Cancel();
                _steps.Cancel();
                ReleaseWorkbench();
            }

            _ui.gameObject.SetActive(false);
            _spatial.gameObject.SetActive(false);
        }

        private void InitializeWorkbench()
        {
            var workbench = _spatial.Build(_font, _playback);
            _focus = workbench.Focus;
            _input = workbench.Input;
            _kernel = workbench.Kernel;
            _result = workbench.Result;
            _products = workbench.Products;
            _equations = workbench.Equations;
            _placement = new KernelInteractionController(workbench.Factory, _kernel, _spatial.PlacementTarget,
                _spatial.PlacementHint, _ui, _narration, _playback);
            _multiplication = new MultiplicationController(workbench.Factory, _input, _kernel, _products, _equations,
                workbench.Number, _ui, _narration, _playback);
            _channelSum = new ChannelSumController(workbench.Factory, _input, _kernel, _products, _equations,
                workbench.Number, _ui, _narration, _playback);
            _outputRecording = new OutputRecordingController(workbench.Factory, _kernel, _result, _equations,
                workbench.Number, _ui, _narration, _playback);
            _windowMovement = new WindowMovementController(_input, _kernel, _equations, _ui, _narration, _playback);
            _quiz = new ConvolutionQuizController(_result, _equations, _ui, _narration, _playback);
            _meaning = new FeatureMeaningController(workbench.Factory, _spatial.MeaningAnchor, _ui, _narration, _playback);
        }

        private void ReleaseWorkbench()
        {
            _placement.Cancel();
            _quiz.Cancel();
            _multiplication.Cancel();
            _channelSum.Cancel();
            _outputRecording.Cancel();
            _windowMovement.Cancel();
        }

        public IEnumerator PlaceFilter()
        {
            yield return _focus.Show(FocusLayout.Input);
            yield return _placement.Run();
        }

        public IEnumerator MoveAndQuiz()
        {
            yield return _focus.Show(FocusLayout.Input);
            yield return _windowMovement.RunManual(0, 1);
            _calculation = _calculation.ForWindow(0, 1);
            yield return _focus.Show(FocusLayout.Output);
            yield return _quiz.Run(1);
        }

        public IEnumerator BeginAutomaticCalculation()
        {
            yield return _focus.Show(FocusLayout.InputAndOutput);
            _playback.Speed = 1.5f;
            _narration.Play(NarrationCue.AutoIntro);
            yield return RunWindow();
        }

        public IEnumerator WaitForNarration() => _narration.WaitUntilDone();

        public IEnumerator CalculateWindow(int index)
        {
            if (index == 3) { _narration.Play(NarrationCue.NextRow); }
            yield return _windowMovement.Run(index / 3, index % 3);
            _calculation = _calculation.ForWindow(index / 3, index % 3);
            yield return RunWindow();
        }

        public IEnumerator ConfirmResult()
        {
            _playback.Speed = 1f;
            yield return _narration.WaitUntilDone();
            yield return _focus.Show(FocusLayout.Output);
            yield return _summary.WaitForConfirmation();
        }

        public IEnumerator ShowMeaning()
        {
            ReleaseWorkbench();
            _meaning.Prepare();
            yield return _focus.Show(FocusLayout.Meaning);
            yield return _meaning.Run();
            _summary.ShowFinish();
        }

        public IEnumerator RunFirstWindow()
        {
            yield return _steps.Run(RunFirstMultiplication, "다음 · 아홉 곱 더하기");
            yield return _steps.Run(RunFirstChannelSum, "다음 · RGB 합치기");
            yield return _steps.Run(RunFirstRgbOutput, "다음 · 필터 한 칸 이동");
        }

        private IEnumerator RunFirstMultiplication()
        {
            yield return _focus.Show(FocusLayout.InputAndProducts);
            yield return _multiplication.Run(_calculation, 0);
        }

        private IEnumerator RunFirstChannelSum()
        {
            yield return _focus.Show(FocusLayout.Products);
            yield return _channelSum.RunDetailed(_calculation, 0);
        }

        private IEnumerator RunFirstRgbOutput()
        {
            _result.ClearCell(0);
            yield return _focus.Show(FocusLayout.InputAndProducts);
            yield return _channelSum.RunNarrated(_calculation, 1);
            yield return _channelSum.RunNarrated(_calculation, 2);
            yield return _focus.Show(FocusLayout.InputAndOutput);
            yield return _outputRecording.Run(_calculation, true);
            FirstOutputRecorded?.Invoke();
        }

        private IEnumerator RunWindow()
        {
            yield return _channelSum.RunAllChannels(_calculation);
            yield return _outputRecording.Run(_calculation, false);
        }
    }
}
