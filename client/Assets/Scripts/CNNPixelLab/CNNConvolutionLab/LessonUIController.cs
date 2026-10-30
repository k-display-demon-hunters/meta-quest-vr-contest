using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Displays lesson state and reports user intent to the lesson module.
    public sealed class LessonUIController : MonoBehaviour
    {
        private readonly Button[] _answers = new Button[3];
        private Text _title, _explanation, _equation, _feedback, _channelSums, _guide;
        private Button _next, _skip;
        private bool _advance;
        public event Action<int> AnswerRequested;
        public event Action SkipRequested;
        public string Title { set => _title.text = value; }
        public string Explanation { set => _explanation.text = value; }
        public string Equation { set => _equation.text = value; }
        public string Feedback { set => _feedback.text = value; }
        public string ChannelSums { set => _channelSums.text = value; }
        public string Guide { set => _guide.text = value; }
        public void SetEquationSize(int size) { _equation.fontSize = size; }
        public void SetChoices(int[] choices)
        {
            for (int i = 0; i < 3; i++)
            {
                _answers[i].GetComponentInChildren<Text>().text = choices[i].ToString();
                _answers[i].interactable = true;
            }
        }
        public void SetAnswersEnabled(bool enabled)
        {
            foreach (Button answer in _answers) { answer.interactable = enabled; }
        }
        public void ShowAnswers(bool visible)
        {
            foreach (Button answer in _answers) { answer.gameObject.SetActive(visible); }
        }
        public void ShowSkip(bool physical, bool waiting)
        {
            _skip.gameObject.SetActive(physical || waiting);
            _skip.GetComponentInChildren<Text>().text = physical ? "자동 맞추고 다음으로" : "정답 보고 다음으로";
        }
        private void RequestSkip() { SkipRequested?.Invoke(); }
        public void BeginCalculation()
        {
            ShowCalculation();
            ExerciseRoot.gameObject.SetActive(true);
            _equation.text = _feedback.text = _guide.text = _channelSums.text = "";
            ShowAnswers(false);
            _next.gameObject.SetActive(false);
        }

        private Font _font;
        private GameObject _observationUi;
        private Text _summary;
        private Button _combine;
        private Text _combineLabel;
        private Button _restart;
        private Button _nextObservation;
        private Text _nextObservationLabel;

        public event Action CombineRequested;
        public event Action RestartRequested;
        public event Action AdvanceRequested;
        public event Action CloseRequested;

        private Transform ExerciseRoot { get; set; }

        public void Initialize(Font font)
        {
            _font = font;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
            ((RectTransform)transform).sizeDelta = new Vector2(1300, 1400);
            transform.localScale = Vector3.one * 0.002f;
            _observationUi = Rect(transform, "RGB observation controls", 0, 0, 1300, 950).gameObject;
            Label(_observationUi.transform, "Title", "같은 픽셀을 R · G · B로 나누어 관찰해요", 0, 340, 1250, 60, 29);
            Label(_observationUi.transform, "RGB meaning", "R 빨강 · G 초록 · B 파랑 | 0: 없음 → 255: 가장 강함", 0, 285, 1200, 45, 25);
            _summary = Label(_observationUi.transform, "Explanation", "", 850, 30, 440, 390, 26);
            _combine = Button(_observationUi.transform, "RGB 합쳐 보기", 0, -330, 350,
                () => CombineRequested?.Invoke());
            _combineLabel = _combine.GetComponentInChildren<Text>();
            _restart = Button(_observationUi.transform, "계산 다시 보기", 0, -395, 350,
                () => RestartRequested?.Invoke());
            _nextObservation = Button(_observationUi.transform, "다음으로", 850, -260, 440,
                () => AdvanceRequested?.Invoke());
            _nextObservationLabel = _nextObservation.GetComponentInChildren<Text>();
            ExerciseRoot = Rect(transform, "Convolution exercise", 0, 0, 1300, 1400);
            _title = Label(ExerciseRoot, "Step", "", 0, 365, 1180, 45, 30);
            _explanation = Label(ExerciseRoot, "Explanation", "", 0, 310, 1180, 65, 25);
            _channelSums = Label(ExerciseRoot, "Channel sums", "", 0, -340, 1180, 42, 26);
            _equation = Label(ExerciseRoot, "Equation", "", 0, -390, 1180, 50, 30);
            _feedback = Label(ExerciseRoot, "Feedback", "", 900, -170, 420, 140, 24);
            for (int i = 0; i < 3; i++)
            {
                int option = i;
                _answers[i] = MakeButton(ExerciseRoot, "Answer " + i, (i - 1) * 320, -445, 270,
                    () => AnswerRequested?.Invoke(option));
            }
            _next = MakeButton(ExerciseRoot, "다음", 900, -300, 420, () => _advance = true);
            _skip = MakeButton(ExerciseRoot, "현재 단계 건너뛰기", 900, -370, 420, RequestSkip);
            Image side = Box(ExerciseRoot, "Side explanation", 900, 20, 480, 770, UI.SceneUIStyle.Surface);
            Label(side.transform, "Heading", "계산 원리", 0, 325, 440, 50, 30);
            _guide = Label(side.transform, "Guide", "", 0, 100, 420, 350, 23);
            _feedback.transform.SetAsLastSibling();
            _next.transform.SetAsLastSibling();
            _skip.transform.SetAsLastSibling();
            _guide.alignment = TextAnchor.UpperLeft;

            ExerciseRoot.gameObject.SetActive(false);
            Button(transform, "다른 영역 선택", -850, -340, 380,
                () => CloseRequested?.Invoke());
        }

        public void ShowObservation()
        {
            _observationUi.SetActive(true);
            _combine.gameObject.SetActive(true);
            _combineLabel.text = "RGB 합쳐 보기";
            _restart.gameObject.SetActive(false);
            _summary.text = "앞뒤로 떨어진 세 층의 같은 칸은 같은 픽셀의 색 성분이에요.\n옆에서 깊이를 살펴보세요. RGB를 합치면 원래 픽셀 색이 돼요.\n관찰 후 이미지로 돌아가 ‘다음으로’를 누르세요.";
        }

        public void ShowCalculation()
        {
            _observationUi.SetActive(false);
        }

        public void ShowCompleted(int[] sums)
        {
            ExerciseRoot.gameObject.SetActive(false);
            _observationUi.SetActive(true);
            _restart.gameObject.SetActive(true);
            _combine.gameObject.SetActive(false);
            _summary.text = $"R 합 ({sums[0]}) + G 합 ({sums[1]}) + B 합 ({sums[2]}) + 편향 0\n= 이번 위치의 계산 결과 {sums[0] + sums[1] + sums[2]}\n세 색의 가중치 묶음이 필터 하나예요. 계산 결과는 색이 아니라 숫자 하나예요.";
        }

        public void SetObservationBusy(bool busy)
        {
            _combine.interactable = !busy;
        }

        public void ShowObservationProgress(bool pending, bool busy)
        {
            _nextObservation.interactable = pending || !busy;
            _nextObservationLabel.text = pending ? "자동 꺼내고 다음으로" : "다음으로";
        }

        public void ShowCombined(bool combined)
        {
            _combineLabel.text = combined ? "RGB 다시 펼치기" : "RGB 합쳐 보기";
        }

        public IEnumerator WaitForNext(string label)
        {
            _advance = false;
            _next.interactable = true;
            _next.GetComponentInChildren<Text>().text = label;
            _next.gameObject.SetActive(true);
            yield return new WaitUntil(() => _advance);
            _next.gameObject.SetActive(false);
        }

        private Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            Image image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
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

        private static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size)
        {
            Text text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private Button Button(Transform parent, string label, float x, float y, float width,
            UnityEngine.Events.UnityAction action)
        {
            Image image = Rect(parent, label, x, y, width, 54).gameObject.AddComponent<Image>();
            image.color = new Color(0.65f, 0.67f, 0.7f);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Label(image.transform, "Label", label, 0, 0, width, 54, 25).color = Color.black;
            SceneUIStyle.Apply(button);
            return button;
        }
    }
}
