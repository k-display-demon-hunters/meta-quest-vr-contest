using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns stage order and the current calculation window; stages never reference each other.
    public sealed class ConvolutionLessonManager : MonoBehaviour
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private WorkbenchBuilder _spatial;
        [SerializeField] private Font _font;
        private InputGridController _input;
        private WorkbenchKernelController _kernel;
        private ResultGridController _result;
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

        public event Action Completed;
        public event Action ContinueRequested;
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            _summary = new ResultSummaryController(_ui);
        }

        private void OnEnable()
        {
            _summary.ContinueRequested += HandleContinue;
            _summary.Confirmed += HideWorkbench;
        }

        public void BeginPractice()
        {
            StopPractice();
            if (!_initialized)
            {
                _ui.Initialize();
                InitializeWorkbench();
                _initialized = true;
            }
            _calculation = ConvolutionCalculation.CreateExample();
            _ui.BeginCalculation();
            _spatial.gameObject.SetActive(true);
            _spatial.Show();
            _input.Prepare(_calculation);
            _kernel.SetWeights(_calculation);
            _result.BeginProducts();
            _equations.Clear();
            ProgressChanged?.Invoke(0f);
            StartCoroutine(RunLesson());
        }

        public void StopPractice()
        {
            // Stop the owner coroutine before closing stage inputs or restoring the shared view.
            StopAllCoroutines();
            if (_initialized)
            {
                _summary.Cancel();
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
            _equations = workbench.Equations;
            _placement = new KernelInteractionController(workbench.Factory, _kernel, _spatial.PlacementTarget, _spatial.PlacementHint, _ui);
            _multiplication = new MultiplicationController(workbench.Factory, _input, _kernel, _result, _equations, workbench.Number, _ui);
            _channelSum = new ChannelSumController(workbench.Factory, _input, _result, _equations, workbench.Number, _ui);
            _outputRecording = new OutputRecordingController(_kernel, _result, _equations, workbench.Number, _ui);
            _windowMovement = new WindowMovementController(_input, _kernel, _equations, _ui);
            _quiz = new ConvolutionQuizController(_result, _equations, _ui);
        }

        private void HideWorkbench()
        {
            ReleaseWorkbench();
            _spatial.gameObject.SetActive(false);
        }

        private void ReleaseWorkbench()
        {
            _placement.Cancel();
            _quiz.Cancel();
            _multiplication.Cancel();
            _channelSum.Cancel();
        }

        private IEnumerator RunLesson()
        {
            yield return _placement.Run();
            yield return RunFirstWindow();
            ProgressChanged?.Invoke(1f / 9f);

            yield return MoveToWindow(0, 1, false);
            yield return _quiz.Run(_calculation.Row * 3 + _calculation.Column);
            yield return RunWindow();
            ProgressChanged?.Invoke(2f / 9f);

            int[] remainingPositions = { 2, 5, 4, 3, 6, 7, 8 };
            for (int step = 0; step < remainingPositions.Length; step++)
            {
                int index = remainingPositions[step];
                yield return MoveToWindow(index / 3, index % 3, true);
                yield return RunWindow();
                ProgressChanged?.Invoke((step + 3f) / 9f);
            }

            yield return _summary.Run(_calculation.Outputs(), new[]
            {
                _calculation.Sum(0, 0, 0), _calculation.Sum(1, 0, 0), _calculation.Sum(2, 0, 0)
            });
            Completed?.Invoke();
        }

        private IEnumerator RunFirstWindow()
        {
            yield return _multiplication.Run(_calculation, 0);
            yield return _channelSum.RunDetailed(_calculation, 0);
            yield return _channelSum.RunCompact(_calculation, 1, true);
            yield return _channelSum.RunCompact(_calculation, 2, true);
            yield return _outputRecording.Run(_calculation, true);
        }

        private IEnumerator RunWindow()
        {
            for (int channel = 0; channel < 3; channel++)
            {
                yield return _channelSum.RunCompact(_calculation, channel, false);
            }
            yield return _outputRecording.Run(_calculation, false);
        }

        private IEnumerator MoveToWindow(int row, int column, bool showGuidance)
        {
            yield return _windowMovement.Run(row, column, showGuidance);
            _calculation = _calculation.ForWindow(row, column);
        }

        private void HandleContinue()
        {
            StopPractice();
            ContinueRequested?.Invoke();
        }

        private void OnDisable()
        {
            _summary.ContinueRequested -= HandleContinue;
            _summary.Confirmed -= HideWorkbench;
            StopPractice();
        }
    }
}
