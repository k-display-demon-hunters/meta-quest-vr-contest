using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // One output filter with three input-channel slices, not three independent filters.
    public sealed class RgbConvolutionView : MonoBehaviour
    {
        private static readonly string[] Channels = { "R", "G", "B" };
        private static readonly Color[] Tints = {
            new Color(0.6f, 0.16f, 0.16f), new Color(0.12f, 0.4f, 0.18f), new Color(0.15f, 0.25f, 0.6f) };
        // Fixed filter shared across selected positions. Bias is zero.
        private readonly int[,] Weights = {
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 },
            { -1, 0, 1, -1, 0, 1, -1, 0, 1 } };
        private readonly int[,] _inputs = new int[3, 9];
        private readonly Text[,] _inputLabels = new Text[3, 9];
        private readonly Text[,] _weightLabels = new Text[3, 9];
        private readonly Image[,] _inputCells = new Image[3, 9];
        private readonly Image[,] _weightCells = new Image[3, 9];
        private readonly Text[] _sums = new Text[3];
        private readonly RectTransform[] _panels = new RectTransform[3];
        private readonly Button[] _moves = new Button[4];
        private SpatialPixelLab _lab;
        private Font _font;
        private Text _operation;
        private Text _output;
        private Text _position;
        private Button _calculate;
        private GameObject _help;
        private bool _calculationAvailable;
        private Button _combine;
        private GameObject _combinedPixels;
        private CanvasGroup _combinedFade;
        private readonly CanvasGroup[] _channelFades = new CanvasGroup[3];
        private readonly Image[] _combinedCells = new Image[9];
        private bool _combined;

        private ConvolutionExercise _exercise;
        private Vector2Int _selected;
        private int _columns;
        private int _rows;
        private Coroutine _animation;

        public void Initialize(SpatialPixelLab lab, Font font)
        {
            _lab = lab;
            _font = font;
            // Retain the existing world-space canvas and its XR raycaster.
            foreach (Transform child in transform) { child.gameObject.SetActive(false); }
            ((RectTransform)transform).sizeDelta = new Vector2(1300, 950);
            transform.localScale = Vector3.one * 0.002f;
            Box(transform, "Background", 0, 0, 1300, 950, new Color(0.08f, 0.09f, 0.11f));
            Label(transform, "Filter", "같은 픽셀의 색을 R·G·B로 나눠 봐요", 0, 430, 1200, 48, 28);
            _position = Label(transform, "Position", "", 0, 385, 1100, 42, 23);
            for (int c = 0; c < 3; c++)
            {
                RectTransform panel = Rect(transform, Channels[c], 0, 65, 390, 530);
                _panels[c] = panel;
                _channelFades[c] = panel.gameObject.AddComponent<CanvasGroup>();
                Box(panel, "Panel", 0, 0, 390, 530, new Color(0.13f, 0.14f, 0.16f));
                Label(panel, "Channel", Channels[c] + " 픽셀", 0, 225, 350, 40, 28).color = Color.Lerp(Tints[c], Color.white, 0.5f);
                Label(panel, "Kernel", "곱할 수 (가중치)", 0, -10, 350, 36, 24);
                for (int i = 0; i < 9; i++)
                {
                    float x = (i % 3 - 1) * 98;
                    float y = 165 - (i / 3) * 60;
                    _inputCells[c, i] = Box(panel, "Pixel " + i, x, y, 92, 56, Tints[c]);
                    _inputLabels[c, i] = Label(_inputCells[c, i].transform, "Value", "", 0, 0, 92, 56, 27);
                    _weightCells[c, i] = Box(panel, "Weight " + i, x, y - 230, 92, 56, new Color(0.22f, 0.23f, 0.25f));
                    _weightLabels[c, i] = Label(_weightCells[c, i].transform, "Value", Weights[c, i].ToString(), 0, 0, 92, 56, 27);
                }
                _sums[c] = Label(panel, "Sum", Channels[c] + " 합: —", 0, -235, 370, 40, 26);
            }
            _operation = Label(transform, "Multiply", "", 0, -245, 1200, 44, 27);
            _output = Label(transform, "Output", "", 0, -295, 1240, 50, 28);
            _calculate = Button(transform, "학습 다시 하기", 0, -360, 270, () => BeginCalculation());
            _combine = Button(transform, "RGB 합쳐 보기", 0, -285, 330, () => ToggleCombined());
            RectTransform combined = Rect(transform, "Combined RGB pixels", 0, 65, 390, 300);
            _combinedPixels = combined.gameObject;
            _combinedFade = combined.gameObject.AddComponent<CanvasGroup>();
            _combinedFade.blocksRaycasts = false;
            Label(combined, "Label", "R + G + B → 컬러 픽셀", 0, 225, 390, 40, 27);
            for (int i = 0; i < 9; i++)
            {
                _combinedCells[i] = Box(combined, "Pixel " + i, (i % 3 - 1) * 98, 165 - (i / 3) * 60, 92, 56, Color.black);
            }
            _combinedPixels.SetActive(false);
            _moves[0] = Button(transform, "←", -495, -360, 95, () => _lab.MoveSelection(-1, 0));
            _moves[1] = Button(transform, "→", -385, -360, 95, () => _lab.MoveSelection(1, 0));
            _moves[2] = Button(transform, "↑", 385, -360, 95, () => _lab.MoveSelection(0, 1));
            _moves[3] = Button(transform, "↓", 495, -360, 95, () => _lab.MoveSelection(0, -1));
            RectTransform exerciseRoot = Rect(transform, "Convolution exercise", 0, 40, 1260, 840);
            _exercise = exerciseRoot.gameObject.AddComponent<ConvolutionExercise>();
            _exercise.Initialize(font);
            Button(transform, "이미지로 돌아가기", 0, -430, 360, () => _lab.CloseDetails());
            Image helpBackground = Box(transform, "Side explanation", 900, 60, 480, 770, new Color(0.1f, 0.12f, 0.15f));
            _help = helpBackground.gameObject;
            Label(_help.transform, "Title", "세 채널을 합친 필터 하나", 0, 325, 440, 60, 28);
            Text helpText = Label(_help.transform, "Explanation",
                "각 채널의 픽셀과 가중치를 같은 위치끼리 곱해 더했어요.\n\n" +
                "R 합 + G 합 + B 합 + 편향이 이번 위치의 최종 결과예요.\n\n" +
                "가중치는 그대로 두고 위치를 바꾸면, 새로운 입력에 같은 계산을 적용해요.\n\n" +
                "화살표로 위치를 바꾸어 다시 학습해 보세요.",
                0, -10, 420, 590, 27);
            helpText.alignment = TextAnchor.UpperLeft;
            _help.SetActive(false);
        }

        public void Show(Color32[] pixels, int columns, int rows, Vector2Int selected, bool calculationAvailable)
        {
            _calculationAvailable = calculationAvailable;
            gameObject.SetActive(true);
            StopAllCoroutines();
            _animation = null;
            _columns = columns;
            _rows = rows;
            _selected = selected;
            _combined = false;
            _combinedPixels.SetActive(false);
            _combine.GetComponentInChildren<Text>().text = "RGB 합쳐 보기";
            _exercise.gameObject.SetActive(false);
            _help.SetActive(false);
            _position.text = "R 빨강 · G 초록 · B 파랑 | 0: 없음 → 255: 가장 강함";
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 9; i++)
                {
                    Color32 pixel = pixels[(selected.y + 2 - i / 3) * columns + selected.x + i % 3];
                    int value = c == 0 ? pixel.r : c == 1 ? pixel.g : pixel.b;
                    _inputs[c, i] = value;
                    _combinedCells[i].color = pixel;
                    _inputLabels[c, i].text = value.ToString();
                    _weightLabels[c, i].text = Weights[c, i].ToString();
                    _weightCells[c, i].gameObject.SetActive(calculationAvailable);
                }
                _panels[c].Find("Kernel").gameObject.SetActive(calculationAvailable);
                _sums[c].gameObject.SetActive(calculationAvailable);
                RectTransform panelBackground = (RectTransform)_panels[c].Find("Panel");
                panelBackground.sizeDelta = new Vector2(390, calculationAvailable ? 530 : 300);
                panelBackground.anchoredPosition = new Vector2(0, calculationAvailable ? 0 : 120);
                _sums[c].text = Channels[c] + " 합: —";
                _channelFades[c].alpha = 1;
                _panels[c].localPosition = new Vector3((c - 1) * 420, 65, 0);
            }
            ClearHighlights();
            _operation.text = "‘직접 계산하기’를 눌러 답을 고르고 빈칸을 채워 보세요";
            _output.text = "같은 위치끼리 곱하기 → 색마다 더하기 → 세 색과 편향 합치기";
            _operation.gameObject.SetActive(calculationAvailable);
            _output.gameObject.SetActive(calculationAvailable);
            _calculate.gameObject.SetActive(calculationAvailable);
            _combine.gameObject.SetActive(!calculationAvailable);
            foreach (Button move in _moves) { move.gameObject.SetActive(calculationAvailable); }
            _help.transform.Find("Title").GetComponent<Text>().text = calculationAvailable ? "세 채널을 합친 필터 하나" : "픽셀의 RGB 값";
            _help.transform.Find("Explanation").GetComponent<Text>().text = calculationAvailable
                ? "각 채널에서 같은 위치의 픽셀과 고정 가중치를 곱해 더해요.\n\nR 합 + G 합 + B 합 + 편향이 최종 결과예요."
                : "픽셀 하나의 색은 빨강(R), 초록(G), 파랑(B)의 세기로 표현해요.\n\n세 판의 같은 위치는 같은 픽셀을 뜻해요.\n\n‘RGB 합쳐 보기’를 눌러 세 색이 원래 픽셀을 이루는 모습을 보세요.\n\n관찰을 마치면 이미지로 돌아가 ‘다음으로’를 누르세요.";
            SetControls(true);
            if (calculationAvailable) { BeginCalculation(); }
            else { _help.SetActive(true); _animation = StartCoroutine(Unfold()); }
        }

        private IEnumerator Unfold()
        {
            SetControls(false);
            float elapsed = 0;
            while (elapsed < 0.65f)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / 0.65f);
                for (int c = 0; c < 3; c++)
                {
                    _panels[c].localPosition = Vector3.Lerp(new Vector3(0, 65, -c * 18), new Vector3((c - 1) * 420, 65, 0), t);
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            for (int c = 0; c < 3; c++) { _panels[c].localPosition = new Vector3((c - 1) * 420, 65, 0); }
            _animation = null;
            SetControls(true);
        }

        private void BeginCalculation()
        {
            if (!_calculationAvailable || _animation != null || _exercise.gameObject.activeSelf) { return; }
            SetControls(false);
            _help.SetActive(false);
            _exercise.Begin(_inputs, Weights, CompleteExercise);
            SetControls(false);
        }

        private void ToggleCombined()
        {
            if (_calculationAvailable || _animation != null) { return; }
            _animation = StartCoroutine(AnimateCombined(!_combined));
        }

        private IEnumerator AnimateCombined(bool combine)
        {
            SetControls(false);
            _combinedPixels.SetActive(true);
            float elapsed = 0;
            while (elapsed < 1.5f)
            {
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 1.5f));
                float merge = combine ? t : 1 - t;
                for (int c = 0; c < 3; c++)
                {
                    _panels[c].localPosition = new Vector3((c - 1) * 420 * (1 - merge), 65, 0);
                    _channelFades[c].alpha = 1 - Mathf.Clamp01((merge - 0.55f) / 0.45f);
                }
                _combinedFade.alpha = Mathf.Clamp01((merge - 0.55f) / 0.45f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _combined = combine;
            for (int c = 0; c < 3; c++)
            {
                _panels[c].localPosition = new Vector3(combine ? 0 : (c - 1) * 420, 65, 0);
                _channelFades[c].alpha = combine ? 0 : 1;
            }
            _combinedFade.alpha = combine ? 1 : 0;
            _combinedPixels.SetActive(combine);
            _combine.GetComponentInChildren<Text>().text = combine ? "RGB 다시 펼치기" : "RGB 합쳐 보기";
            _animation = null;
            SetControls(true);
        }

        private void CompleteExercise(int[] sums)
        {
            _help.SetActive(true);
            _animation = StartCoroutine(Unfold());
            for (int c = 0; c < 3; c++) { _sums[c].text = $"{Channels[c]} 합: {sums[c]}"; }
            _operation.text = $"R {sums[0]} + G {sums[1]} + B {sums[2]} + 편향 0";
            _output.text = $"이번 위치의 합성곱 결과: {sums[0] + sums[1] + sums[2]}";
            SetControls(true);
        }

        private void ClearHighlights()
        {
            for (int c = 0; c < 3; c++)
                for (int i = 0; i < 9; i++)
                {
                    float intensity = _inputs[c, i] / 255f;
                    _inputCells[c, i].color = c == 0 ? new Color(intensity, 0, 0) : c == 1 ? new Color(0, intensity, 0) : new Color(0, 0, intensity);
                    _weightCells[c, i].color = new Color(0.22f, 0.23f, 0.25f);
                    _inputLabels[c, i].color = _weightLabels[c, i].color = Color.white;
                    if (c == 1 && intensity > 0.68f) { _inputLabels[c, i].color = Color.black; }
                }
        }

        private void SetControls(bool enabled)
        {


            enabled = enabled && _animation == null && (_exercise == null || !_exercise.gameObject.activeSelf);
            _calculate.interactable = enabled;
            _combine.interactable = enabled;
            _moves[0].interactable = enabled && _selected.x > 0;
            _moves[1].interactable = enabled && _selected.x < _columns - 3;
            _moves[2].interactable = enabled && _selected.y < _rows - 3;
            _moves[3].interactable = enabled && _selected.y > 0;
        }

        private void OnDisable() { StopAllCoroutines(); _animation = null; }

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
