using System;
using System.Collections;
using UnityEngine;

namespace UI
{
    internal sealed class RedCalculationController
    {
        private readonly RgbVolumeController _spatial;
        private readonly LessonUIController _ui;
        private readonly ConvolutionCalculation _calculation;
        private readonly CalculationAnimationController _animation;
        public RedCalculationController(RgbVolumeController spatial, LessonUIController ui, ConvolutionCalculation calculation)
        {
            _spatial = spatial;
            _ui = ui;
            _calculation = calculation;
            _animation = new CalculationAnimationController(spatial, calculation);
        }
        public IEnumerator Run()
        {
            yield return _spatial.PullColour(0);
            yield return _spatial.Focus(0);
            _spatial.HighlightRow(-1);
            _ui.Title = "R 학습 · 정해진 가중치로 계산하기";
            _ui.Explanation = "앞의 R 픽셀과 왼쪽 가중치를 관찰하세요. 가중치를 Grip으로 잡아 R 픽셀 앞에 놓으세요.";
            _ui.Guide = "앞으로 나온 R(빨강)부터 살펴봐요.\n\n뒤의 G·B 층도 같은 필터의 일부예요. 가중치는 고정이에요.\n\n각 픽셀에 같은 위치의 가중치를 곱하고, 9개의 곱을 모두 더해요.\n\n이처럼 입력을 정해진 가중치로 곱해 합치는 계산을 선형결합이라고 해요.";
            _ui.ChannelSums = _calculation.Summary(0);
            _ui.Equation = "R 합 = x₁w₁ + x₂w₂ + … + x₉w₉";
            _ui.Feedback = "가중치를 R 앞에 놓고 계속을 누르세요. 버튼으로 자동 정렬할 수도 있어요.";
            yield return _ui.WaitForNext("자동 맞추고 다음으로");
            _spatial.AlignKernel();
            while (!_spatial.IsAligned)
            {
                _ui.Feedback = "잡고 있는 가중치를 R 픽셀 앞에 놓거나 Grip을 놓은 후 계속을 누르세요.";
                yield return _ui.WaitForNext("가중치 맞추기 / 계속");
                _spatial.AlignKernel();
            }
            _ui.Feedback = "앞뒤로 놓인 같은 위치의 두 값을 곱해요. 숫자 사본이 이동하며 계산을 보여줘요.";
            yield return _ui.WaitForNext("R 계산 시작");
            int[] rowSums = new int[3];
            int accumulated = 0;
            for (int row = 0; row < 3; row++)
            {
                string[] terms = new string[3];
                for (int column = 0; column < 3; column++)
                {
                    int i = row * 3 + column;
                    int product = _calculation.Product(0, i);
                    rowSums[row] += product;
                    yield return _animation.MultiplyAndAccumulate(0, i, accumulated + rowSums[row]);
                    terms[column] = $"{_calculation.Input(0, i)}×({_calculation.Weight(0, i)})";
                }
                _spatial.HighlightRow(row);
                _ui.Title = $"R 학습 · {row + 1}번째 줄의 곱을 더하기";
                _ui.Explanation = "같은 위치끼리 곱한 세 결과를 더해요. 줄별 합은 9개 항을 나누어 계산하기 위한 중간값이에요.";
                _ui.Equation = string.Join(" + ", terms) + $" = {rowSums[row]}";
                int previous = accumulated;
                accumulated += rowSums[row];
                _ui.ChannelSums = $"R 누적 합: {previous} + ({rowSums[row]}) = {accumulated}";
                _ui.Feedback = "음수인 곱도 부호를 유지해 더해요. 픽셀 값만 더하지 않아요.";
                yield return _ui.WaitForNext(row < 2 ? "다음 줄 보기" : "R 전체 합 보기");
            }
            _spatial.HighlightRow(-1);
            _ui.Title = "R 학습 · 9개 곱을 합쳐 R 계산 완료";
            _ui.Equation = $"({rowSums[0]}) + ({rowSums[1]}) + ({rowSums[2]}) = {_calculation.Sum(0)}";
            _ui.ChannelSums = _calculation.Summary(1);
            _ui.Explanation = "세 줄의 합을 더한 값은 9개 곱을 한꺼번에 더한 값과 같아요.";
            _ui.Feedback = "이제 같은 방법으로 G와 B의 계산값을 직접 구해 보세요.";
            yield return _ui.WaitForNext("G 퀴즈로");

        }
    }
}
