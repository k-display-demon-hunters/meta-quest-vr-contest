using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns stage order and the current calculation window; stages never reference each other.
    public sealed class ConvolutionLessonManager : MonoBehaviour
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private RgbVolumeController _spatial;
        [SerializeField] private Font _font;
        private bool _initialized;
        private ConvolutionCalculation _calculation;
        private KernelInteractionController _placement;
        private CalculationLessonController _calculationLesson;
        private ConvolutionQuizController _quiz;
        private ResultSummaryController _summary;

        public event Action Completed;
        public event Action ContinueRequested;
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            _placement = new KernelInteractionController(_spatial, _ui);
            _calculationLesson = new CalculationLessonController(_spatial, _ui);
            _quiz = new ConvolutionQuizController(_spatial, _ui);
            _summary = new ResultSummaryController(_spatial, _ui);
        }

        private void OnEnable()
        {
            _summary.ContinueRequested += HandleContinue;
        }

        public void BeginPractice()
        {
            StopPractice();
            if (!_initialized)
            {
                _ui.Initialize();
                _spatial.Initialize(_font);
                _initialized = true;
            }
            _calculation = ConvolutionCalculation.CreateExample();
            _ui.BeginCalculation();
            _spatial.gameObject.SetActive(true);
            _spatial.SetData(_calculation);
            _spatial.PrepareSlidingPractice();
            ProgressChanged?.Invoke(0f);
            StartCoroutine(RunLesson());
        }

        public void StopPractice()
        {
            // Stop the owner coroutine before closing stage inputs or restoring the shared view.
            StopAllCoroutines();
            if (_initialized)
            {
                _placement.Cancel();
                _quiz.Cancel();
                _summary.Cancel();
                _spatial.ReleaseInteractions();
            }
            _ui.gameObject.SetActive(false);
            _spatial.gameObject.SetActive(false);
        }

        private IEnumerator RunLesson()
        {
            yield return _placement.Run();
            yield return _calculationLesson.RunFirstWindow(_calculation);
            ProgressChanged?.Invoke(1f / 9f);

            yield return MoveToWindow(0, 1, false);
            yield return _quiz.Run(_calculation.Row * 3 + _calculation.Column);
            yield return _calculationLesson.RunWindow(_calculation);
            ProgressChanged?.Invoke(2f / 9f);

            int[] remainingPositions = { 2, 5, 4, 3, 6, 7, 8 };
            for (int step = 0; step < remainingPositions.Length; step++)
            {
                int index = remainingPositions[step];
                yield return MoveToWindow(index / 3, index % 3, true);
                yield return _calculationLesson.RunWindow(_calculation);
                ProgressChanged?.Invoke((step + 3f) / 9f);
            }

            yield return _summary.Run(_calculation.Outputs(), new[]
            {
                _calculation.Sum(0, 0, 0), _calculation.Sum(1, 0, 0), _calculation.Sum(2, 0, 0)
            });
            Completed?.Invoke();
        }

        private IEnumerator MoveToWindow(int row, int column, bool showGuidance)
        {
            yield return _calculationLesson.MoveToWindow(row, column, showGuidance);
            _calculation = _calculation.ForWindow(row, column);
            _spatial.SetWindow(_calculation);
        }

        private void HandleContinue()
        {
            StopPractice();
            ContinueRequested?.Invoke();
        }

        private void OnDisable()
        {
            _summary.ContinueRequested -= HandleContinue;
            StopPractice();
        }
    }
}
