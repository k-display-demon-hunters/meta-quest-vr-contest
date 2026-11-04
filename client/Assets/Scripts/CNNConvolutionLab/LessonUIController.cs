using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // Layout is saved in PixelCalculationUI.prefab; this component only presents state.
    public sealed class LessonUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _exerciseRoot;
        [SerializeField] private GameObject _observationUi;
        [SerializeField] private Text _title, _explanation, _equation, _feedback, _channelSums, _guide;
        [SerializeField] private Text _summary;
        [SerializeField] private Text[] _channelProgress;
        [SerializeField] private Image[] _channelProgressBackgrounds;
        [SerializeField] private Button _next, _skip, _return, _restart, _nextObservation, _combine;
        [SerializeField] private GameObject _notebook, _keypadRoot;
        [SerializeField] private Text _keypadValue, _keypadFeedback;
        [SerializeField] private Button _submitAnswer;
        private string _answerInput = "";
        // Display history only; the lesson and quiz decide when a channel is complete.
        private readonly int?[] _completedChannelSums = new int?[3];
        private int _activeChannel = -1;
        private bool _advance;
        public event Action<int> AnswerRequested;
        public event Action SkipRequested;
        public event Action CombineRequested;
        public event Action RestartRequested;
        public event Action AdvanceRequested;
        public event Action CloseRequested;
        public string Title { set => _title.text = value; }
        public string Explanation { set => _explanation.text = value; }
        public string Equation { set => _equation.text = value; }
        public string Feedback { set { _feedback.text = value; _keypadFeedback.text = value; } }
        public string ChannelSums { set => _channelSums.text = value; }
        public string Guide { set => _guide.text = value; }
        public void Initialize(Font font)
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void RequestNext() { _advance = true; }
        public void RequestSkip() { SkipRequested?.Invoke(); }
        public void RequestReturn() { CloseRequested?.Invoke(); }
        public void RequestRestart() { RestartRequested?.Invoke(); }
        public void RequestAdvance() { AdvanceRequested?.Invoke(); }
        public void RequestCombine() { CombineRequested?.Invoke(); }
        public void SetEquationSize(int size) { _equation.fontSize = Mathf.Max(42, size); }
        public void ShowKeypad(bool visible)
        {
            _keypadRoot.SetActive(visible);
            _notebook.SetActive(!visible);
            _equation.gameObject.SetActive(!visible);
            _channelSums.gameObject.SetActive(!visible);
            _answerInput = "";
            RefreshAnswerInput();
        }
        public void AppendAnswerDigit(int digit)
        {
            if (!_keypadRoot.activeSelf) { return; }
            if (_answerInput.Length >= 2)
            {
                Feedback = "두 자리까지 입력돼요.\n←로 지우고 고쳐 주세요.";
                return;
            }
            _answerInput = _answerInput == "0" ? digit.ToString() : _answerInput + digit;
            AnswerInputEdited();
        }
        public void ClearAnswerInput()
        {
            if (!_keypadRoot.activeSelf) { return; }
            _answerInput = "";
            AnswerInputEdited();
        }
        public void DeleteAnswerDigit()
        {
            if (!_keypadRoot.activeSelf || _answerInput.Length == 0) { return; }
            _answerInput = _answerInput.Substring(0, _answerInput.Length - 1);
            AnswerInputEdited();
        }
        public void RequestAnswer()
        {
            if (!_keypadRoot.activeSelf || _answerInput.Length == 0) { return; }
            AnswerRequested?.Invoke(int.Parse(_answerInput));
        }
        private void AnswerInputEdited()
        {
            RefreshAnswerInput();
            Feedback = _answerInput.Length == 0
                ? "숫자를 입력한 뒤\n답 확인을 눌러 주세요."
                : "입력한 값을 확인하고\n답 확인을 눌러 주세요.";
        }
        private void RefreshAnswerInput()
        {
            _keypadValue.text = "답: " + (_answerInput.Length == 0 ? "—" : _answerInput);
            _submitAnswer.interactable = _answerInput.Length > 0;
        }
        public void ShowSkip(bool pendingInput, bool pendingKernel)
        {
            // Quiz answers are never bypassed: this assistance only finishes a physical action.
            _skip.gameObject.SetActive(pendingInput || pendingKernel);
            _skip.GetComponentInChildren<Text>().text = pendingKernel ? "필터 자동 배치" : "입력 자동 배치";
        }
        public void BeginCalculation()
        {
            gameObject.SetActive(true);
            _exerciseRoot.SetActive(true);
            _observationUi.SetActive(false);
            _equation.text = _feedback.text = _guide.text = _channelSums.text = "";
            Array.Clear(_completedChannelSums, 0, _completedChannelSums.Length);
            _activeChannel = -1;
            RefreshChannelProgress();
            ShowKeypad(false);
            _next.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
        }
        public void BeginChannel(int channel)
        {
            _activeChannel = channel;
            RefreshChannelProgress();
        }
        public void CompleteChannel(int channel, int sum)
        {
            _completedChannelSums[channel] = sum;
            _activeChannel = -1;
            RefreshChannelProgress();
        }
        private void RefreshChannelProgress()
        {
            for (int channel = 0; channel < 3; channel++)
            {
                char name = "RGB"[channel];
                if (_completedChannelSums[channel].HasValue)
                {
                    _channelProgress[channel].text = $"{name} 완료 · 합 {_completedChannelSums[channel].Value}";
                    _channelProgress[channel].color = new Color(1, 0.86f, 0.56f);
                    _channelProgressBackgrounds[channel].color = new Color(0.22f, 0.18f, 0.10f);
                }
                else if (channel == _activeChannel)
                {
                    _channelProgress[channel].text = name + " · 진행 중";
                    _channelProgress[channel].color = Color.white;
                    _channelProgressBackgrounds[channel].color = new Color(0.22f, 0.29f, 0.33f);
                }
                else
                {
                    _channelProgress[channel].text = name + " · 대기";
                    _channelProgress[channel].color = new Color(0.63f, 0.68f, 0.70f);
                    _channelProgressBackgrounds[channel].color = new Color(0.09f, 0.12f, 0.14f);
                }
            }
        }
        public void SetReturnAvailable(bool available) { _return.gameObject.SetActive(available); }
        public void ShowCalculation() { _observationUi.SetActive(false); }
        public void ShowCompleted(int[] sums)
        {
            _exerciseRoot.SetActive(false);
            _observationUi.SetActive(true);
            _restart.gameObject.SetActive(false);
            _combine.gameObject.SetActive(false);
            _summary.text = "<size=48><b>합성곱 계산 완료</b></size>\n\n"
                + $"R 합  {sums[0]}     G 합  {sums[1]}     B 합  {sums[2]}\n\n"
                + "세 채널의 합 + 편향\n"
                + $"<size=48><color=#FFDB91>{sums[0]} + {sums[1]} + {sums[2]} + 0 = {sums[0] + sums[1] + sums[2]}</color></size>\n\n"
                + "세 채널의 계산이 하나의 출력값이 돼요.\n이번 연습의 편향은 0이에요.";
            _nextObservation.GetComponentInChildren<Text>().text = "학습 마치기";
        }
        public void SetObservationBusy(bool busy) { _nextObservation.interactable = !busy; }
        public void ShowObservationProgress(bool pending, bool busy) { _nextObservation.interactable = !busy; }
        public void ShowCombined(bool combined) { }
        public IEnumerator WaitForNext(string label)
        {
            _advance = false;
            _next.interactable = true;
            _next.GetComponentInChildren<Text>().text = label;
            _next.gameObject.SetActive(true);
            yield return new WaitUntil(() => _advance);
            _next.gameObject.SetActive(false);
        }
    }
}
