using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // Layout is saved in PixelCalculationUI.prefab; this component only presents state.
    public sealed class LessonUIController : MonoBehaviour
    {
        private const float ActionGlowPeriod = 1.4f;
        [SerializeField] private GameObject _exerciseRoot;
        [SerializeField] private GameObject _exerciseControls;
        [SerializeField] private Text _caption, _playbackLabel;
        [SerializeField] private Button _next, _skip, _finish, _replay;
        [SerializeField] private Button _playbackButton;
        [SerializeField] private Image _actionGlow;
        private bool _paused;
        private Button _glowingAction;
        private float _actionGlowStartTime;
        public event Action NextRequested;
        public event Action SkipRequested;
        public event Action FinishRequested;
        public event Action PlaybackToggleRequested;
        public event Action RestartRequested;
        public event Action ReplayRequested;
        // One subtitle line; the narration carries the full explanation.
        public string Caption { set => _caption.text = value; }

        private void LateUpdate()
        {
            Button action = ActiveProgressionButton();
            if (_glowingAction != action)
            {
                _glowingAction = action;
                _actionGlow.gameObject.SetActive(action != null);
                if (action == null) { return; }

                RectTransform glow = _actionGlow.rectTransform;
                RectTransform button = (RectTransform)action.transform;
                Vector3 center = glow.parent.InverseTransformPoint(button.TransformPoint(button.rect.center));
                center.z = glow.localPosition.z;
                glow.localPosition = center;
                _actionGlowStartTime = Time.unscaledTime;
            }
            if (action == null) { return; }

            float cycle = (Time.unscaledTime - _actionGlowStartTime) / ActionGlowPeriod;
            float pulse = (Mathf.Cos(cycle * 2f * Mathf.PI) + 1f) * 0.5f;
            _actionGlow.canvasRenderer.SetAlpha(Mathf.Lerp(0.45f, 1f, pulse));
        }

        private Button ActiveProgressionButton()
        {
            if (_paused) { return CanPress(_playbackButton) ? _playbackButton : null; }
            if (CanPress(_next)) { return _next; }
            if (CanPress(_skip)) { return _skip; }
            return CanPress(_finish) ? _finish : null;
        }

        private static bool CanPress(Button button) => button.isActiveAndEnabled && button.IsInteractable();

        public void Initialize()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void RequestNext() { NextRequested?.Invoke(); }
        public void RequestSkip() { SkipRequested?.Invoke(); }
        public void RequestFinish() { FinishRequested?.Invoke(); }
        public void RequestPlaybackToggle() { PlaybackToggleRequested?.Invoke(); }
        public void RequestRestart() { RestartRequested?.Invoke(); }
        public void RequestReplay() { ReplayRequested?.Invoke(); }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            _playbackLabel.text = paused ? "체험 재개" : "체험 멈추기";
            _skip.interactable = !paused;
            _next.interactable = !paused;
            _replay.interactable = !paused;
        }

        public void ShowSkip(bool pendingKernel, string label = "필터 자동 배치")
        {
            // Quiz answers are never bypassed: this assistance only finishes a physical action.
            _skip.GetComponentInChildren<Text>(true).text = label;
            _skip.gameObject.SetActive(pendingKernel);
        }
        public void ShowReplay(bool visible) => _replay.gameObject.SetActive(visible);

        public void BeginCalculation()
        {
            gameObject.SetActive(true);
            _exerciseRoot.SetActive(true);
            _exerciseControls.SetActive(true);
            _next.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
            _finish.gameObject.SetActive(false);
            _replay.gameObject.SetActive(false);
        }
        public void ShowFinish()
        {
            _exerciseControls.SetActive(false);
            _finish.interactable = true;
            _finish.gameObject.SetActive(true);
        }
        public void DisableFinish() => _finish.interactable = false;
        public void ShowNext(string label)
        {
            _next.interactable = true;
            _next.gameObject.SetActive(true);
            _next.GetComponentInChildren<Text>().text = label;
        }
        public void HideNext()
        {
            _next.gameObject.SetActive(false);
        }
    }
}
