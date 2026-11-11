using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class CinematicControlsController : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Image _holdBar;
        [SerializeField] private Text _failureText;
        [SerializeField] private Image _failureBacking;

        private Button _button;
        private Canvas _buttonCanvas;

        internal event Action OnButtonRequested;

        internal void Bind(Camera camera, bool ending)
        {
            _button = ending ? _restartButton : _startButton;
            _buttonCanvas = _button.GetComponentInParent<Canvas>(true);
            _buttonCanvas.worldCamera = camera;
            _button.onClick.AddListener(HandleButton);
        }

        internal void SetButtonVisible(bool visible) => _buttonCanvas.gameObject.SetActive(visible);
        internal void SetHoldProgress(float progress) => _holdBar.fillAmount = progress;

        internal void ShowFailure(string text)
        {
            _failureText.text = text;
            _failureText.fontSize = 26;
            _failureText.color = Color.white;
            _failureBacking.enabled = false;
            _failureText.rectTransform.anchoredPosition = new Vector2(0, 220);
            _failureText.rectTransform.sizeDelta = new Vector2(1450, 550);
        }

        private void HandleButton() => OnButtonRequested?.Invoke();

        private void OnDisable()
        {
            if (_button != null) { _button.onClick.RemoveListener(HandleButton); }
        }
    }
}
