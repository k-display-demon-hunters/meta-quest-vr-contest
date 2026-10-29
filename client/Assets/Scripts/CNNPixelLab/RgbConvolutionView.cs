using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Displays lesson state and reports user intent to the lesson module.
    public sealed class RgbConvolutionView : MonoBehaviour
    {
        private Font _font;
        private GameObject _observationUi;
        private Text _summary;
        private Button _combine;
        private Text _combineLabel;
        private Button _restart;
        private Button _nextObservation;
        private Text _nextObservationLabel;
        private readonly Button[] _moves = new Button[4];

        public event Action CombineRequested;
        public event Action RestartRequested;
        public event Action AdvanceRequested;
        public event Action CloseRequested;
        public event Action<int, int> SelectionMoveRequested;

        public Transform ObservationRoot => _observationUi.transform;
        public Transform ExerciseRoot { get; private set; }

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
            _moves[0] = Button(_observationUi.transform, "←", -480, -395, 90,
                () => SelectionMoveRequested?.Invoke(-1, 0));
            _moves[1] = Button(_observationUi.transform, "→", -370, -395, 90,
                () => SelectionMoveRequested?.Invoke(1, 0));
            _moves[2] = Button(_observationUi.transform, "↑", 370, -395, 90,
                () => SelectionMoveRequested?.Invoke(0, 1));
            _moves[3] = Button(_observationUi.transform, "↓", 480, -395, 90,
                () => SelectionMoveRequested?.Invoke(0, -1));
            ExerciseRoot = Rect(transform, "Convolution exercise", 0, 0, 1300, 1400);
            Button(transform, "이미지로 돌아가기", -850, -340, 380,
                () => CloseRequested?.Invoke());
        }

        public void ShowObservation()
        {
            _observationUi.SetActive(true);
            foreach (Button move in _moves)
            {
                move.gameObject.SetActive(false);
            }
            _combine.gameObject.SetActive(true);
            _combineLabel.text = "RGB 합쳐 보기";
            _restart.gameObject.SetActive(false);
            _summary.text = "앞뒤로 떨어진 세 층의 같은 칸은 같은 픽셀의 색 성분이에요.\n옆에서 깊이를 살펴보세요. RGB를 합치면 원래 픽셀 색이 돼요.\n관찰 후 이미지로 돌아가 ‘다음으로’를 누르세요.";
        }

        public void ShowCalculation()
        {
            _observationUi.SetActive(false);
        }

        public void ShowCompleted(int[] sums, Vector2Int selected, int columns, int rows)
        {
            _observationUi.SetActive(true);
            _restart.gameObject.SetActive(true);
            _combine.gameObject.SetActive(false);
            foreach (Button move in _moves)
            {
                move.gameObject.SetActive(true);
            }
            _moves[0].interactable = selected.x > 0;
            _moves[1].interactable = selected.x < columns - 3;
            _moves[2].interactable = selected.y < rows - 3;
            _moves[3].interactable = selected.y > 0;
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
