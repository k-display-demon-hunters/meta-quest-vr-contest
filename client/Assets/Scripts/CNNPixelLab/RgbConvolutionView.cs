using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // RGB weight slices belong to a single 3 x 3 x 3 filter.
    public sealed class RgbConvolutionView : MonoBehaviour
    {
        private readonly int[,] _weights = {
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 } };
        private readonly int[,] _inputs = new int[3, 9];
        private SpatialPixelLab _lab;
        private Font _font;
        private SpatialConvolutionController _observation;
        private ConvolutionExercise _exercise;
        private GameObject _observationUi;
        private Text _summary;
        private Button _combine;
        private Button _restart;
        private readonly Button[] _moves = new Button[4];
        private Vector2Int _selected;
        private int _columns;
        private int _rows;
        private bool _combined;
        private bool _busy;

        public void Initialize(SpatialPixelLab lab, Font font)
        {
            _lab = lab;
            _font = font;
            foreach (Transform child in transform) { child.gameObject.SetActive(false); }
            ((RectTransform)transform).sizeDelta = new Vector2(1300, 1400);
            transform.localScale = Vector3.one * 0.002f;
            _observationUi = Rect(transform, "RGB observation controls", 0, 0, 1300, 950).gameObject;
            Label(_observationUi.transform, "Title", "같은 픽셀을 R · G · B로 나누어 관찰해요", 0, 340, 1250, 60, 29);
            Label(_observationUi.transform, "RGB meaning", "R 빨강 · G 초록 · B 파랑 | 0: 없음 → 255: 가장 강함", 0, 285, 1200, 45, 25);
            _summary = Label(_observationUi.transform, "Explanation",
                "", 850, 30, 440, 390, 26);
            _combine = Button(_observationUi.transform, "RGB 합쳐 보기", 0, -330, 350, ToggleCombined);
            _restart = Button(_observationUi.transform, "계산 다시 보기", 0, -395, 350, BeginCalculation);
            _moves[0] = Button(_observationUi.transform, "←", -480, -395, 90, () => _lab.MoveSelection(-1, 0));
            _moves[1] = Button(_observationUi.transform, "→", -370, -395, 90, () => _lab.MoveSelection(1, 0));
            _moves[2] = Button(_observationUi.transform, "↑", 370, -395, 90, () => _lab.MoveSelection(0, 1));
            _moves[3] = Button(_observationUi.transform, "↓", 480, -395, 90, () => _lab.MoveSelection(0, -1));
            GameObject observation = new GameObject("RGB layers in space");
            observation.transform.SetParent(_observationUi.transform, false);
            _observation = observation.AddComponent<SpatialConvolutionController>();
            _observation.Initialize(font);
            RectTransform exercise = Rect(transform, "Convolution exercise", 0, 0, 1300, 1400);
            _exercise = exercise.gameObject.AddComponent<ConvolutionExercise>();
            _exercise.Initialize(font);
            Button(transform, "이미지로 돌아가기", -850, -340, 380, _lab.CloseDetails);
        }

        public void Show(Color32[] pixels, int columns, int rows, Vector2Int selected, bool calculationAvailable)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            foreach (SpatialConvolutionController spatial in GetComponentsInChildren<SpatialConvolutionController>(true)) { spatial.ReleaseInteractions(); }
            _exercise.gameObject.SetActive(false);
            _busy = false;
            _combined = false;
            _selected = selected;
            _columns = columns;
            _rows = rows;
            foreach (Button move in _moves) { move.gameObject.SetActive(false); }
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    Color32 pixel = pixels[(selected.y + 2 - i / 3) * columns + selected.x + i % 3];
                    _inputs[c, i] = c == 0 ? pixel.r : c == 1 ? pixel.g : pixel.b;
                }
            }
            _observationUi.SetActive(true);
            _observation.SetData(_inputs, _weights);
            _combine.gameObject.SetActive(true);
            _combine.GetComponentInChildren<Text>().text = "RGB 합쳐 보기";
            _restart.gameObject.SetActive(false);
            _summary.text = "앞뒤로 떨어진 세 층의 같은 칸은 같은 픽셀의 색 성분이에요.\n옆에서 깊이를 살펴보세요. RGB를 합치면 원래 픽셀 색이 돼요.\n관찰 후 이미지로 돌아가 ‘다음으로’를 누르세요.";
            if (calculationAvailable) { BeginCalculation(); }
            else { StartCoroutine(ShowObservation()); }
        }

        private IEnumerator ShowObservation(bool extract = true)
        {
            _busy = true;
            _combine.interactable = false;
            yield return _observation.Observe(_lab.SelectionWorldPosition, extract);
            _lab.SetBackgroundHidden(true);
            _combine.interactable = true;
            _busy = false;
        }

        private void BeginCalculation()
        {
            if (_busy) { return; }
            _observation.ReleaseInteractions();
            _observationUi.SetActive(false);
            _exercise.SelectionSource = _lab.SelectionWorldPosition;
            _exercise.Begin(_inputs, _weights, CompleteExercise);
        }

        private void ToggleCombined()
        {
            if (!_busy) { StartCoroutine(Combine()); }
        }

        private IEnumerator Combine()
        {
            _busy = true;
            _combine.interactable = false;
            _combined = !_combined;
            yield return _observation.SpreadColours(_combined);
            _combine.GetComponentInChildren<Text>().text = _combined ? "RGB 다시 펼치기" : "RGB 합쳐 보기";
            _combine.interactable = true;
            _busy = false;
        }

        private void CompleteExercise(int[] sums)
        {
            _observationUi.SetActive(true);
            _restart.gameObject.SetActive(true);
            _combine.gameObject.SetActive(false);
            foreach (Button move in _moves) { move.gameObject.SetActive(true); }
            _moves[0].interactable = _selected.x > 0;
            _moves[1].interactable = _selected.x < _columns - 3;
            _moves[2].interactable = _selected.y < _rows - 3;
            _moves[3].interactable = _selected.y > 0;
            _summary.text = $"R 합 ({sums[0]}) + G 합 ({sums[1]}) + B 합 ({sums[2]}) + 편향 0\n= 이번 위치의 계산 결과 {sums[0] + sums[1] + sums[2]}\n세 색의 가중치 묶음이 필터 하나예요. 계산 결과는 색이 아니라 숫자 하나예요.";
            StartCoroutine(ShowObservation(false));
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _busy = false;
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

        private static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            Image image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
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

        private Button Button(Transform parent, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
        {
            Image image = Box(parent, label, x, y, width, 54, new Color(0.65f, 0.67f, 0.7f));
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Label(image.transform, "Label", label, 0, 0, width, 54, 25).color = Color.black;
            return button;
        }
    }
}
