using System;
using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // Owns the learning sequence; spatial display and input remain in their controllers.
    public sealed class ConvolutionLessonManager : MonoBehaviour
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private RgbVolumeController _spatial;
        [SerializeField] private Font _font;
        private bool _initialized;
        private bool _running;
        private bool _summaryReady;
        private ConvolutionQuizController _quiz;
        public event Action Completed;
        public event Action ContinueRequested;
        public event Action<float> ProgressChanged;

        private void OnEnable()
        {
            _ui.SkipRequested += HandleAssist;
            _ui.AdvanceRequested += HandleContinue;
        }

        public void BeginPractice()
        {
            BeginCalculation(ConvolutionCalculation.CreateExample());
        }

        private void BeginCalculation(ConvolutionCalculation calculation)
        {
            StopAllCoroutines();
            _quiz?.Cancel();
            _quiz = null;
            if (!_initialized)
            {
                _ui.Initialize();
                _spatial.Initialize(_font);
                _initialized = true;
            }
            _summaryReady = false;
            _running = true;
            _ui.BeginCalculation();
            _spatial.gameObject.SetActive(true);
            _spatial.SetData(calculation);
            _spatial.PrepareSlidingPractice();
            ProgressChanged?.Invoke(0f);
            StartCoroutine(RunLesson(calculation));
        }

        private IEnumerator RunLesson(ConvolutionCalculation calculation)
        {
            _ui.Title = "필터가 지나간 자리";
            _ui.Explanation = "손잡이를 잡아 입력판 맨 왼쪽 위의 점선에 놓으세요.";
            _ui.Guide = "고정 학습 예제 · 입력 5×5 · 필터 3×3";
            _ui.Feedback = "계산을 익히기 위한 고정 예제예요. 각 채널의 값은 0~3이에요.";
            yield return _spatial.WaitForKernel();
            _ui.ShowSkip(false);
            RedCalculationController firstChannel = new RedCalculationController(_spatial, _ui, calculation);
            yield return firstChannel.Run();
            yield return ShowCompactChannel(calculation, 1, true);
            yield return ShowCompactChannel(calculation, 2, true);
            _ui.Title = "세 채널의 합이 출력값 하나가 돼요";
            _ui.Explanation = "오른쪽 판은 지금부터 출력값이 채워지는 지도예요.";
            _spatial.BeginOutputMap();
            yield return RecordOutput(calculation);
            ProgressChanged?.Invoke(1f / 9f);

            _ui.ResetChannelProgress();
            _spatial.ClearEquations();
            yield return _spatial.MoveWindow(0, 1);
            _quiz = new ConvolutionQuizController(_spatial, _ui, calculation);
            yield return _quiz.Run();
            _quiz.Cancel();
            yield return CalculateCurrentWindow(calculation);
            ProgressChanged?.Invoke(2f / 9f);

            int[] remainingPositions = { 2, 5, 4, 3, 6, 7, 8 };
            for (int step = 0; step < remainingPositions.Length; step++)
            {
                int index = remainingPositions[step];
                _ui.ResetChannelProgress();
                _spatial.ClearEquations();
                _ui.Title = "필터를 한 칸 옮겨요";
                _ui.Explanation = "새 위치에서도 R · G · B를 각각 계산해요.";
                yield return _spatial.MoveWindow(index / 3, index % 3);
                yield return CalculateCurrentWindow(calculation);
                ProgressChanged?.Invoke((step + 3f) / 9f);
            }

            _running = false;
            _spatial.EndPositionQuiz();
            _ui.Title = "아홉 위치의 계산이 끝났어요";
            _ui.Explanation = "입력판 위를 이동하며 출력값 9개를 만들었어요.";
            _ui.Guide = "입력 5×5 → 필터 3×3 → 출력 3×3";
            _ui.Feedback = "결과 확인을 눌러 완성된 지도를 살펴보세요.";
            yield return _ui.WaitForNext("결과 확인");
            _spatial.ReleaseInteractions();
            _spatial.gameObject.SetActive(false);
            _ui.ShowOutputSummary(calculation.Outputs(), new[] {
                calculation.Sum(0, 0, 0), calculation.Sum(1, 0, 0), calculation.Sum(2, 0, 0) });
            _summaryReady = true;
            Completed?.Invoke();
        }

        private IEnumerator CalculateCurrentWindow(ConvolutionCalculation calculation)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                yield return ShowCompactChannel(calculation, channel, false);
            }
            yield return RecordOutput(calculation);
        }

        private IEnumerator ShowCompactChannel(ConvolutionCalculation calculation, int channel, bool showProducts)
        {
            _ui.BeginChannel(channel);
            _ui.Title = $"{"RGB"[channel]} · 출력 {calculation.Row + 1}행 {calculation.Column + 1}열";
            _ui.Explanation = "현재 위치의 아홉 곱을 모두 더해요.";
            _ui.Guide = "R · G · B의 계산은 같은 위치에서 이어져요.";
            _ui.Feedback = "";
            _spatial.ShowSlidingChannel(channel, showProducts);
            yield return new WaitForSecondsRealtime(0.25f);
            if (showProducts) { _spatial.ShowProducts(channel, 9); }
            _spatial.ShowChannelSum(channel, false);
            _ui.CompleteChannel(channel, calculation.Sum(channel));
            yield return new WaitForSecondsRealtime(0.8f);
        }

        private IEnumerator RecordOutput(ConvolutionCalculation calculation)
        {
            _ui.Title = $"출력 {calculation.Row + 1}행 {calculation.Column + 1}열에 기록해요";
            _ui.Explanation = "R 합 + G 합 + B 합 = 출력값 하나";
            _spatial.ShowRgbSum();
            yield return new WaitForSecondsRealtime(0.7f);
            yield return _spatial.RecordWindowOutput();
            yield return new WaitForSecondsRealtime(0.35f);
        }

        private void Update()
        {
            if (_running) { _ui.ShowSkip(_spatial.WaitingForKernel); }
        }

        private void HandleAssist()
        {
            if (_spatial.WaitingForKernel) { _spatial.AlignKernel(); }
        }

        private void HandleContinue()
        {
            if (!_summaryReady) { return; }
            _summaryReady = false;
            StopPractice();
            ContinueRequested?.Invoke();
        }

        public void StopPractice()
        {
            StopAllCoroutines();
            _running = false;
            _summaryReady = false;
            _quiz?.Cancel();
            if (_initialized) { _spatial.ReleaseInteractions(); }
            _ui.gameObject.SetActive(false);
            _spatial.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _ui.SkipRequested -= HandleAssist;
            _ui.AdvanceRequested -= HandleContinue;
            StopPractice();
        }
    }
}
