using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // Layout is saved in PixelCalculationUI.prefab; this component only presents state.
    public sealed class LessonUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _exerciseRoot;
        [SerializeField] private GameObject _observationUi;
        [SerializeField] private Text _title, _explanation, _equation, _feedback, _channelSums, _guide;
        [SerializeField] private Text _summary;
        [SerializeField] private Button _next, _skip, _return, _restart, _nextObservation, _combine;
        [SerializeField] private Button[] _answers;
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
        public string Feedback { set => _feedback.text = value; }
        public string ChannelSums { set => _channelSums.text = value; }
        public string Guide { set => _guide.text = value; }
        public void Initialize(Font font)
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void RequestNext() { _advance = true; }
        public void RequestSkip() { SkipRequested?.Invoke(); }
        public void RequestAnswer(int option) { AnswerRequested?.Invoke(option); }
        public void RequestReturn() { CloseRequested?.Invoke(); }
        public void RequestRestart() { RestartRequested?.Invoke(); }
        public void RequestAdvance() { AdvanceRequested?.Invoke(); }
        public void RequestCombine() { CombineRequested?.Invoke(); }
        public void SetEquationSize(int size) { _equation.fontSize = Mathf.Max(42, size); }
        public void SetChoices(int[] choices)
        {
            for (int i = 0; i < _answers.Length; i++)
            {
                _answers[i].GetComponentInChildren<Text>().text = choices[i].ToString();
                _answers[i].interactable = true;
            }
        }
        public void SetAnswersEnabled(bool enabled)
        {
            foreach (Button answer in _answers) { answer.interactable = enabled; }
        }
        public void ShowAnswers(bool visible)
        {
            foreach (Button answer in _answers) { answer.gameObject.SetActive(visible); }
        }
        public void ShowSkip(bool physical, bool waiting)
        {
            // Quiz answers are never bypassed: this assistance only finishes a physical action.
            _skip.gameObject.SetActive(physical);
            _skip.GetComponentInChildren<Text>().text = "자동 배치";
        }
        public void BeginCalculation()
        {
            gameObject.SetActive(true);
            _exerciseRoot.SetActive(true);
            _observationUi.SetActive(false);
            _equation.text = _feedback.text = _guide.text = _channelSums.text = "";
            ShowAnswers(false);
            _next.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
        }
        public void SetReturnAvailable(bool available) { _return.gameObject.SetActive(available); }
        public void SetCompletionDestination(bool factoryHosted)
        {
            _nextObservation.GetComponentInChildren<Text>().text = factoryHosted ? "학습 마치기" : "다른 영역 보기";
        }
        public void ShowCalculation() { _observationUi.SetActive(false); }
        public void ShowCompleted(int[] sums)
        {
            _exerciseRoot.SetActive(false);
            _observationUi.SetActive(true);
            _restart.gameObject.SetActive(false);
            _combine.gameObject.SetActive(false);
            _summary.text = $"B 퀴즈 완료!\n\nR = {sums[0]}    G = {sums[1]}    B = {sums[2]}\n\n세 색의 합을 더하면 {sums[0] + sums[1] + sums[2]}\n(이번 연습의 편향은 0이에요.)";
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
