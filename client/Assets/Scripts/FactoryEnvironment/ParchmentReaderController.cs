using System;
using System.Collections;
using Tablet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Education
{
    [DisallowMultipleComponent]
    public sealed class ParchmentReaderController : MonoBehaviour
    {
        private const float OpenDuration = 0.3f;
        private const float OpenScale = 0.001f;

        private RectTransform _rect;
        private TrackedDeviceGraphicRaycaster _raycaster;
        private Text _source;
        private Text _title;
        private Text _body;
        private Text _pageLabel;
        private Button _previous;
        private Button _next;
        private Button _complete;
        private string[] _pages;
        private Action _onComplete;
        private Action _onClose;
        private int _pageIndex;

        public void Initialize(int lessonIndex, Camera camera, Sprite scroll, Font font, Font titleFont,
            Vector3 startPosition, Quaternion startRotation, Action onComplete, Action onClose)
        {
            ParchmentLesson lesson = ParchmentLesson.All[lessonIndex];
            _pages = lesson.Pages;
            _onComplete = onComplete;
            _onClose = onClose;
            _rect = GetComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(1400f, 788f);
            _rect.position = startPosition;
            _rect.rotation = startRotation;
            _rect.localScale = Vector3.one * 0.0006f;

            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;
            _raycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
            _raycaster.enabled = false;

            Image background = AddImage(transform, "Holographic scroll", Vector2.zero, _rect.sizeDelta,
                Color.white, false);
            background.sprite = scroll;
            _source = Label(transform, font, "Document number", new Vector2(0f, 213f),
                new Vector2(920f, 35f), 23, TextAnchor.MiddleLeft);
            _title = Label(transform, titleFont != null ? titleFont : font, "Document title",
                new Vector2(0f, 160f), new Vector2(920f, 65f), 46, TextAnchor.MiddleLeft);
            AddImage(transform, "Hologram rule", new Vector2(0f, 119f),
                new Vector2(920f, 2f), new Color(0.56f, 0.91f, 1f, 0.8f), false);
            _body = Label(transform, font, "Document body", new Vector2(-145f, -58f),
                new Vector2(630f, 322f), 25, TextAnchor.UpperLeft);
            _body.resizeTextForBestFit = true;
            _body.resizeTextMinSize = 20;
            _body.resizeTextMaxSize = 25;
            _body.lineSpacing = 1.07f;
            AddIllustration(lessonIndex, font);

            AddImage(transform, "Control backdrop", new Vector2(0f, -303f),
                new Vector2(940f, 72f), new Color(0.01f, 0.08f, 0.15f, 0.96f), false);
            _previous = MakeButton(transform, font, "이전", new Vector2(-330f, -303f),
                new Vector2(145f, 50f), () => ChangePage(-1));
            _pageLabel = Label(transform, font, "Page count", new Vector2(-115f, -303f),
                new Vector2(135f, 48f), 22, TextAnchor.MiddleCenter);
            _next = MakeButton(transform, font, "다음", new Vector2(100f, -303f),
                new Vector2(155f, 50f), () => ChangePage(1));
            _complete = MakeButton(transform, font, "완료", new Vector2(100f, -303f),
                new Vector2(155f, 50f), Complete);
            MakeButton(transform, font, "닫기", new Vector2(335f, -303f),
                new Vector2(130f, 50f), () => _onClose?.Invoke());

            _source.text = "양피지 " + (lessonIndex + 1).ToString("00") + "  /  " + lesson.Department;
            _title.text = lesson.Title;
            ShowPage();
            StartCoroutine(Expand(camera));
        }

        private IEnumerator Expand(Camera camera)
        {
            Vector3 fromPosition = _rect.position;
            Quaternion fromRotation = _rect.rotation;
            Vector3 targetPosition = camera.transform.position + camera.transform.forward * 1.55f
                - camera.transform.up * 0.08f;
            Quaternion targetRotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            float elapsed = 0f;
            while (elapsed < OpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.Clamp01(elapsed / OpenDuration);
                amount = amount * amount * (3f - 2f * amount);
                _rect.position = Vector3.Lerp(fromPosition, targetPosition, amount);
                _rect.rotation = Quaternion.Slerp(fromRotation, targetRotation, amount);
                _rect.localScale = Vector3.one * Mathf.Lerp(0.0006f, OpenScale, amount);
                yield return null;
            }
            _rect.position = targetPosition;
            _rect.rotation = targetRotation;
            _rect.localScale = Vector3.one * OpenScale;
            _raycaster.enabled = true;
        }

        private void ChangePage(int delta)
        {
            _pageIndex = Mathf.Clamp(_pageIndex + delta, 0, _pages.Length - 1);
            ShowPage();
        }

        private void ShowPage()
        {
            _body.text = _pages[_pageIndex];
            _pageLabel.text = (_pageIndex + 1) + " / " + _pages.Length;
            _previous.interactable = _pageIndex > 0;
            bool lastPage = _pageIndex == _pages.Length - 1;
            _next.gameObject.SetActive(!lastPage);
            _complete.gameObject.SetActive(lastPage);
        }

        private void Complete()
        {
            if (_pageIndex == _pages.Length - 1)
            {
                _onComplete?.Invoke();
            }
        }

        private void AddIllustration(int lessonIndex, Font font)
        {
            RectTransform illustration = Rect(transform, "Lesson illustration", new Vector2(338f, -55f),
                new Vector2(274f, 292f));
            AddImage(illustration, "Illustration panel", Vector2.zero, new Vector2(274f, 292f),
                new Color(0.01f, 0.12f, 0.21f, 0.68f), false);
            switch (lessonIndex)
            {
                case 0:
                    DrawGrid(illustration, 8, 8, 23f, (column, row) =>
                        row == 2 && (column == 2 || column == 5) ||
                        row == 5 && column >= 2 && column <= 5
                            ? new Color(0.66f, 0.92f, 1f) : new Color(0.09f, 0.25f, 0.37f));
                    Caption(illustration, font, "8 × 8 픽셀 격자");
                    break;
                case 1:
                    DrawGrid(illustration, 5, 5, 34f, (column, row) =>
                        column >= 1 && column <= 3 && row >= 1 && row <= 3
                            ? new Color(0.33f, 0.72f, 0.94f) : new Color(0.08f, 0.24f, 0.35f));
                    Caption(illustration, font, "3 × 3 필터 · 패딩 1");
                    break;
                case 2:
                    Label(illustration, font, "Vertical edge filter", new Vector2(0f, 10f),
                        new Vector2(220f, 160f), 35, TextAnchor.MiddleCenter).text =
                        "−1    0    1\n−1    0    1\n−1    0    1";
                    Caption(illustration, font, "세로 경계 검출 필터");
                    break;
                case 3:
                    for (int index = 0; index < 3; index++)
                    {
                        int mapIndex = index;
                        RectTransform map = Rect(illustration, "Feature map " + index,
                            new Vector2((index - 1) * 85f, 5f), new Vector2(75f, 90f));
                        DrawGrid(map, 5, 5, 13f, (column, row) =>
                            mapIndex == 0 ? (column == 2 ? new Color(0.6f, 0.9f, 1f) : new Color(0.08f, 0.24f, 0.35f)) :
                            mapIndex == 1 ? (row == 2 ? new Color(0.6f, 0.9f, 1f) : new Color(0.08f, 0.24f, 0.35f)) :
                            (column == row ? new Color(0.6f, 0.9f, 1f) : new Color(0.08f, 0.24f, 0.35f)));
                    }
                    Caption(illustration, font, "세로 · 가로 · 주변 반응");
                    break;
                case 4:
                    int[] values = { 0, 0, 0, 2, 5 };
                    string[] inputs = { "−3", "−1", "0", "2", "5" };
                    AddImage(illustration, "Zero line", new Vector2(0f, -64f),
                        new Vector2(220f, 2f), new Color(0.63f, 0.9f, 1f), false);
                    for (int index = 0; index < values.Length; index++)
                    {
                        float x = (index - 2) * 48f;
                        float height = Mathf.Max(3f, values[index] * 25f);
                        AddImage(illustration, "ReLU output " + index,
                            new Vector2(x, -64f + height * 0.5f), new Vector2(29f, height),
                            new Color(0.38f, 0.83f, 1f), false);
                        Label(illustration, font, "ReLU input " + index, new Vector2(x, -91f),
                            new Vector2(43f, 25f), 17, TextAnchor.MiddleCenter).text = inputs[index];
                    }
                    Caption(illustration, font, "ReLU: 음수는 0");
                    break;
            }
        }

        private static void DrawGrid(Transform parent, int columns, int rows, float spacing,
            Func<int, int, Color> cellColor)
        {
            float firstX = -(columns - 1) * spacing * 0.5f;
            float firstY = (rows - 1) * spacing * 0.5f + 12f;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    AddImage(parent, "Cell " + row + "-" + column,
                        new Vector2(firstX + column * spacing, firstY - row * spacing),
                        new Vector2(spacing - 2f, spacing - 2f), cellColor(column, row), false);
                }
            }
        }

        private static void Caption(Transform parent, Font font, string value)
        {
            Label(parent, font, "Illustration caption", new Vector2(0f, -119f),
                new Vector2(255f, 31f), 19, TextAnchor.MiddleCenter).text = value;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image AddImage(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, bool raycastTarget)
        {
            Image image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position,
            Vector2 size, int fontSize, TextAnchor alignment)
        {
            Text text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, Font font, string caption, Vector2 position,
            Vector2 size, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(parent, caption, position, size);
            Image surface = rect.gameObject.AddComponent<Image>();
            surface.color = new Color(0.03f, 0.17f, 0.27f, 0.98f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.73f, 0.91f, 1f);
            colors.pressedColor = new Color(0.45f, 0.78f, 1f);
            button.colors = colors;
            Label(rect, font, caption + " label", Vector2.zero,
                size - new Vector2(10f, 5f), 24, TextAnchor.MiddleCenter).text = caption;
            button.onClick.AddListener(action);
            return button;
        }
    }
}
