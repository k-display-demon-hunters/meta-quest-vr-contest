using System;
using UnityEngine;
using UnityEngine.UI;

namespace OpeningEnding.Ending
{
    public sealed class FactoryEndingPanelController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _title;
        [SerializeField] private Text _message;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _exitButton;

        public event Action OnRestartRequested;
        public event Action OnExitRequested;

        private void OnEnable()
        {
            _restartButton.onClick.AddListener(HandleRestart);
            _exitButton.onClick.AddListener(HandleExit);
            UI.SceneUIStyle.ApplyButtons(_canvas.transform);
            ShowRestoration();
        }

        public void BindCamera(Camera camera) => _canvas.worldCamera = camera;

        public void ShowRestoration()
        {
            // Hide the canvas and its selectable controls for the entire cinematic.
            _canvas.enabled = false;
            _restartButton.gameObject.SetActive(false);
            _exitButton.gameObject.SetActive(false);
        }

        public void ShowCompletion()
        {
            _title.text = "축하합니다. 공장에 빛이 돌아왔어요.";
            _message.text = string.Empty;
            _canvas.enabled = true;
            _restartButton.gameObject.SetActive(true);
            _exitButton.gameObject.SetActive(true);
            SetNavigationAvailable(true);
        }

        public void ShowMessage(string message) => _message.text = message;

        public void SetNavigationAvailable(bool available)
        {
            _restartButton.interactable = available;
            _exitButton.interactable = available;
        }

        private void HandleRestart() => OnRestartRequested?.Invoke();
        private void HandleExit() => OnExitRequested?.Invoke();

        private void OnDisable()
        {
            _restartButton.onClick.RemoveListener(HandleRestart);
            _exitButton.onClick.RemoveListener(HandleExit);
        }
    }
}
