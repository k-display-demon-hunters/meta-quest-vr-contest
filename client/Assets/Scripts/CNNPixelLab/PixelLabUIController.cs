using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class PixelLabUIController
    {
        private readonly Button _detailButton;
        private readonly Button _nextButton;
        private readonly Text _instruction;
        private readonly GameObject[] _background;
        private readonly bool[] _backgroundStates = new bool[3];
        private bool _backgroundHidden;

        public PixelLabUIController(Button detailButton, Button nextButton, Font font,
            GameObject board, GameObject filter)
        {
            _detailButton = detailButton;
            _nextButton = nextButton;
            SceneUIStyle.Apply(_detailButton);
            SceneUIStyle.Apply(_nextButton);
            foreach (Button button in detailButton.transform.parent.GetComponentsInChildren<Button>())
            {
                RectTransform rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(button == nextButton ? 380 : 320, 100);
                float x = button == detailButton ? -390 : button == nextButton ? 0 : 390;
                rect.anchoredPosition = new Vector2(x, 20);
                Text label = button.GetComponentInChildren<Text>();
                label.fontSize = button == nextButton ? 48 : 34;
                label.resizeTextForBestFit = false;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(10, 5);
                label.rectTransform.offsetMax = new Vector2(-10, -5);
            }
            _background = new[] { board, filter, detailButton.transform.parent.gameObject };
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
            _detailButton.interactable = false;
            _nextButton.interactable = true;
            _nextButton.GetComponentInChildren<Text>().text = "다음으로";
        }

        public void ResetControls()
        {
            if (_nextButton != null)
            {
                _nextButton.interactable = true;
                _nextButton.GetComponentInChildren<Text>().text = "다음으로";
            }
            _detailButton.interactable = false;
            _detailButton.GetComponentInChildren<Text>().text = "더 자세히 보기";
        }

        public void ShowPixelsRevealed(bool hasSelection)
        {
            _detailButton.interactable = hasSelection;
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
                _instruction.text = "R/G/B를 골라 픽셀 값을 살펴보세요\n가운데 ‘다음으로’를 누르면 합성곱 연산 원리를 알아봅니다";
                return;
            }
            _instruction.text = hasSelection
                ? "‘더 자세히 보기’를 눌러 RGB 값을 살펴보세요"
                : "필터를 잡아 이미지 위에 놓으세요\n오른손: Grip을 누른 채 레이로 이동 · 클릭: Trigger";
        }

        public void SetBackgroundHidden(bool hidden)
        {
            if (_backgroundHidden == hidden) { return; }
            for (int i = 0; i < _background.Length; i++)
            {
                if (hidden) { _backgroundStates[i] = _background[i].activeSelf; }
                _background[i].SetActive(hidden ? false : _backgroundStates[i]);
            }
            _backgroundHidden = hidden;
        }
    }
}
