using UnityEngine;
using UnityEngine.UI;

namespace CNNScreen
{
    public sealed class ScreenLabUIController : MonoBehaviour
    {
        [SerializeField] private Button _detailButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _instruction;
        [SerializeField] private AudioSource _voice;
        [SerializeField] private AudioClip[] _guidance;
        private int _requestedVoice = -1;
        private int _playedVoice = -1;
        public void RequestGuidance(int step) { _requestedVoice = Mathf.Max(_requestedVoice, step); }
        private void Update()
        {
            if (_voice.isPlaying || _playedVoice >= _requestedVoice) { return; }
            _playedVoice++;
            _voice.clip = _guidance[_playedVoice];
            _voice.Play();
        }
        public void StopGuidance() { _voice.Stop(); _requestedVoice = _playedVoice = -1; }
        private void OnDisable() { StopGuidance(); }
        public event System.Action DetailsRequested;
        public event System.Action NextRequested;
        public event System.Action ResetRequested;
        public void RequestDetails() { DetailsRequested?.Invoke(); }
        public void RequestNext() { NextRequested?.Invoke(); }
        public void RequestReset() { ResetRequested?.Invoke(); }
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
            StopGuidance();
            Present("전시대 위의 돋보기를 Grip으로 잡아보세요.", false, false);
        }
        public void ShowFilterGrabbed()
        {
            Present("돋보기를 이미지에 가까이 가져가세요.\n한 위치에 잠깐 머물러 보세요.", false, false);
        }
        public void ShowObservationHint()
        {
            Present("같은 위치에 잠깐 머물러 보세요.", false, false);
        }
        public void ShowMoving() { Present("돋보기를 움직여 다른 색도 살펴보세요.", false, false); }
        public void ShowExplorationHint()
        {
            Present("돋보기를 움직이며 RGB 값을 비교해 보세요.", false, true, "관찰 마치기");
        }
        public void ShowExitGuide()
        {
            Present("픽셀 · RGB 관찰을 마쳤어요!\n입구 양피지를 수집하고\n열린 문으로 이동해 합성곱을 배워보세요.", false, false);
        }
    }
}
