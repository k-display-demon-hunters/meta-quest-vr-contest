using System;
using UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialPanelController : MonoBehaviour
    {
        private Text _title;
        private Text _body;
        private Text _nextLabel;
        private Button _previous;
        private Button _finish;
        public event Action OnPrevious;
        public event Action OnNext;
        public event Action OnFinish;

        public void Initialize(Font font, Camera camera)
        {
            GameObject panel = new GameObject("Entrance control guide", typeof(RectTransform), typeof(Canvas),
                typeof(TrackedDeviceGraphicRaycaster));
            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            panel.transform.SetParent(transform, false);
            panel.transform.localScale = Vector3.one * 0.002f;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 580f);
            Image background = Rect(panel.transform, "Panel", Vector2.zero, new Vector2(1000f, 580f)).gameObject.AddComponent<Image>();
            background.color = SceneUIStyle.Surface;
            background.raycastTarget = false;
            _title = Label(panel.transform, font, "Title", new Vector2(0f, 205f), new Vector2(920f, 80f), 38);
            _body = Label(panel.transform, font, "Instructions", new Vector2(0f, 25f), new Vector2(890f, 250f), 30);
            _previous = MakeButton(panel.transform, font, "이전", new Vector2(-290f, -205f), () => OnPrevious?.Invoke());
            Button next = MakeButton(panel.transform, font, "다음", new Vector2(0f, -205f), () => OnNext?.Invoke());
            _nextLabel = next.GetComponentInChildren<Text>();
            _finish = MakeButton(panel.transform, font, "건너뛰기", new Vector2(290f, -205f), () => OnFinish?.Invoke());
        }

        public void ShowPage(int page, int count, string title, string body)
        {
            _title.text = $"공장 안내 {page + 1} / {count} · {title}";
            _body.text = body;
            _previous.interactable = page > 0;
            _nextLabel.text = page == count - 1 ? "탐색 시작" : "다음";
        }

        public void ConfigureVisualGuide()
        {
            RectTransform panel = (RectTransform)_title.transform.parent;
            panel.Find("Panel").GetComponent<RectTransform>().sizeDelta = new Vector2(1450, 750);
            _title.rectTransform.anchoredPosition = new Vector2(0, 300);
            _title.rectTransform.sizeDelta = new Vector2(1350, 80);
            _body.rectTransform.anchoredPosition = new Vector2(300, 50);
            _body.rectTransform.sizeDelta = new Vector2(650, 380);
            _previous.GetComponent<RectTransform>().anchoredPosition = new Vector2(-290, -300);
            _nextLabel.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);
            _finish.GetComponent<RectTransform>().anchoredPosition = new Vector2(290, -300);
        }

        public void ShowBriefing(string title, string body)
        {
            _title.text = title;
            _body.text = body;
            _previous.gameObject.SetActive(false);
            _finish.gameObject.SetActive(false);
            _nextLabel.text = "체험 시작";
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Text Label(Transform parent, Font font, string name, Vector2 position, Vector2 size, int fontSize)
        {
            Text text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, Font font, string caption, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(parent, caption, position, new Vector2(250f, 72f));
            Image surface = rect.gameObject.AddComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            Label(rect, font, "Label", Vector2.zero, new Vector2(230f, 64f), 27).text = caption;
            button.onClick.AddListener(action);
            SceneUIStyle.Apply(button);
            return button;
        }
    }
}
