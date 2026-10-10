using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ConvolutionExercise : MonoBehaviour
    {
        private readonly Text[,] _labels = new Text[3, 9];
        private readonly Image[,] _cells = new Image[3, 9];
        private readonly Button[] _answers = new Button[3];
        private readonly int[] _options = new int[3];
        private readonly int[] _sums = new int[3];
        private int[,] _inputs;
        private int[,] _weights;
        private Font _font;
        private Text _title, _explanation, _equation, _feedback, _channelSums;
        private Button _next;
        private Text _guide;


        private bool _answered, _advance, _waiting;
        private int _correct;
        private string _hint, _solved;
        private Action<int[]> _finished;

        public void Initialize(Font font)
        {
            _font = font;
            Image background = Box(transform, "Exercise background", 0, 0, 1260, 840, new Color(0.08f, 0.09f, 0.11f));
            background.raycastTarget = true;
            _title = Label(transform, "Step", "", 0, 365, 1180, 45, 30);
            _explanation = Label(transform, "Explanation", "", 0, 305, 1180, 65, 25);
            string[] headings = { "픽셀 값", "곱할 수 (가중치)", "곱한 결과" };
            for (int g = 0; g < 3; g++)
            {
                float center = (g - 1) * 395;
                Label(transform, headings[g], headings[g], center, 245, 370, 38, 27);
                for (int i = 0; i < 9; i++)
                {
                    _cells[g, i] = Box(transform, "Cell " + g + "/" + i,
                        center + (i % 3 - 1) * 105, 190 - (i / 3) * 70, 98, 64, CellColor);
                    _labels[g, i] = Label(_cells[g, i].transform, "Value", "", 0, 0, 98, 64, 28);
                }
            }
            _channelSums = Label(transform, "Channel sums", "", 0, -10, 1180, 42, 26);
            _equation = Label(transform, "Equation", "", 0, -65, 1180, 50, 30);
            _feedback = Label(transform, "Feedback", "", 0, -130, 1180, 75, 25);
            for (int i = 0; i < 3; i++)
            {
                int option = i;
                _answers[i] = MakeButton(transform, "Answer " + i, (i - 1) * 320, -210, 270,
                    () => Choose(option));
            }
            _next = MakeButton(transform, "다음", 0, -285, 330, () => _advance = true);
            Image side = Box(transform, "Side explanation", 900, 20, 480, 770, new Color(0.1f, 0.12f, 0.15f));
            Label(side.transform, "Heading", "계산 원리", 0, 325, 440, 50, 30);
            _guide = Label(side.transform, "Guide", "", 0, -10, 420, 590, 27);
            _guide.alignment = TextAnchor.UpperLeft;
            gameObject.SetActive(false);
        }

        public void Begin(int[,] inputs, int[,] weights, Action<int[]> finished)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            _inputs = (int[,])inputs.Clone();
            _weights = (int[,])weights.Clone();
            _finished = finished;

            _waiting = false;
            Array.Clear(_sums, 0, _sums.Length);
            ShowAnswers(false);
            _next.gameObject.SetActive(false);

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            DisplayChannel(0, false);
            Highlight(-1);
            _title.text = "R 학습 · 정해진 가중치로 계산하기";
            _explanation.text = "왼쪽은 픽셀 값, 가운데는 고정된 가중치예요. 같은 위치끼리 곱한 결과를 오른쪽에 채워요.";
            _guide.text = "먼저 R(빨강) 채널만 살펴봐요.\n\n가중치는 이미 정해져 있어요.\n\n각 픽셀에 같은 위치의 가중치를 곱하고, 9개의 곱을 모두 더해요.\n\n이처럼 입력을 정해진 가중치로 곱해 합치는 계산을 선형결합이라고 해요.";
            _channelSums.text = ChannelSummary(0);
            _equation.text = "R 합 = x₁w₁ + x₂w₂ + … + x₉w₉";
            _feedback.text = "‘R 계산 시작’을 누르면 한 줄씩 계산 과정을 보여줘요.";
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
                    _labels[2, i].text = product.ToString();
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

            DisplayChannel(1, false);
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
            for (int i = 0; i < 3; i++) { _labels[2, i].text = (_inputs[1, i] * _weights[1, i]).ToString(); }
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

            DisplayChannel(2, false);
            Highlight(-1);
            _title.text = "B 퀴즈 · 9개 항의 선형결합을 완성하세요";
            _explanation.text = "같은 위치의 픽셀과 가중치를 곱한 뒤, 9개 결과를 모두 더하면 B 합은 얼마일까요?";
            _guide.text = "이번에는 B 채널 전체를 계산해 보세요.\n\n각 위치의 입력과 가중치를 짝지어 곱하세요.\n\nB 합 = x₁w₁ + x₂w₂ + … + x₉w₉\n\n픽셀 값만 더하거나 가중치만 더하는 것이 아니에요.\n\n음수 항도 부호를 유지해서 더하세요.";
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

            _title.text = "마지막 퀴즈 · 세 채널의 계산을 합치세요";
            _explanation.text = "R·G·B 합은 아래에 있어요. 세 합과 편향 0을 더한 최종 결과를 고르세요.";
            _guide.text = "R·G·B 가중치 세 묶음이 합쳐져 필터 하나를 이뤄요.\n\n필터 하나는 27개 입력의 가중합을 구하고, 마지막에 편향을 한 번 더해요.\n\n출력 = R 합 + G 합 + B 합 + 편향\n\n이번 편향은 0이에요. 결과는 음수나 255보다 큰 값도 가능해요.";
            int total = _sums[0] + _sums[1] + _sums[2];
            yield return Ask($"({_sums[0]}) + ({_sums[1]}) + ({_sums[2]}) + 0", total,
                "세 채널의 합을 모두 더하세요. 평균을 내거나 한 채널만 고르는 것이 아니에요.",
                total / 3, _sums[0] + _sums[1]);
            _title.text = "이번 위치의 합성곱 계산 완료";
            _explanation.text = "입력마다 가중치를 곱해 합치고, 세 채널의 합과 편향을 더했어요.";
            _feedback.text = "정해진 가중치로 각 입력을 곱하고 모두 더해 이번 위치의 계산을 완성했어요.";
            yield return WaitForNext("RGB 판으로 돌아가기");
            Action<int[]> completed = _finished;
            int[] sums = (int[])_sums.Clone();

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
            for (int i = 0; i < 9; i++)
            {
                _labels[0, i].text = _inputs[channel, i].ToString();
                _labels[1, i].text = _weights[channel, i].ToString();
                _labels[2, i].text = showProducts ? (_inputs[channel, i] * _weights[channel, i]).ToString() : "?";
            }
        }

        private IEnumerator Ask(string expression, int answer, string hint, int distractor1, int distractor2)
        {
            _correct = answer;
            _hint = hint;
            _solved = expression + " = " + answer;
            _equation.text = expression + " = [ ? ]";
            _equation.fontSize = expression.Length > 48 ? 23 : 30;
            _feedback.text = "빈칸에 들어갈 숫자를 고르세요 · 컨트롤러 트리거 또는 마우스 클릭";
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
            yield return new WaitUntil(() => _answered);
            _waiting = false;
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

        private static Color CellColor => new Color(0.2f, 0.22f, 0.25f);
        private void HighlightRow(int row)
        {
            for (int g = 0; g < 3; g++)
                for (int i = 0; i < 9; i++)
                {
                    bool active = i / 3 == row;
                    _cells[g, i].color = active ? new Color(0.85f, 0.72f, 0.32f) : CellColor;
                    _labels[g, i].color = active ? Color.black : Color.white;
                }
        }
        private void Highlight(int index)
        {
            for (int g = 0; g < 3; g++)
                for (int i = 0; i < 9; i++)
                {
                    _cells[g, i].color = i == index ? new Color(0.85f, 0.72f, 0.32f) : CellColor;
                    _labels[g, i].color = i == index ? Color.black : Color.white;
                }
        }
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
            return button;
        }
    }
}
