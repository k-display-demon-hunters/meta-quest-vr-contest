using System;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FactoryEnvironment
{
    public sealed class FactoryTutorialPanelController : MonoBehaviour
    {
        private Text _title;
        private Text _body;
        private Text _nextLabel;
        private Button _previous;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedModule;
        public event Action OnPrevious;
        public event Action OnNext;
        public event Action OnFinish;

        public void Initialize(Font font, Camera camera)
        {
            GameObject panel = new GameObject("Entrance control guide", typeof(RectTransform), typeof(Canvas),
                typeof(TrackedDeviceGraphicRaycaster));
            panel.transform.SetParent(transform, false);
            panel.transform.localScale = Vector3.one * 0.002f;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 580f);
            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            Image background = Rect(panel.transform, "Panel", Vector2.zero, new Vector2(1000f, 580f)).gameObject.AddComponent<Image>();
            background.color = SceneUIStyle.Surface;
            background.raycastTarget = false;
            _title = Label(panel.transform, font, "Title", new Vector2(0f, 205f), new Vector2(920f, 80f), 38);
            _body = Label(panel.transform, font, "Instructions", new Vector2(0f, 25f), new Vector2(890f, 250f), 30);
            _previous = MakeButton(panel.transform, font, "이전", new Vector2(-290f, -205f), () => OnPrevious?.Invoke());
            Button next = MakeButton(panel.transform, font, "다음", new Vector2(0f, -205f), () => OnNext?.Invoke());
            _nextLabel = next.GetComponentInChildren<Text>();
            MakeButton(panel.transform, font, "안내 마치기", new Vector2(290f, -205f), () => OnFinish?.Invoke());
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Factory guide UI input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform.parent, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        public void ShowPage(int page, int count, string title, string body)
        {
            _title.text = $"조작 안내 {page + 1} / {count} · {title}";
            _body.text = body;
            _previous.interactable = page > 0;
            _nextLabel.text = page == count - 1 ? "탐색 시작" : "다음";
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

        private void OnDestroy()
        {
            if (_ownedEventSystem != null)
            {
                _ownedEventSystem.SetActive(false);
                Destroy(_ownedEventSystem);
            }
            if (_addedModule != null)
            {
                _addedModule.enabled = false;
                Destroy(_addedModule);
            }
        }
    }
}
