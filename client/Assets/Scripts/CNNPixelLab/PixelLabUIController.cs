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
            _background = new[] { board, filter, detailButton.transform.parent.gameObject };
            RectTransform hint = new GameObject("Interaction hint", typeof(RectTransform)).GetComponent<RectTransform>();
            hint.SetParent(_detailButton.transform.parent, false);
            hint.anchorMin = hint.anchorMax = hint.pivot = Vector2.one * 0.5f;
            hint.anchoredPosition = new Vector2(0, -70);
            hint.sizeDelta = new Vector2(1200, 70);
            _instruction = hint.gameObject.AddComponent<Text>();
            _instruction.font = font;
            _instruction.fontSize = 26;
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
            _instruction.text = "시선으로 필터를 옮기고 원하는 부분에서 Grip을 놓으세요";
            _detailButton.interactable = false;
        }

        public void ShowRevealStarted()
        {
            _instruction.gameObject.SetActive(false);
            _detailButton.interactable = false;
            _nextButton.interactable = false;
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
            lessonLabel.text = "합성곱 연산\n원리 알아보기";
            lessonLabel.fontSize = 24;
        }

        public void UpdateInstruction(bool readyForConvolution, bool hasSelection)
        {
            _instruction.gameObject.SetActive(true);
            if (readyForConvolution)
            {
                _instruction.text = "R/G/B를 골라 픽셀 값을 살펴보세요\n관찰을 마치면 ‘합성곱 연산 원리 알아보기’를 누르세요";
                return;
            }
            _instruction.text = hasSelection
                ? "‘더 자세히 보기’를 눌러 RGB 값을 살펴보세요"
                : "필터를 잡아 이미지 위에 놓으세요\n오른손: Grip을 누른 채 시선으로 이동 · 클릭: Trigger";
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
