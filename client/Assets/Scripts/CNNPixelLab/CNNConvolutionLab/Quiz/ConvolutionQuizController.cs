using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace UI
{
    internal sealed class ConvolutionQuizController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        private readonly int[] _options = new int[3];
        private readonly int[] _sums = new int[3];
        private bool _answered, _waiting;
        private int _correct;
        private string _hint, _solved;
        public bool Waiting => _waiting;
        public ConvolutionQuizController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
            _sums[0] = calculation.Sum(0);
        }
        private int Sum(int channel) => _calculation.Sum(channel);
        public void Cancel() { _waiting = false; _spatial.HideAnswers(); }
        public IEnumerator Run()
        {
            yield return _spatial.PullColour(1);
            yield return _spatial.Focus(1);
            _spatial.AlignKernel();
            _spatial.HighlightRow(0);
            _ui.Title = "G 퀴즈 · 첫 줄의 가중합을 구하세요";
            _ui.Explanation = "강조된 첫 줄의 픽셀 3개와 고정 가중치를 같은 위치끼리 곱해 더하면 얼마일까요?";
            _ui.Guide = "R에서 본 계산을 G(초록)에 적용해요.\n\n가중치는 그대로예요.\n\n먼저 첫 줄의 세 항을 곱해 더하고, 이어서 전체 9개 항의 합을 구해요.\n\n숫자 선택지에서 계산 결과를 고르세요.";
            int firstRow = 0, firstRaw = 0, firstAbsolute = 0;
            for (int i = 0; i < 3; i++)
            {
                int product = _calculation.Product(1, i);
                firstRow += product;
                firstRaw += _calculation.Input(1, i);
                firstAbsolute += Math.Abs(product);
            }
            yield return Ask("G 첫 줄의 (픽셀 × 가중치) 3개 합", firstRow,
                "같은 위치의 두 숫자를 곱한 다음 세 결과를 더하세요. 음수 부호도 유지하세요.", firstRaw, firstAbsolute);
            _spatial.ShowProducts(1, 3);
            yield return _ui.WaitForNext("G 전체 합 구하기");
            _spatial.HighlightRow(-1);
            _ui.Title = "G 퀴즈 · 9개 곱의 전체 합은?";
            _ui.Explanation = $"첫 줄의 합은 {firstRow}예요. 나머지 두 줄도 계산해 G 전체 합을 완성하세요.";
            yield return Ask("G의 9개 (픽셀 × 가중치)의 합", Sum(1),
                $"첫 줄의 합 {firstRow}에 둘째 줄과 셋째 줄의 가중합을 더하세요.", firstRow, -Sum(1));
            _sums[1] = Sum(1);
            _spatial.ShowProducts(1, 9);
            _ui.ChannelSums = _calculation.Summary(2);
            yield return _ui.WaitForNext("B 퀴즈로");

            yield return _spatial.PullColour(2);
            yield return _spatial.Focus(2);
            _spatial.AlignKernel();
            _spatial.HighlightRow(-1);
            _ui.Title = "B 퀴즈 · 9개 항의 선형결합을 완성하세요";
            _ui.Explanation = "같은 위치의 픽셀과 가중치를 곱한 뒤, 9개 결과를 모두 더하면 B 합은 얼마일까요?";
            _ui.Guide = "이번에는 B 색 성분 전체를 계산해 보세요.\n\n각 위치의 입력과 가중치를 짝지어 곱하세요.\n\nB 합 = x₁w₁ + x₂w₂ + … + x₉w₉\n\n픽셀 값만 더하거나 가중치만 더하는 것이 아니에요.\n\n음수 항도 부호를 유지해서 더하세요.";
            int rawSum = 0;
            int absoluteSum = 0;
            for (int i = 0; i < 9; i++)
            {
                rawSum += _calculation.Input(2, i);
                absoluteSum += Math.Abs(_calculation.Product(2, i));
            }
            yield return Ask("B의 9개 (픽셀 × 가중치)의 합", Sum(2),
                "왼쪽 열·가운데 열·오른쪽 열의 곱을 각각 더해 보세요. 음수 항의 부호를 유지하세요.",
                rawSum, absoluteSum);
            _sums[2] = Sum(2);
            _spatial.ShowProducts(2, 9);
            _ui.ChannelSums = _calculation.Summary(3);
            yield return _ui.WaitForNext("RGB 결과 합치기");

            _ui.Title = "마지막 퀴즈 · 세 색 성분의 계산을 합치세요";
            _ui.Explanation = "R·G·B 합은 아래에 있어요. 세 합과 편향 0을 더한 최종 결과를 고르세요.";
            _ui.Guide = "R·G·B 가중치 세 묶음이 합쳐져 필터 하나를 이뤄요.\n\n필터 하나는 27개 입력의 가중합을 구하고, 마지막에 편향을 한 번 더해요.\n\n출력 = R 합 + G 합 + B 합 + 편향\n\n이번 편향은 0이에요. 결과는 음수나 255보다 큰 값도 가능해요.";
            int total = _sums[0] + _sums[1] + _sums[2];
            yield return Ask($"({_sums[0]}) + ({_sums[1]}) + ({_sums[2]}) + 0", total,
                "세 색 성분의 합을 모두 더하세요. 평균을 내거나 한 색 성분만 고르는 것이 아니에요.",
                total / 3, _sums[0] + _sums[1]);
            yield return _spatial.CombineSums(_sums);
            _ui.Title = "이번 위치의 합성곱 계산 완료";
            _ui.Explanation = "입력마다 가중치를 곱해 합치고, 세 색 성분의 합과 편향을 더했어요.";
            _ui.Feedback = "정해진 가중치로 각 입력을 곱하고 모두 더해 이번 위치의 계산을 완성했어요.";
            yield return _ui.WaitForNext("결과 정리 보기");
        }
        private IEnumerator Ask(string expression, int answer, string hint, int distractor1, int distractor2)
        {
            _correct = answer;
            _hint = hint;
            _solved = expression + " = " + answer;
            _ui.Equation = expression + " = [ ? ]";
            _ui.SetEquationSize( expression.Length > 48 ? 23 : 30);
            _ui.Feedback = "숫자 블록을 Grip으로 잡아 ‘여기에 답 놓기’에 놓으세요. 아래 숫자 버튼으로도 답할 수 있어요.";
            List<int> choices = new List<int> { answer };
            if (!choices.Contains(distractor1)) { choices.Add(distractor1); }
            if (choices.Count < 3 && !choices.Contains(distractor2)) { choices.Add(distractor2); }
            for (int offset = 1; choices.Count < 3; offset++)
                if (!choices.Contains(answer + offset)) { choices.Add(answer + offset); }
            for (int i = choices.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int temp = choices[i]; choices[i] = choices[j]; choices[j] = temp;
            }
            for (int i = 0; i < 3; i++)
            {
                _options[i] = choices[i];
            }
            _ui.SetChoices(_options);
            _answered = false;
            _waiting = true;
            _ui.ShowAnswers(true);
            _spatial.ShowAnswers(_options, Choose);
            yield return new WaitUntil(() => _answered);
            _waiting = false;
            _spatial.HideAnswers();
            _ui.ShowAnswers(false);
        }
        public void Choose(int option)
        {
            if (!_waiting || _answered) { return; }
            if (_options[option] != _correct)
            {
                _ui.Feedback = "다시 골라 보세요. " + _hint;
                return;
            }
            _answered = true;
            _waiting = false;
            _ui.Equation = _solved;
            _ui.Feedback = "맞아요! 빈칸을 채웠어요.";
            _spatial.ShowAnswer(_correct);
            _ui.SetAnswersEnabled(false);
        }
        public void SkipCurrentStep()
        {
            if (!_waiting || _answered) { return; }
            _answered = true;
            _waiting = false;
            _ui.Equation = _solved;
            _ui.Feedback = "이 문제는 건너뛰었어요. 계산 결과를 확인하세요.";
            _spatial.ShowAnswer(_correct);
        }
    }
}
