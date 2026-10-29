using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class PixelLabUIController
    {
        private readonly Button _detailButton;
        private readonly Button _nextButton;
        private readonly Text _instruction;
        private readonly GameObject _controls;
        private bool _controlsWereActive;
        private bool _backgroundHidden;

        public PixelLabUIController(Button detailButton, Button nextButton, Font font)
        {
            _detailButton = detailButton;
            _nextButton = nextButton;
            SceneUIStyle.Apply(_detailButton);
            SceneUIStyle.Apply(_nextButton);
            foreach (Button button in detailButton.transform.parent.GetComponentsInChildren<Button>())
            {
                RectTransform rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(button == nextButton ? 380 : 320, 100);
                float x = button == detailButton || button == nextButton ? -210 : 210;
                rect.anchoredPosition = new Vector2(x, 20);
                Text label = button.GetComponentInChildren<Text>();
                label.fontSize = button == nextButton ? 48 : 34;
                label.resizeTextForBestFit = false;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(10, 5);
                label.rectTransform.offsetMax = new Vector2(-10, -5);
                if (button != detailButton && button != nextButton) { label.text = "처음으로"; }
            }
            _controls = detailButton.transform.parent.gameObject;
            RectTransform hint = new GameObject("Interaction hint", typeof(RectTransform)).GetComponent<RectTransform>();
            hint.SetParent(_detailButton.transform.parent, false);
            hint.anchorMin = hint.anchorMax = hint.pivot = Vector2.one * 0.5f;
            hint.anchoredPosition = new Vector2(0, -105);
            hint.sizeDelta = new Vector2(1400, 90);
            _instruction = hint.gameObject.AddComponent<Text>();
            _instruction.font = font;
            _instruction.fontSize = 32;
            _instruction.alignment = TextAnchor.MiddleCenter;
            _instruction.color = Color.white;
            _instruction.raycastTarget = false;
        }

        public void SetSelectionAvailable(bool available)
        {
            _detailButton.interactable = available;
        }

        public void ShowFilterGrabbed()
        {
            _instruction.text = "오른손 레이로 필터를 옮기고 원하는 부분에서 Grip을 놓으세요";
            _detailButton.interactable = false;
        }

        public void ShowRevealStarted()
        {
            _instruction.gameObject.SetActive(false);
            _detailButton.gameObject.SetActive(false);
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = false;
            _nextButton.GetComponentInChildren<Text>().text = "다음으로";
        }

        public void ResetControls()
        {
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(false);
                _nextButton.interactable = false;
                _nextButton.GetComponentInChildren<Text>().text = "다음으로";
            }
            _detailButton.interactable = false;
            _detailButton.gameObject.SetActive(true);
            _detailButton.GetComponentInChildren<Text>().text = "자세히 보기";
        }

        public void ShowPixelsRevealed(bool hasSelection)
        {
            _detailButton.gameObject.SetActive(false);
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            Text lessonLabel = _nextButton.GetComponentInChildren<Text>();
            lessonLabel.text = "다음으로";
            lessonLabel.fontSize = 48;
        }

        public void UpdateInstruction(bool readyForConvolution, bool hasSelection)
        {
            _instruction.gameObject.SetActive(true);
            if (readyForConvolution)
            {
                _instruction.text = "같은 픽셀의 R·G·B가 앞뒤로 펼쳐졌어요. 옆으로 움직여 세 층을 관찰하세요.\n숫자는 색의 세기(0~255)예요. ‘다음으로’에서 이 값으로 합성곱을 계산해요.";
                return;
            }
            _instruction.text = hasSelection
                ? "‘자세히 보기’를 누르면 선택한 픽셀이 입체적인 RGB 층으로 펼쳐져요."
                : "필터를 찾아 이미지를 관찰하세요!\n주변을 둘러보고 필터를 Grip으로 잡아 이미지 위에 놓으세요.";
        }

        public void SetBackgroundHidden(bool hidden)
        {
            if (_backgroundHidden == hidden) { return; }
            if (hidden)
            {
                _controlsWereActive = _controls.activeSelf;
            }
            _controls.SetActive(hidden ? false : _controlsWereActive);
            _backgroundHidden = hidden;
        }
    }
}
