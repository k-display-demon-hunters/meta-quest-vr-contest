using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class ScreenLabUIController : MonoBehaviour
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
        public void EnableInput() { _input.Enable(transform.parent); }
        public void DisableInput() { _input.Dispose(); }
        public void Initialize()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void SetVisible(bool visible) { gameObject.SetActive(visible); }
        private void Present(string message, bool details, bool next, string nextLabel = "다음")
        {
            _instruction.text = message;
            _detailButton.gameObject.SetActive(details);
            _detailButton.interactable = details;
            _nextButton.gameObject.SetActive(next);
            _nextButton.interactable = next;
            _detailButton.GetComponentInChildren<Text>().text = "픽셀 확대하기";
            _nextButton.GetComponentInChildren<Text>().text = nextLabel;
        }
        public void ResetControls()
        {
            Present("푸른 원 안의 돋보기를 찾아보세요.\nGrip으로 잡아 이미지 위에 놓으세요.", false, false);
        }
        public void ShowFilterGrabbed()
        {
            Present("돋보기를 이미지에 가까이 가져가세요.\n원하는 곳에서 Grip을 놓으세요.", false, false);
        }
        public void ShowPixelPage(int page)
        {
            if (page == 0) { Present("픽셀  1 / 2\n이미지는 작은 색 조각으로 이루어져요.\n이 조각 하나를 ‘픽셀’이라고 해요.", false, true); }
            else if (page == 1) { Present("픽셀  2 / 2\n돋보기 안에 보이는 3 × 3칸,\n모두 9개의 픽셀을 골랐어요.", false, true); }
            else { Present("픽셀 안에는 어떤 값이 있을까요?\n[픽셀 확대하기]를 눌러 살펴보세요.", true, false); }
        }
        public void ShowMoving() { Present("선택한 픽셀을 펼치고 있어요.\n잠시만 기다려 주세요.", false, false); }
        public void ShowRgbPage(int page)
        {
            if (page == 0) { Present("RGB  1 / 3\nR은 빨강, G는 초록, B는 파랑이에요.\n세 값을 섞어 한 픽셀의 색을 만들어요.", false, true); }
            else if (page == 1) { Present("RGB  2 / 3\n숫자는 각 색의 세기예요.\n0은 없음, 255는 가장 강함이에요.", false, true); }
            else { Present("RGB  3 / 3\n앞뒤의 같은 칸은 같은 픽셀이에요.\n이제 작은 숫자로 계산을 배워봐요.", false, true, "관찰 마치기"); }
        }
        public void ShowExitGuide()
        {
            Present("픽셀 · RGB 관찰을 마쳤어요!\n입구 양피지를 수집하고\n열린 문으로 이동해 합성곱을 배워보세요.", false, false);
        }
    }
}
