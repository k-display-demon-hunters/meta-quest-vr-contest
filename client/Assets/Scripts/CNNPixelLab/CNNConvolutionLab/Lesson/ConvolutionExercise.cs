using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ConvolutionExercise : MonoBehaviour
    {
        private SpatialConvolutionController _spatial;
        private readonly Button[] _answers = new Button[3];
        private readonly int[] _options = new int[3];
        private readonly int[] _sums = new int[3];
        private int[,] _inputs;
        private int[,] _weights;
        private Font _font;
        private Text _title, _explanation, _equation, _feedback, _channelSums;
        private Button _next;
        private Button _skip;
        private Text _guide;


        private bool _answered, _advance, _waiting;
        private int _correct;
        private string _hint, _solved;
        private Action<int[]> _finished;

        public void Initialize(Font font)
        {
            _font = font;
            _title = Label(transform, "Step", "", 0, 365, 1180, 45, 30);
            _explanation = Label(transform, "Explanation", "", 0, 310, 1180, 65, 25);
            GameObject spatial = new GameObject("Spatial calculation");
            spatial.transform.SetParent(transform, false);
            _spatial = spatial.AddComponent<SpatialConvolutionController>();
            _spatial.Initialize(font);
            _channelSums = Label(transform, "Channel sums", "", 0, -340, 1180, 42, 26);
            _equation = Label(transform, "Equation", "", 0, -390, 1180, 50, 30);
            _feedback = Label(transform, "Feedback", "", 900, -170, 420, 140, 24);
            for (int i = 0; i < 3; i++)
            {
                int option = i;
                _answers[i] = MakeButton(transform, "Answer " + i, (i - 1) * 320, -445, 270,
                    () => Choose(option));
            }
            _next = MakeButton(transform, "다음", 900, -300, 420, () => _advance = true);
            _skip = MakeButton(transform, "현재 단계 건너뛰기", 900, -370, 420, SkipCurrentStep);
            Image side = Box(transform, "Side explanation", 900, 20, 480, 770, UI.SceneUIStyle.Surface);
            Label(side.transform, "Heading", "계산 원리", 0, 325, 440, 50, 30);
            _guide = Label(side.transform, "Guide", "", 0, 100, 420, 350, 23);
            _feedback.transform.SetAsLastSibling();
            _next.transform.SetAsLastSibling();
            _skip.transform.SetAsLastSibling();
            _guide.alignment = TextAnchor.UpperLeft;
            _spatial.ReleaseInteractions();
            gameObject.SetActive(false);
        }

        public void Begin(int[,] inputs, int[,] weights, Action<int[]> finished)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            _inputs = (int[,])inputs.Clone();
            _weights = (int[,])weights.Clone();
            _finished = finished;
            _spatial.SetData(_inputs, _weights);

            _waiting = false;
            Array.Clear(_sums, 0, _sums.Length);
            _title.text = "선택한 픽셀을 공간으로 꺼내는 중";
            _explanation.text = "R·G·B 층과 같은 색의 고정 가중치를 관찰하세요.";
            _equation.text = _feedback.text = _guide.text = _channelSums.text = "";
            _spatial.HideAnswers();
            ShowAnswers(false);
            _next.gameObject.SetActive(false);

            StartCoroutine(Run());
        }

        public void Close()
        {
            _spatial.ReleaseInteractions();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_spatial == null || _skip == null) { return; }
            bool physical = _spatial.Volume.HasPendingAction;
            _skip.gameObject.SetActive(physical || _waiting);
            _skip.GetComponentInChildren<Text>().text = physical
                ? "자동 맞추고 다음으로" : "정답 보고 다음으로";
        }

        private void SkipCurrentStep()
        {
            if (_spatial.Volume.CompletePendingAction()) { return; }
            if (!_waiting || _answered) { return; }
            _answered = true;
            _waiting = false;
            _equation.text = _solved;
            _feedback.text = "이 문제는 건너뛰었어요. 계산 결과를 확인하세요.";
            _spatial.ShowAnswer(_correct);
        }

        private IEnumerator Run()
        {
            _title.text = "필터 하나의 입체 구조 맞추기";
            _explanation.text = "필터 하나는 R·G·B에 대응하는 고정 가중치 27개로 이루어져 있어요.";
            _guide.text = "정면에서는 가로·세로 위치를, 옆면에서는 색 성분의 순서를 확인하세요.\n\n같은 칸을 가리키면 RGB의 대응 위치와 입력–가중치 연결이 표시돼요.\n\n세 층 전체를 잡아 입력에 맞추세요.";
            yield return _spatial.PrepareVolume();
            yield return WaitForNext("RGB 대응 관계 확인 완료");
            yield return _spatial.PullColour(0);
            yield return _spatial.Focus(0);
            Highlight(-1);
            _title.text = "R 학습 · 정해진 가중치로 계산하기";
            _explanation.text = "앞의 R 픽셀과 왼쪽 가중치를 관찰하세요. 가중치를 Grip으로 잡아 R 픽셀 앞에 놓으세요.";
            _guide.text = "앞으로 나온 R(빨강)부터 살펴봐요.\n\n뒤의 G·B 층도 같은 필터의 일부예요. 가중치는 고정이에요.\n\n각 픽셀에 같은 위치의 가중치를 곱하고, 9개의 곱을 모두 더해요.\n\n이처럼 입력을 정해진 가중치로 곱해 합치는 계산을 선형결합이라고 해요.";
            _channelSums.text = ChannelSummary(0);
            _equation.text = "R 합 = x₁w₁ + x₂w₂ + … + x₉w₉";
            _feedback.text = "가중치를 R 앞에 놓고 계속을 누르세요. 버튼으로 자동 정렬할 수도 있어요.";
            yield return WaitForNext("자동 맞추고 다음으로");
            _spatial.AlignKernel();
            while (!_spatial.IsAligned)
            {
                _feedback.text = "잡고 있는 가중치를 R 픽셀 앞에 놓거나 Grip을 놓은 후 계속을 누르세요.";
                yield return WaitForNext("가중치 맞추기 / 계속");
                _spatial.AlignKernel();
            }
            _feedback.text = "앞뒤로 놓인 같은 위치의 두 값을 곱해요. 숫자 사본이 이동하며 계산을 보여줘요.";
            yield return WaitForNext("R 계산 시작");
            int[] rowSums = new int[3];
            int accumulated = 0;
            for (int row = 0; row < 3; row++)
            {
                string[] terms = new string[3];
                for (int column = 0; column < 3; column++)
                {
                    int i = row * 3 + column;
                    int product = _inputs[0, i] * _weights[0, i];
                    rowSums[row] += product;
                    yield return _spatial.MultiplyAndAccumulate(0, i, accumulated + rowSums[row]);
                    terms[column] = $"{_inputs[0, i]}×({_weights[0, i]})";
                }
                HighlightRow(row);
                _title.text = $"R 학습 · {row + 1}번째 줄의 곱을 더하기";
                _explanation.text = "같은 위치끼리 곱한 세 결과를 더해요. 줄별 합은 9개 항을 나누어 계산하기 위한 중간값이에요.";
                _equation.text = string.Join(" + ", terms) + $" = {rowSums[row]}";
                int previous = accumulated;
                accumulated += rowSums[row];
                _channelSums.text = $"R 누적 합: {previous} + ({rowSums[row]}) = {accumulated}";
                _feedback.text = "음수인 곱도 부호를 유지해 더해요. 픽셀 값만 더하지 않아요.";
                yield return WaitForNext(row < 2 ? "다음 줄 보기" : "R 전체 합 보기");
            }
            Highlight(-1);
            _sums[0] = Sum(0);
            _title.text = "R 학습 · 9개 곱을 합쳐 R 계산 완료";
            _equation.text = $"({rowSums[0]}) + ({rowSums[1]}) + ({rowSums[2]}) = {_sums[0]}";
            _channelSums.text = ChannelSummary(1);
            _explanation.text = "세 줄의 합을 더한 값은 9개 곱을 한꺼번에 더한 값과 같아요.";
            _feedback.text = "이제 같은 방법으로 G와 B의 계산값을 직접 구해 보세요.";
            yield return WaitForNext("G 퀴즈로");

            yield return _spatial.PullColour(1);
            yield return _spatial.Focus(1);
            _spatial.AlignKernel();
            HighlightRow(0);
            _title.text = "G 퀴즈 · 첫 줄의 가중합을 구하세요";
            _explanation.text = "강조된 첫 줄의 픽셀 3개와 고정 가중치를 같은 위치끼리 곱해 더하면 얼마일까요?";
            _guide.text = "R에서 본 계산을 G(초록)에 적용해요.\n\n가중치는 그대로예요.\n\n먼저 첫 줄의 세 항을 곱해 더하고, 이어서 전체 9개 항의 합을 구해요.\n\n숫자 선택지에서 계산 결과를 고르세요.";
            int firstRow = 0, firstRaw = 0, firstAbsolute = 0;
            for (int i = 0; i < 3; i++)
            {
                int product = _inputs[1, i] * _weights[1, i];
                firstRow += product;
                firstRaw += _inputs[1, i];
                firstAbsolute += Math.Abs(product);
            }
            yield return Ask("G 첫 줄의 (픽셀 × 가중치) 3개 합", firstRow,
                "같은 위치의 두 숫자를 곱한 다음 세 결과를 더하세요. 음수 부호도 유지하세요.", firstRaw, firstAbsolute);
            _spatial.ShowProducts(1, 3);
            yield return WaitForNext("G 전체 합 구하기");
            Highlight(-1);
            _title.text = "G 퀴즈 · 9개 곱의 전체 합은?";
            _explanation.text = $"첫 줄의 합은 {firstRow}예요. 나머지 두 줄도 계산해 G 전체 합을 완성하세요.";
            yield return Ask("G의 9개 (픽셀 × 가중치)의 합", Sum(1),
                $"첫 줄의 합 {firstRow}에 둘째 줄과 셋째 줄의 가중합을 더하세요.", firstRow, -Sum(1));
            _sums[1] = Sum(1);
            DisplayChannel(1, true);
            _channelSums.text = ChannelSummary(2);
            yield return WaitForNext("B 퀴즈로");

            yield return _spatial.PullColour(2);
            yield return _spatial.Focus(2);
            _spatial.AlignKernel();
            Highlight(-1);
            _title.text = "B 퀴즈 · 9개 항의 선형결합을 완성하세요";
            _explanation.text = "같은 위치의 픽셀과 가중치를 곱한 뒤, 9개 결과를 모두 더하면 B 합은 얼마일까요?";
            _guide.text = "이번에는 B 색 성분 전체를 계산해 보세요.\n\n각 위치의 입력과 가중치를 짝지어 곱하세요.\n\nB 합 = x₁w₁ + x₂w₂ + … + x₉w₉\n\n픽셀 값만 더하거나 가중치만 더하는 것이 아니에요.\n\n음수 항도 부호를 유지해서 더하세요.";
            int rawSum = 0;
            int absoluteSum = 0;
            for (int i = 0; i < 9; i++)
            {
                rawSum += _inputs[2, i];
                absoluteSum += Math.Abs(_inputs[2, i] * _weights[2, i]);
            }
            yield return Ask("B의 9개 (픽셀 × 가중치)의 합", Sum(2),
                "왼쪽 열·가운데 열·오른쪽 열의 곱을 각각 더해 보세요. 음수 항의 부호를 유지하세요.",
                rawSum, absoluteSum);
            _sums[2] = Sum(2);
            DisplayChannel(2, true);
            _channelSums.text = ChannelSummary(3);
            yield return WaitForNext("RGB 결과 합치기");

            _title.text = "마지막 퀴즈 · 세 색 성분의 계산을 합치세요";
            _explanation.text = "R·G·B 합은 아래에 있어요. 세 합과 편향 0을 더한 최종 결과를 고르세요.";
            _guide.text = "R·G·B 가중치 세 묶음이 합쳐져 필터 하나를 이뤄요.\n\n필터 하나는 27개 입력의 가중합을 구하고, 마지막에 편향을 한 번 더해요.\n\n출력 = R 합 + G 합 + B 합 + 편향\n\n이번 편향은 0이에요. 결과는 음수나 255보다 큰 값도 가능해요.";
            int total = _sums[0] + _sums[1] + _sums[2];
            yield return Ask($"({_sums[0]}) + ({_sums[1]}) + ({_sums[2]}) + 0", total,
                "세 색 성분의 합을 모두 더하세요. 평균을 내거나 한 색 성분만 고르는 것이 아니에요.",
                total / 3, _sums[0] + _sums[1]);
            yield return _spatial.CombineSums(_sums);
            _title.text = "이번 위치의 합성곱 계산 완료";
            _explanation.text = "입력마다 가중치를 곱해 합치고, 세 색 성분의 합과 편향을 더했어요.";
            _feedback.text = "정해진 가중치로 각 입력을 곱하고 모두 더해 이번 위치의 계산을 완성했어요.";
            yield return WaitForNext("결과 정리 보기");
            Action<int[]> completed = _finished;
            int[] sums = (int[])_sums.Clone();

            _spatial.ReleaseInteractions();
            gameObject.SetActive(false);
            completed?.Invoke(sums);
        }

        private int Sum(int channel)
        {
            int sum = 0;
            for (int i = 0; i < 9; i++) { sum += _inputs[channel, i] * _weights[channel, i]; }
            return sum;
        }

        private void DisplayChannel(int channel, bool showProducts)
        {
            if (showProducts) { _spatial.ShowProducts(channel, 9); }
        }

        private IEnumerator Ask(string expression, int answer, string hint, int distractor1, int distractor2)
        {
            _correct = answer;
            _hint = hint;
            _solved = expression + " = " + answer;
            _equation.text = expression + " = [ ? ]";
            _equation.fontSize = expression.Length > 48 ? 23 : 30;
            _feedback.text = "숫자 블록을 Grip으로 잡아 ‘여기에 답 놓기’에 놓으세요. 아래 숫자 버튼으로도 답할 수 있어요.";
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
                _answers[i].GetComponentInChildren<Text>().text = choices[i].ToString();
                _answers[i].interactable = true;
            }
            _answered = false;
            _waiting = true;
            ShowAnswers(true);
            _spatial.ShowAnswers(_options, Choose);
            yield return new WaitUntil(() => _answered);
            _waiting = false;
            _spatial.HideAnswers();
            ShowAnswers(false);
        }

        private void Choose(int option)
        {
            if (!_waiting || _answered) { return; }
            if (_options[option] != _correct)
            {
                _feedback.text = "다시 골라 보세요. " + _hint;
                return;
            }
            _answered = true;
            _waiting = false;
            _equation.text = _solved;
            _feedback.text = "맞아요! 빈칸을 채웠어요.";
            _spatial.ShowAnswer(_correct);
            foreach (Button button in _answers) { button.interactable = false; }
        }

        private IEnumerator WaitForNext(string label)
        {
            _advance = false;
            _next.interactable = true;
            _next.GetComponentInChildren<Text>().text = label;
            _next.gameObject.SetActive(true);
            yield return new WaitUntil(() => _advance);
            _next.gameObject.SetActive(false);
        }

        private string ChannelSummary(int completed)
        {
            return "R 합: " + (completed > 0 ? _sums[0].ToString() : "?") +
                "     G 합: " + (completed > 1 ? _sums[1].ToString() : "?") +
                "     B 합: " + (completed > 2 ? _sums[2].ToString() : "?");
        }

        private void HighlightRow(int row) { _spatial.HighlightRow(row); }
        private void Highlight(int index) { _spatial.HighlightRow(-1); }
        private void ShowAnswers(bool visible) { foreach (Button answer in _answers) { answer.gameObject.SetActive(visible); } }
        private void OnDisable() { StopAllCoroutines(); _waiting = false; _finished = null; }

        private RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        private Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            Image image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        private Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            Text text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = _font; text.fontSize = size; text.text = value;
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
            return text;
        }
        private Button MakeButton(Transform parent, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            Image image = Box(parent, label, x, y, width, 58, new Color(0.65f, 0.67f, 0.7f));
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.onClick.AddListener(action);
            Label(image.transform, "Label", label, 0, 0, width, 58, 26).color = Color.black;
            UI.SceneUIStyle.Apply(button);
            return button;
        }
    }
}
