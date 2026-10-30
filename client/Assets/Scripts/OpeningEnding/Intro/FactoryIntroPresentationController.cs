using System;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

namespace OpeningEnding.Intro
{
    public sealed class FactoryIntroPresentationController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _background;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _instruction;
        [SerializeField] private Text _continueLabel;
        [SerializeField] private Text _inputHint;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Font _titleFont;
        [SerializeField] private Sprite _titleButtonSprite;
        private Font _bodyFont;
        private Sprite _buttonSprite;

        private Camera _camera;
        private bool _warning;
        private bool _entryGuide;
        private static readonly Color PanelColor = new Color(0.025f, 0.065f, 0.09f, 0.9f);
        public event Action OnContinueRequested;

        private void OnEnable() => _continueButton.onClick.AddListener(RequestContinue);
        private void RequestContinue() => OnContinueRequested?.Invoke();

        public void BindPlayer(XROrigin rig)
        {
            _camera = rig.Camera;
            _canvas.worldCamera = _camera;
            SceneUIStyle.ApplyButtons(_canvas.transform);
            _bodyFont = _heading.font;
            _buttonSprite = ((Image)_continueButton.targetGraphic).sprite;
        }

        public void Show(string heading, string instruction, string button, bool canContinue)
        {
            _warning = false;
            _entryGuide = false;
            _background.color = PanelColor;
            _heading.text = heading;
            _instruction.text = instruction;
            _continueLabel.text = button;
            bool hasButton = !string.IsNullOrEmpty(button);
            _continueButton.gameObject.SetActive(hasButton);
            _continueButton.interactable = canContinue;
            _inputHint.gameObject.SetActive(hasButton);
            _inputHint.text = "A 버튼으로 계속";
            SetCompactLayout(!hasButton);
            _canvas.enabled = true;
        }

        public void ShowWarning()
        {
            Show("CNN 공장에 이상 발생!",
                "악령이 공장을 공격했어요!\n마을의 화면이 망가지고 있어요.", "", false);
            _warning = true;
        }

        public void ShowTitle()
        {
            Show("<size=66>CNN 체험으로 배우는</size>\n<size=104>AI 업스케일링</size>", "", "GAME START", true);
            _background.color = Color.clear;
            _heading.rectTransform.anchoredPosition = new Vector2(0f, 45f);
            _heading.rectTransform.sizeDelta = new Vector2(1400f, 250f);
            _heading.font = _titleFont;
            _heading.supportRichText = true;
            _heading.color = new Color(1f, 0.94f, 0.72f, 1f);
            _continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(900f, 150f);
            _continueLabel.rectTransform.sizeDelta = new Vector2(868f, 138f);
            _continueLabel.font = _titleFont;
            _continueLabel.fontSize = 90;
            Image buttonImage = (Image)_continueButton.targetGraphic;
            buttonImage.sprite = _titleButtonSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.pixelsPerUnitMultiplier = 0.3f;
            _inputHint.gameObject.SetActive(false);
        }

        public void EndTitle()
        {
            _continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 100f);
            _continueLabel.rectTransform.sizeDelta = new Vector2(568f, 88f);
            _heading.color = Color.white;
            _heading.font = _bodyFont;
            _heading.rectTransform.sizeDelta = new Vector2(1000f, 100f);
            _continueLabel.font = _bodyFont;
            Image buttonImage = (Image)_continueButton.targetGraphic;
            buttonImage.sprite = _buttonSprite;
            buttonImage.type = Image.Type.Simple;
            buttonImage.pixelsPerUnitMultiplier = 1f;
            _continueLabel.fontSize = 46;
            BeginCinematic();
        }

        public void ShowEntryGuide()
        {
            Show("공장 안으로 이동하세요",
                "<b>왼쪽 조이스틱을 앞으로 밀어</b>\n빛나는 길을 따라가세요.", "", false);
            _entryGuide = true;
        }

        private void SetCompactLayout(bool compact)
        {
            _background.rectTransform.sizeDelta = new Vector2(1080f, compact ? 310f : 560f);
            _heading.rectTransform.anchoredPosition = new Vector2(0f, compact ? 85f : 195f);
            _instruction.rectTransform.anchoredPosition = new Vector2(0f, compact ? -30f : 35f);
        }

        private void LateUpdate()
        {
            if (_warning)
            {
                // Slow color breathing keeps the warning readable during the attack.
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
                _background.color = Color.Lerp(new Color(0.2f, 0.025f, 0.035f, 0.93f),
                    new Color(0.38f, 0.035f, 0.045f, 0.95f), pulse);
            }
            if (_entryGuide)
            {
                Vector3 forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up);
                // Looking straight up/down has no horizontal heading; retain the guide's last heading.
                forward = forward.sqrMagnitude > 0.001f ? forward.normalized : _canvas.transform.forward;
                _canvas.transform.SetPositionAndRotation(
                    _camera.transform.position + forward * 2.4f - Vector3.up * 0.48f,
                    Quaternion.LookRotation(forward, Vector3.up));
            }
        }

        public void BeginCinematic()
        {
            _warning = false;
            _entryGuide = false;
            _continueButton.interactable = false;
            _canvas.enabled = false;
        }

        public void ReleasePlayer()
        {
            _entryGuide = false;
            _canvas.worldCamera = null;
        }

        private void OnDisable()
        {
            _continueButton.onClick.RemoveListener(RequestContinue);
            ReleasePlayer();
        }
    }
}
