using System;
using UnityEngine;
using UnityEngine.UI;

namespace OpeningEnding.Ending
{
    public sealed class FactoryEndingPanelController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _backdrop;
        [SerializeField] private Text _title;
        [SerializeField] private Text _message;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _dialogueButton;
        [SerializeField] private Text _dialogueButtonLabel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private EndingUpscalingAnimationController _animation;

        public event Action OnUpscalingRequested;
        public event Action OnDialogueContinueRequested;
        public event Action OnRestartRequested;
        public event Action OnExitRequested;

        private enum PresentationStage { Hidden, Invitation, Process, Reveal, Dialogue, Complete }
        private PresentationStage _presentation;

        private void OnEnable()
        {
            _startButton.onClick.AddListener(HandleUpscaling);
            _dialogueButton.onClick.AddListener(HandleDialogueContinue);
            _restartButton.onClick.AddListener(HandleRestart);
            _exitButton.onClick.AddListener(HandleExit);
            UI.SceneUIStyle.ApplyButtons(_canvas.transform);
            // The animation owns its Awake initialization; another object's Awake may run later.
            _presentation = PresentationStage.Hidden;
            _canvas.enabled = false;
            HideControls();
        }

        public void BindCamera(Camera camera)
        {
            _canvas.worldCamera = camera;
            _animation.BindCamera(camera);
        }

        public void ShowUpscalingInvitation()
        {
            HideControls();
            _presentation = PresentationStage.Invitation;
            SetLayout(new Vector3(0f, .94f, 3.4f), new Vector2(1240f, 360f));
            SetText(_title, "CNN 공장, 다시 가동!", 60, new Vector2(0f, 105f), new Vector2(1140f, 100f));
            SetText(_message, "이제 영상을 선명하게 바꿔 볼까요?", 38, new Vector2(0f, 20f), new Vector2(1140f, 65f));
            _startButton.gameObject.SetActive(true);
            _canvas.enabled = true;
        }

        public void ShowExtraction(float progress)
        {
            BeginProcess();
            _animation.ShowExtraction(progress);
        }

        public void ShowConvolution(float progress)
        {
            BeginProcess();
            _animation.ShowConvolution(progress);
        }

        public void ShowFeatureMaps(float progress)
        {
            BeginProcess();
            _animation.ShowFeatureMaps(progress);
        }

        public void ShowPrediction(float progress)
        {
            BeginProcess();
            _animation.ShowPrediction(progress);
        }

        public void ShowReturn(float progress)
        {
            BeginProcess();
            _animation.ShowReturn(progress);
        }

        private void BeginProcess()
        {
            if (_presentation == PresentationStage.Process)
            {
                return;
            }
            _presentation = PresentationStage.Process;
            HideControls();
            _canvas.enabled = false;
        }

        public void ShowReveal()
        {
            if (_presentation == PresentationStage.Reveal)
            {
                return;
            }
            _presentation = PresentationStage.Reveal;
            _animation.Hide();
            _canvas.enabled = false;
        }

        public void ShowResidentThanks(bool finalPage)
        {
            HideControls();
            _presentation = PresentationStage.Dialogue;
            _animation.Hide();
            SetLayout(new Vector3(.7f, 1.25f, 2.9f), new Vector2(1120f, 420f));
            SetText(_title, "마을 주민", 36, new Vector2(0f, 150f), new Vector2(1000f, 55f));
            _title.alignment = TextAnchor.MiddleLeft;
            _title.color = new Color(.48f, .95f, .85f);
            string line = finalPage
                ? "CNN 공장을 수리해 주셔서\n정말 감사합니다!"
                : "당신 덕분에 마을의 디스플레이가\n다시 선명해졌어요!";
            SetText(_message, line, 44, new Vector2(0f, 25f), new Vector2(1000f, 150f));
            _message.alignment = TextAnchor.MiddleLeft;
            _dialogueButtonLabel.text = finalPage ? "마을 바라보기" : "다음";
            _dialogueButton.gameObject.SetActive(true);
            _canvas.enabled = true;
        }

        public void ShowRestoration()
        {
            _presentation = PresentationStage.Hidden;
            _canvas.enabled = false;
            HideControls();
            _animation.Hide();
        }

        public void ShowCompletion()
        {
            HideControls();
            _presentation = PresentationStage.Complete;
            SetLayout(new Vector3(0f, .85f, 3.4f), new Vector2(1120f, 300f));
            SetText(_title, "선명함을 되찾은 마을", 56, new Vector2(0f, 80f), new Vector2(1040f, 85f));
            SetText(_message, string.Empty, 32, new Vector2(0f, 15f), new Vector2(1040f, 55f));
            _restartButton.gameObject.SetActive(true);
            _exitButton.gameObject.SetActive(true);
            SetNavigationAvailable(true);
            _canvas.enabled = true;
        }

        private void SetLayout(Vector3 position, Vector2 size)
        {
            RectTransform rect = (RectTransform)_canvas.transform;
            rect.localPosition = position;
            rect.sizeDelta = size;
            _backdrop.sizeDelta = size;
        }

        private static void SetText(Text text, string value, int fontSize, Vector2 position, Vector2 size)
        {
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
        }

        private void HideControls()
        {
            _startButton.gameObject.SetActive(false);
            _dialogueButton.gameObject.SetActive(false);
            _restartButton.gameObject.SetActive(false);
            _exitButton.gameObject.SetActive(false);
        }

        public void ShowMessage(string message) => _message.text = message;

        public void SetNavigationAvailable(bool available)
        {
            _restartButton.interactable = available;
            _exitButton.interactable = available;
        }

        private void HandleUpscaling() => OnUpscalingRequested?.Invoke();
        private void HandleDialogueContinue() => OnDialogueContinueRequested?.Invoke();
        private void HandleRestart() => OnRestartRequested?.Invoke();
        private void HandleExit() => OnExitRequested?.Invoke();

        private void OnDisable()
        {
            _startButton.onClick.RemoveListener(HandleUpscaling);
            _dialogueButton.onClick.RemoveListener(HandleDialogueContinue);
            _restartButton.onClick.RemoveListener(HandleRestart);
            _exitButton.onClick.RemoveListener(HandleExit);
        }
    }
}
