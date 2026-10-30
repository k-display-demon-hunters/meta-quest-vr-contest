using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class PixelLabUIController : MonoBehaviour
    {
        [SerializeField] private Button _detailButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _instruction;

        private readonly PixelUIInputController _input = new PixelUIInputController();
        public event System.Action DetailsRequested;
        public event System.Action NextRequested;
        public event System.Action ResetRequested;

        public void RequestDetails() { DetailsRequested?.Invoke(); }
        public void RequestNext() { NextRequested?.Invoke(); }
        public void RequestReset() { ResetRequested?.Invoke(); }
        public void EnableInput() { _input.Enable(transform); }
        public void DisableInput() { _input.Dispose(); }

        public void Initialize()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void ShowRevealPhase(PixelObservationPhase phase)
        {
            if (phase == PixelObservationPhase.Revealing)
            {
                _instruction.gameObject.SetActive(true);
                _instruction.text = "이미지가 픽셀과 숫자로 바뀌고 있어요";
            }
            else if (phase == PixelObservationPhase.Unfolding)
            {
                _instruction.text = "선택한 3×3 픽셀에서 R·G·B가 앞뒤로 펼쳐져요";
            }
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

    }
}
