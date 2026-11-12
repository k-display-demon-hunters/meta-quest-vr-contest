using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Parchment
{
    public sealed class ParchmentReaderController : MonoBehaviour
    {
        [SerializeField] private Text _source;
        [SerializeField] private Text _title;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private Text _pageLabel;
        [SerializeField] private Button _previous;
        [SerializeField] private Button _next;
        [SerializeField] private Button _complete;
        [SerializeField] private Button _close;
        [SerializeField] private ParchmentIllustrationController _illustration;
        [SerializeField] private TrackedDeviceGraphicRaycaster _raycaster;
        private const float RollerFraction = 0.17f;
        private const float UnfoldDuration = 0.8f;
        private Image _scroll;
        private RawImage _sheet;
        private RawImage _leftRoller;
        private RawImage _rightRoller;
        private CanvasGroup _content;
        private Vector2 _scrollSize;
        private IReadOnlyList<ParchmentPage> _pages;
        private int _page;
        private string _lessonTitle;
        private string _lessonSource;
        public event Action Completed;
        public event Action CloseRequested;

        private void Awake()
        {
            // The existing prefab stores its background first, followed by the lesson UI.
            // Build only the moving image segments here so the prefab asset remains unchanged.
            _scroll = transform.GetChild(0).GetComponent<Image>();
            _scrollSize = _scroll.rectTransform.sizeDelta;
            Transform[] content = new Transform[transform.childCount - 1];
            for (int i = 0; i < content.Length; i++) { content[i] = transform.GetChild(i + 1); }
            _content = new GameObject("Lesson Content", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            _content.transform.SetParent(transform, false);
            RectTransform contentRect = (RectTransform)_content.transform;
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = ((RectTransform)transform).sizeDelta;
            foreach (Transform child in content) { child.SetParent(_content.transform, false); }

            _sheet = CreateSegment("Unfolding Hologram", RollerFraction, 1f - RollerFraction * 2f);
            _leftRoller = CreateSegment("Left Scroll Roller", 0f, RollerFraction);
            _rightRoller = CreateSegment("Right Scroll Roller", 1f - RollerFraction, RollerFraction);
            _content.transform.SetAsLastSibling();
            _complete.GetComponentInChildren<Text>().text = "학습 완료";
        }

        private RawImage CreateSegment(string name, float start, float width)
        {
            RawImage image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(transform, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.raycastTarget = false;
            Sprite source = _scroll.sprite;
            Rect rect = source.rect;
            image.texture = source.texture;
            image.uvRect = new Rect((rect.x + rect.width * start) / source.texture.width,
                rect.y / source.texture.height, rect.width * width / source.texture.width,
                rect.height / source.texture.height);
            image.rectTransform.sizeDelta = new Vector2(_scrollSize.x * width, _scrollSize.y);
            return image;
        }

        private void OnEnable()
        {
            _previous.onClick.AddListener(Previous);
            _next.onClick.AddListener(Next);
            _complete.onClick.AddListener(Complete);
            _close.onClick.AddListener(RequestClose);
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            _previous.onClick.RemoveListener(Previous);
            _next.onClick.RemoveListener(Next);
            _complete.onClick.RemoveListener(Complete);
            _close.onClick.RemoveListener(RequestClose);
        }
        public void Present(int index, Camera camera, Pose origin)
        {
            ParchmentLesson lesson = ParchmentCatalog.Get(index);
            _pages = lesson.Pages;
            _page = 0;
            _lessonSource = "양피지 " + (index + 1).ToString("00") + "  /  " + lesson.Department;
            _lessonTitle = lesson.Title;
            GetComponent<Canvas>().worldCamera = camera;
            RefreshPage(animateIllustration: false);
            StartCoroutine(Expand(camera, origin));
        }
        private IEnumerator Expand(Camera camera, Pose origin)
        {
            _raycaster.enabled = false;
            _content.alpha = 0f;
            _content.interactable = false;
            _content.blocksRaycasts = false;
            _scroll.enabled = false;
            Vector3 target = camera.transform.position + camera.transform.forward * 1.55f - camera.transform.up * 0.08f;
            Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            transform.SetPositionAndRotation(origin.position, origin.rotation);
            transform.localScale = Vector3.one * (1.25f / _scrollSize.x);
            SetUnfoldProgress(0f);
            float elapsed = 0f;
            while (elapsed < UnfoldDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float move = Mathf.SmoothStep(0f, 1f, elapsed / 0.3f);
                float unfold = Mathf.SmoothStep(0f, 1f, (elapsed - 0.15f) / (UnfoldDuration - 0.15f));
                transform.SetPositionAndRotation(Vector3.Lerp(origin.position, target, move), Quaternion.Slerp(origin.rotation, rotation, move));
                transform.localScale = Vector3.one * Mathf.Lerp(1.25f / _scrollSize.x, 0.001f, move);
                SetUnfoldProgress(unfold);
                yield return null;
            }
            transform.SetPositionAndRotation(target, rotation);
            transform.localScale = Vector3.one * 0.001f;
            SetUnfoldProgress(1f);
            _scroll.enabled = true;
            _sheet.gameObject.SetActive(false);
            _leftRoller.gameObject.SetActive(false);
            _rightRoller.gameObject.SetActive(false);
            _content.interactable = true;
            _content.blocksRaycasts = true;
            _raycaster.enabled = true;
            _illustration.Play();
        }
        private void SetUnfoldProgress(float progress)
        {
            float sheetWidth = _scrollSize.x * (1f - RollerFraction * 2f) * progress;
            float rollerOffset = (sheetWidth + _scrollSize.x * RollerFraction) * 0.5f;
            _sheet.rectTransform.sizeDelta = new Vector2(sheetWidth, _scrollSize.y);
            _leftRoller.rectTransform.anchoredPosition = new Vector2(-rollerOffset, 0f);
            _rightRoller.rectTransform.anchoredPosition = new Vector2(rollerOffset, 0f);
            _content.alpha = Mathf.SmoothStep(0f, 1f, (progress - 0.75f) / 0.25f);
        }
        private void Previous() { _page = Mathf.Max(0, _page - 1); RefreshPage(); }
        private void Next() { _page = Mathf.Min(_pages.Count - 1, _page + 1); RefreshPage(); }
        private void Complete() { if (_page == _pages.Count - 1) { Completed?.Invoke(); } }
        private void RequestClose() => CloseRequested?.Invoke();
        private void RefreshPage(bool animateIllustration = true)
        {
            ParchmentPage page = _pages[_page];
            _source.text = page.Source ?? _lessonSource;
            _title.text = page.Title ?? _lessonTitle;
            _body.text = page.Body;

            _illustration.Show(page.Illustration, animateIllustration);
            _pageLabel.text = (_page + 1) + " / " + _pages.Count;
            _previous.interactable = _page > 0;
            _next.gameObject.SetActive(_page < _pages.Count - 1);
            _complete.gameObject.SetActive(_page == _pages.Count - 1);
        }
    }
}
