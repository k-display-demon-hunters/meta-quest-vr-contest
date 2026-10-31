using System;
using System.Collections;
using UnityEngine;

namespace UI
{
    // First-room arithmetic: owns R/G sequencing and its calculation display.
    public sealed class PixelPracticeManager : MonoBehaviour
    {
        [SerializeField] private LessonUIController _ui;
        [SerializeField] private RgbVolumeController _spatial;
        [SerializeField] private Font _font;
        private bool _initialized;
        private bool _running;
        public event Action Completed;

        private void OnEnable() { _ui.SkipRequested += HandleAssist; }
        private void OnDisable()
        {
            _ui.SkipRequested -= HandleAssist;
            StopPractice();
        }
        public void BeginPractice(PixelPatchData patch)
        {
            if (!_initialized)
            {
                _ui.Initialize(_font);
                _spatial.Initialize(_font);
                _initialized = true;
            }
            _ui.SetReturnAvailable(false);
            _ui.BeginCalculation();
            _spatial.gameObject.SetActive(true);
            ConvolutionCalculation calculation = new ConvolutionCalculation(patch);
            _spatial.SetData(calculation);
            _running = true;
            StartCoroutine(Run(calculation));
        }
        private IEnumerator Run(ConvolutionCalculation calculation)
        {
            _ui.Title = "작은 숫자로 연습해요";
            _ui.Explanation = "지금부터는 실제 RGB 값을\n0~3의 작은 연습용 값으로 바꿔요.";
            _ui.Guide = "연습용 값 0~3\n\n원래 색의 값은 0~255예요.";
            yield return _spatial.ArrangePractice();
            yield return _ui.WaitForNext("다음");
            _ui.Title = "옆에 나타난 것은 가중치예요";
            _ui.Explanation = "각 칸의 숫자에 곱할 값이에요.\n입력 RGB와는 다른 블록이에요.";
            yield return _spatial.RevealWeights();
            yield return _ui.WaitForNext("다음");
            _ui.Title = "1은 남기고, 0은 빼요";
            _ui.Explanation = "숫자 × 1 = 그대로\n숫자 × 0 = 0";
            _ui.Guide = "같은 위치끼리 곱해요.\n\n곱한 값을 모두 더해요.";
            yield return _ui.WaitForNext("R 시작");
            RedCalculationController lesson = new RedCalculationController(_spatial, _ui, calculation);
            yield return lesson.Run(0);
            yield return lesson.Run(1);
            _ui.Title = "R · G 학습 완료!";
            _ui.Explanation = "다음 공간에서는 B를 직접 계산해요.\n같은 방법으로 할 수 있어요.";
            _ui.Equation = $"R 합 {calculation.Sum(0)}    G 합 {calculation.Sum(1)}";
            _ui.Guide = "다음 공간 입구의\n양피지도 수집하세요.";
            _ui.Feedback = "[다음 공간으로]를 누르세요.";
            yield return _ui.WaitForNext("다음 공간으로");
            _running = false;
            _spatial.ReleaseInteractions();
            _ui.gameObject.SetActive(false);
            Completed?.Invoke();
        }
        private void Update()
        {
            if (_running) { _ui.ShowSkip(_spatial.HasPendingAction || _spatial.WaitingForKernel, false); }
        }
        private void HandleAssist()
        {
            if (!_spatial.CompletePendingAction()) { _spatial.AlignKernel(); }
        }
        public void StopPractice()
        {
            StopAllCoroutines();
            _running = false;
            if (_initialized) { _spatial.ReleaseInteractions(); }
            _ui.gameObject.SetActive(false);
            _spatial.gameObject.SetActive(false);
        }
    }
}
