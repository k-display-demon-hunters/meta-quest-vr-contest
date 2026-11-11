using Core;
using Learning;
using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns stage order and the current calculation window; stages never reference each other.
    public sealed class CNNConvolutionLabManager : MonoBehaviour, ILearningExperience
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private WorkbenchBuilder _spatial;
        [SerializeField] private Font _font;
        private readonly LessonPlayback _playback = new LessonPlayback();
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
        private ResultSummaryController _summary;
        private LessonStepController _steps;

        private const float CalculationProgressLimit = 0.95f;
        private const float LaterWindowPlaybackSpeed = 1.5f;
        private bool _started;
        private bool _completed;

        public LearningStageId StageId => LearningStageId.ConvolutionLab;
        public float Progress { get; private set; }
        public bool CanExit => isActiveAndEnabled && _completed;
        public event Action<bool> ExitAvailabilityChanged;
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            _summary = new ResultSummaryController(_ui, _playback);
            _steps = new LessonStepController(_ui, _playback);
        }

        private void OnEnable()
        {
            _summary.FinishRequested += FinishPractice;
            _summary.Confirmed += ShowCompletedComparison;
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
            _input.Prepare(_calculation);
            _kernel.SetWeights(_calculation);
            _result.Clear();
            _products.Clear();
            _equations.Clear();
            StartCoroutine(RunLesson());
        }

        private void TogglePlayback() => SetPaused(!_playback.IsPaused);

        private void SetPaused(bool paused)
        {
            _playback.IsPaused = paused;
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
            var workbench = _spatial.Build(_font);
            _input = workbench.Input;
            _kernel = workbench.Kernel;
            _result = workbench.Result;
            _products = workbench.Products;
            _equations = workbench.Equations;
            _placement = new KernelInteractionController(workbench.Factory, _kernel, _spatial.PlacementTarget,
                _spatial.PlacementHint, _ui, _playback);
            _multiplication = new MultiplicationController(workbench.Factory, _input, _kernel, _products, _equations,
                workbench.Number, _ui, _playback);
            _channelSum = new ChannelSumController(workbench.Factory, _input, _kernel, _products, _equations,
                workbench.Number, _ui, _playback);
            _outputRecording = new OutputRecordingController(workbench.Factory, _kernel, _result, _equations, workbench.Number, _ui, _playback);
            _windowMovement = new WindowMovementController(_input, _kernel, _equations, _ui, _playback);
            _quiz = new ConvolutionQuizController(_result, _equations, _ui, _playback);
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

        private void ShowCompletedComparison()
        {
            ReleaseWorkbench();
            _products.SetVisible(false);
            _calculation = _calculation.ForWindow(0, 0);
            _input.ShowWindow(0, 0);
            _input.ShowChannel(0);
            _kernel.ShowChannel(0);
            _kernel.SetPose(_input.WindowPose(0, 0));
            _outputRecording.ShowComparison(_calculation);
        }

        private IEnumerator RunLesson()
        {
            _ui.Instruction = "필터 손잡이를 잡고 입력판 왼쪽 위 점선에 놓으세요.";
            yield return _placement.Run();
            _ui.Instruction = "입력값과 가중치를 곱하고, R · G · B의 결과가 합쳐지는 모습을 살펴보세요.";
            yield return RunFirstWindow();

            yield return _windowMovement.RunManual(0, 1);
            _calculation = _calculation.ForWindow(0, 1);
            _playback.Speed = 1f;
            _ui.Instruction = "오른쪽 출력판의 빈칸 8개 중 하나를 레이로 선택하세요.";
            yield return _quiz.Run(_calculation.Row * 3 + _calculation.Column);

            _playback.Speed = LaterWindowPlaybackSpeed;
            _ui.Instruction = "필터가 한 칸씩 움직일 때마다 출력값이 채워지는 모습을 살펴보세요.";
            yield return RunWindow();
            SetProgress(2f / 9f * CalculationProgressLimit);

            int[] remainingPositions = { 2, 3, 4, 5, 6, 7, 8 };
            for (int step = 0; step < remainingPositions.Length; step++)
            {
                int index = remainingPositions[step];
                yield return MoveToWindow(index / 3, index % 3, true);
                yield return RunWindow();
                SetProgress((step + 3f) / 9f * CalculationProgressLimit);
            }

            _playback.Speed = 1f;
            _ui.Instruction = "‘결과 확인’을 눌러 완성된 3×3 출력판을 살펴보세요.";
            yield return _summary.Run(_calculation.Outputs(), new[]
            {
                _calculation.Sum(0, 0, 0), _calculation.Sum(1, 0, 0), _calculation.Sum(2, 0, 0)
            });
        }

        private IEnumerator RunFirstWindow()
        {
            yield return _steps.Run(() => _multiplication.Run(_calculation, 0), "다음 · 아홉 곱 더하기");
            yield return _steps.Run(() => _channelSum.RunDetailed(_calculation, 0), "다음 · RGB 합치기");
            yield return _steps.Run(RunFirstRgbOutput, "다음 · 필터 한 칸 이동");
        }

        private IEnumerator RunFirstRgbOutput()
        {
            _ui.ResetChannelProgress();
            _ui.CompleteChannel(0, _calculation.Sum(0));
            _result.ClearCell(0);
            yield return _channelSum.RunCompact(_calculation, 1);
            yield return _channelSum.RunCompact(_calculation, 2);
            yield return _outputRecording.Run(_calculation, true);
            SetProgress(1f / 9f * CalculationProgressLimit);
        }

        private IEnumerator RunWindow()
        {
            for (int channel = 0; channel < 3; channel++)
            {
                yield return _channelSum.RunCompact(_calculation, channel);
            }
            yield return _outputRecording.Run(_calculation, false);
        }

        private IEnumerator MoveToWindow(int row, int column, bool showGuidance)
        {
            yield return _windowMovement.Run(row, column, showGuidance);
            _calculation = _calculation.ForWindow(row, column);
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
            _summary.Confirmed -= ShowCompletedComparison;
            _ui.PlaybackToggleRequested -= TogglePlayback;
            _ui.RestartRequested -= RestartPractice;
            StopPractice();
            ExitAvailabilityChanged?.Invoke(false);
        }
    }
}
