using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Parchment
{
    public sealed class ParchmentReaderController : MonoBehaviour
    {
        [SerializeField] private Text _source;
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Text _pageLabel;
        [SerializeField] private Button _previous;
        [SerializeField] private Button _next;
        [SerializeField] private Button _complete;
        [SerializeField] private Button _close;
        [SerializeField] private ParchmentIllustrationController _illustration;
        [SerializeField] private TrackedDeviceGraphicRaycaster _raycaster;
        private IReadOnlyList<ParchmentPage> _pages;
        private int _page;
        public event Action Completed;
        public event Action CloseRequested;

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
            _source.text = "양피지 " + (index + 1).ToString("00") + "  /  " + lesson.Department;
            _title.text = lesson.Title;
            GetComponent<Canvas>().worldCamera = camera;
            RefreshPage();
            StartCoroutine(Expand(camera, origin));
        }
        private IEnumerator Expand(Camera camera, Pose origin)
        {
            _raycaster.enabled = false;
            Vector3 target = camera.transform.position + camera.transform.forward * 1.55f - camera.transform.up * 0.08f;
            Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.3f);
                transform.SetPositionAndRotation(Vector3.Lerp(origin.position, target, t), Quaternion.Slerp(origin.rotation, rotation, t));
                transform.localScale = Vector3.one * Mathf.Lerp(0.0006f, 0.001f, t);
                yield return null;
            }
            _raycaster.enabled = true;
        }
        private void Previous() { _page = Mathf.Max(0, _page - 1); RefreshPage(); }
        private void Next() { _page = Mathf.Min(_pages.Count - 1, _page + 1); RefreshPage(); }
        private void Complete() { if (_page == _pages.Count - 1) { Completed?.Invoke(); } }
        private void RequestClose() => CloseRequested?.Invoke();
        private void RefreshPage()
        {
            _body.text = _pages[_page].Body;
            _illustration.Show(_pages[_page].Illustration);
            _pageLabel.text = (_page + 1) + " / " + _pages.Count;
            _previous.interactable = _page > 0;
            _next.gameObject.SetActive(_page < _pages.Count - 1);
            _complete.gameObject.SetActive(_page == _pages.Count - 1);
        }
    }
}
