using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace UI
{
    public sealed class PixelRevealStage : MonoBehaviour
    {
        public enum RevealPhase { Inactive, AwaitingClick, Revealing, Revealed }

        [SerializeField] private Transform _board;
        [SerializeField] private Transform _controls;
        [SerializeField] private Camera _camera;
        [SerializeField] private Font _font;
        [SerializeField] private Vector2 _boardSize = new Vector2(2.4f, 1.35f);
        [SerializeField] private float _revealDuration = 2f;
        [SerializeField] private UnityEvent _onPixelsRevealed = new UnityEvent();
        private CanvasGroup[] _columns;
        private GameObject _pixelCanvas;
        private BoxCollider _clickCollider;
        private XRSimpleInteractable _clickTarget;
        private bool _initialized;
        private Color32[] _sourcePixels;
        private Image[] _fills;
        private Text[] _numbers;
        private GameObject _channelControls;
        private Button[] _channelButtons;
        private Text _instruction;
        public event System.Action PixelsRevealed;

        public RevealPhase Phase { get; private set; }
        public bool IsReadyForConvolution => Phase == RevealPhase.Revealed;

        public void ShowImage(Color32[] pixels, int columns, int rows)
        {
            if (Phase != RevealPhase.Inactive) { return; }
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("[CNNPixelLab] An active XRPlayerRig camera is required.", this);
                return;
            }
            if (!_initialized)
            {
                BuildPixels(pixels, columns, rows);
                _instruction = Rect(_controls, "Reveal instruction", 0, -70, 1200, 70).gameObject.AddComponent<Text>();
                _instruction.font = _font;
                _instruction.fontSize = 26;
                _instruction.alignment = TextAnchor.MiddleCenter;
                _instruction.color = Color.white;
                _instruction.raycastTarget = false;
                // Enable image selection only during this step so it cannot intercept filter dragging.
                _clickCollider = _board.gameObject.AddComponent<BoxCollider>();
                _clickCollider.size = new Vector3(_boardSize.x, _boardSize.y, 0.04f);
                _clickCollider.enabled = false;
                _clickTarget = _board.gameObject.AddComponent<XRSimpleInteractable>();
                _clickTarget.colliders.Clear();
                _clickTarget.colliders.Add(_clickCollider);
                _clickTarget.activated.AddListener(HandleImageSelected);
                _clickTarget.enabled = false;
                _initialized = true;
            }
            _pixelCanvas.SetActive(true);
            foreach (CanvasGroup column in _columns) { column.alpha = 0; }
            Phase = RevealPhase.AwaitingClick;
            _instruction.gameObject.SetActive(true);
            _instruction.text = "이미지를 선택하세요\n오른손: 이미지를 가리키고 Trigger를 누르세요";
            _clickCollider.enabled = true;
            _clickTarget.enabled = true;
        }

        public void RevealPixels()
        {
            if (Phase != RevealPhase.AwaitingClick) { return; }
            Phase = RevealPhase.Revealing;
            _instruction.text = "이미지가 픽셀과 숫자로 바뀌고 있어요";
            _channelControls.SetActive(true);
            StartCoroutine(RevealColumns());
        }

        public void ResetStage()
        {
            StopAllCoroutines();
            Phase = RevealPhase.Inactive;
            if (!_initialized) { return; }
            _clickTarget.enabled = false;
            _clickCollider.enabled = false;
            _pixelCanvas.SetActive(false);
            _channelControls.SetActive(false);
            _instruction.gameObject.SetActive(false);
        }

        public void CompleteReveal()
        {
            if (Phase != RevealPhase.AwaitingClick && Phase != RevealPhase.Revealing) { return; }
            StopAllCoroutines();
            _channelControls.SetActive(true);
            FinishReveal();
        }

        private IEnumerator RevealColumns()
        {
            float elapsed = 0;
            const float FadeWidth = 0.2f;
            while (elapsed < _revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, _revealDuration));
                for (int x = 0; x < _columns.Length; x++)
                {
                    float start = x / (float)Mathf.Max(1, _columns.Length - 1) * (1 - FadeWidth);
                    _columns[x].alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01((progress - start) / FadeWidth));
                }
                yield return null;
            }
            FinishReveal();
        }

        private void FinishReveal()
        {
            foreach (CanvasGroup column in _columns) { column.alpha = 1; }
            Phase = RevealPhase.Revealed;
            _instruction.gameObject.SetActive(false);
            _clickTarget.enabled = false;
            _clickCollider.enabled = false;
            _onPixelsRevealed.Invoke();
            PixelsRevealed?.Invoke();
        }

        private void BuildPixels(Color32[] pixels, int columns, int rows)
        {
            const float CellSize = 96;
            _sourcePixels = pixels;
            _fills = new Image[pixels.Length];
            _numbers = new Text[pixels.Length];
            _pixelCanvas = new GameObject("Image pixels and values", typeof(RectTransform), typeof(Canvas));
            RectTransform canvasRect = (RectTransform)_pixelCanvas.transform;
            canvasRect.SetParent(_board, false);
            canvasRect.localPosition = Vector3.back * 0.045f;
            canvasRect.sizeDelta = new Vector2(columns * CellSize, rows * CellSize);
            canvasRect.localScale = new Vector3(_boardSize.x / (columns * CellSize), _boardSize.y / (rows * CellSize), 1);
            Canvas canvas = _pixelCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;
            _columns = new CanvasGroup[columns];
            for (int x = 0; x < columns; x++)
            {
                RectTransform columnRect = Rect(canvasRect, "Column " + x, (x + 0.5f - columns / 2f) * CellSize, 0, CellSize, rows * CellSize);
                CanvasGroup column = columnRect.gameObject.AddComponent<CanvasGroup>();
                column.alpha = 0;
                column.blocksRaycasts = false;
                column.interactable = false;
                _columns[x] = column;
                for (int y = 0; y < rows; y++)
                {
                    Color32 pixel = pixels[y * columns + x];
                    RectTransform cell = Rect(columnRect, "Pixel " + x + "," + (rows - 1 - y), 0, (y + 0.5f - rows / 2f) * CellSize, CellSize, CellSize);
                    Image fill = cell.gameObject.AddComponent<Image>();
                    fill.color = pixel;
                    fill.raycastTarget = false;
                    Text text = Rect(cell, "Value", 0, 0, CellSize - 4, CellSize - 4).gameObject.AddComponent<Text>();
                    text.font = _font;
                    text.fontSize = 30;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = ((Color)pixel).grayscale > 0.55f ? Color.black : Color.white;
                    text.text = pixel.r.ToString();
                    text.raycastTarget = false;
                    text.supportRichText = false;
                    _fills[y * columns + x] = fill;
                    _numbers[y * columns + x] = text;
                }
            }
            _channelControls = Rect(_controls, "RGB channels", 0, 120, 600, 65).gameObject;
            _channelButtons = new Button[3];
            Text hint = Rect(_channelControls.transform, "Channel help", 0, 75, 1100, 45).gameObject.AddComponent<Text>();
            hint.font = _font;
            hint.fontSize = 26;
            hint.text = "색 선택: Trigger · 필터 이동: Grip · 숫자는 색의 세기 (0~255)";
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = Color.white;
            hint.raycastTarget = false;
            string[] labels = { "R · 빨강", "G · 초록", "B · 파랑" };
            for (int c = 0; c < 3; c++)
            {
                int channel = c;
                RectTransform rect = Rect(_channelControls.transform, labels[c], (c - 1) * 180, 0, 160, 65);
                Image image = rect.gameObject.AddComponent<Image>();
                image.color = new Color(0.65f, 0.67f, 0.7f);
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SetChannel(channel));
                _channelButtons[c] = button;
                Text label = Rect(rect, "Label", 0, 0, 160, 65).gameObject.AddComponent<Text>();
                label.font = _font;
                label.fontSize = 28;
                label.text = labels[c];
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.black;
                label.raycastTarget = false;
                UI.SceneUIStyle.Apply(button);
            }
            SetChannel(0);
            _channelControls.SetActive(false);
        }

        private void SetChannel(int channel)
        {
            for (int i = 0; i < _sourcePixels.Length; i++)
            {
                Color32 pixel = _sourcePixels[i];
                byte value = channel == 0 ? pixel.r : channel == 1 ? pixel.g : pixel.b;
                _fills[i].color = channel == 0 ? new Color32(value, 0, 0, 255)
                    : channel == 1 ? new Color32(0, value, 0, 255) : new Color32(0, 0, value, 255);
                _numbers[i].text = value.ToString();
                _numbers[i].color = channel == 1 && value > 175 ? Color.black : Color.white;
            }
            for (int c = 0; c < 3; c++) { _channelButtons[c].interactable = c != channel; }
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private void HandleImageSelected(ActivateEventArgs args)
        {
            RevealPixels();
        }

        private void OnDisable()
        {
            ResetStage();
        }
    }
}
