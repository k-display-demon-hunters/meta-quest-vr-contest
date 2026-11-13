using Core;
using Learning;
using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns stage order, the focus scene of each stage and the current calculation window;
    // stages never reference each other.
    public sealed class CNNConvolutionLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private LessonNarrationController _narration;
        [SerializeField] private WorkbenchBuilder _spatial;
        [SerializeField] private Font _font;
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

        private const float CalculationProgressLimit = 0.95f;
        private const float LaterWindowPlaybackSpeed = 1.5f;
        private const int SecondRowStart = 3;
        private bool _started;
        private bool _completed;

        public LearningStageId StageId => LearningStageId.ConvolutionLab;
        public float Progress { get; private set; }
        public bool CanExit => isActiveAndEnabled && _completed;
        public event Action<bool> ExitAvailabilityChanged;
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            _summary = new ResultSummaryController(_ui, _narration, _playback);
            _steps = new LessonStepController(_ui, _narration, _playback);
        }

        private void OnEnable()
        {
            _summary.FinishRequested += FinishPractice;
            _ui.PlaybackToggleRequested += TogglePlayback;
            _ui.RestartRequested += RestartPractice;
            if (_started && !_completed)
            {
                BeginPractice();
            }
            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void Start()
        {
            _started = true;
            BeginPractice();
        }

        private void BeginPractice()
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
            StartCoroutine(RunLesson());
        }

        private void TogglePlayback() => SetPaused(!_playback.IsPaused);

        private void SetPaused(bool paused)
        {
            _playback.IsPaused = paused;
            _narration.SetPaused(paused);
            _placement.SetPaused(paused);
            _quiz.SetPaused(paused);
            _windowMovement.SetPaused(paused);
            _ui.SetPaused(paused);
        }

        private void RestartPractice()
        {
            StopPractice();
            Progress = 0f;
            _completed = false;
            ExitAvailabilityChanged?.Invoke(CanExit);
            ProgressChanged?.Invoke(Progress);
            BeginPractice();
        }

        public IEnumerator EndSession()
        {
            StopPractice();
            yield break;
        }

        private void StopPractice()
        {
            // Stop the owner coroutine before closing stage inputs or restoring the shared view.
            StopAllCoroutines();
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

        private IEnumerator RunLesson()
        {
            yield return _focus.Show(FocusLayout.Input);
            yield return _placement.Run();
            yield return RunFirstWindow();

            yield return _focus.Show(FocusLayout.Input);
            yield return _windowMovement.RunManual(0, 1);
            _calculation = _calculation.ForWindow(0, 1);
            yield return _focus.Show(FocusLayout.Output);
            yield return _quiz.Run(_calculation.Row * 3 + _calculation.Column);

            yield return _focus.Show(FocusLayout.InputAndOutput);
            _playback.Speed = LaterWindowPlaybackSpeed;
            _narration.Play(NarrationCue.AutoIntro);
            yield return RunWindow();
            SetProgress(2f / 9f * CalculationProgressLimit);
            yield return _narration.WaitUntilDone();

            for (int index = 2; index < 9; index++)
            {
                if (index == SecondRowStart) { _narration.Play(NarrationCue.NextRow); }
                yield return _windowMovement.Run(index / 3, index % 3);
                _calculation = _calculation.ForWindow(index / 3, index % 3);
                yield return RunWindow();
                SetProgress((index + 1f) / 9f * CalculationProgressLimit);
            }

            _playback.Speed = 1f;
            yield return _narration.WaitUntilDone();
            yield return _focus.Show(FocusLayout.Output);
            yield return _summary.WaitForConfirmation();
            ReleaseWorkbench();
            _meaning.Prepare();
            yield return _focus.Show(FocusLayout.Meaning);
            yield return _meaning.Run();
            _summary.ShowFinish();
        }

        private IEnumerator RunFirstWindow()
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
            yield return RunNarratedChannel(1, NarrationCue.GreenChannel);
            yield return RunNarratedChannel(2, NarrationCue.BlueChannel);
            yield return _focus.Show(FocusLayout.InputAndOutput);
            yield return _outputRecording.Run(_calculation, true);
            SetProgress(1f / 9f * CalculationProgressLimit);
        }

        private IEnumerator RunNarratedChannel(int channel, NarrationCue cue)
        {
            _ui.Caption = ChannelSumContent.Compact(channel);
            _narration.Play(cue);
            yield return _channelSum.RunCompact(_calculation, channel);
            yield return _narration.WaitUntilDone();
        }

        private IEnumerator RunWindow()
        {
            _ui.Caption = $"필터 위치 {_calculation.Row + 1}행 {_calculation.Column + 1}열 · 세 층을 곱하고 더해요.";
            for (int channel = 0; channel < 3; channel++)
            {
                yield return _channelSum.RunCompact(_calculation, channel);
            }
            yield return _outputRecording.Run(_calculation, false);
        }

        private void FinishPractice()
        {
            _completed = true;
            SetProgress(1f);
            ExitAvailabilityChanged?.Invoke(CanExit);
        }

        private void SetProgress(float value)
        {
            Progress = Mathf.Max(Progress, value);
            ProgressChanged?.Invoke(Progress);
        }

        private void OnDisable()
        {
            _summary.FinishRequested -= FinishPractice;
            _ui.PlaybackToggleRequested -= TogglePlayback;
            _ui.RestartRequested -= RestartPractice;
            StopPractice();
            ExitAvailabilityChanged?.Invoke(false);
        }
    }
}
