using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // Layout is saved in PixelCalculationUI.prefab; this component only presents state.
    public sealed class LessonUIController : MonoBehaviour
    {
        [SerializeField] private GameObject _exerciseRoot;
        [SerializeField] private GameObject _observationUi;
        [SerializeField] private Text _title, _explanation, _feedback, _guide;
        [SerializeField] private Text _summary;
        [SerializeField] private Text[] _summaryOutputs;
        [SerializeField] private Text _summaryFormula;
        [SerializeField] private Text[] _channelProgress;
        [SerializeField] private Image[] _channelProgressBackgrounds;
        [SerializeField] private Button _next, _skip, _finish;
        [SerializeField] private GameObject _notebook;
        // Display history only; the lesson and quiz decide when a channel is complete.
        private readonly int?[] _completedChannelSums = new int?[3];
        private int _activeChannel = -1;
        public event Action NextRequested;
        public event Action SkipRequested;
        public event Action FinishRequested;
        public string Title { set => _title.text = value; }
        public string Explanation { set => _explanation.text = value; }
        public string Feedback { set => _feedback.text = value; }
        public string Guide { set => _guide.text = value; }
        public void Initialize()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void RequestNext() { NextRequested?.Invoke(); }
        public void RequestSkip() { SkipRequested?.Invoke(); }
        public void RequestFinish() { FinishRequested?.Invoke(); }
        public void ShowSkip(bool pendingKernel)
        {
            // Quiz answers are never bypassed: this assistance only finishes a physical action.
            // The fixed label is already saved in PixelCalculationUI.prefab.
            _skip.gameObject.SetActive(pendingKernel);
        }
        public void BeginCalculation()
        {
            gameObject.SetActive(true);
            _exerciseRoot.SetActive(true);
            _observationUi.SetActive(false);
            _feedback.text = _guide.text = "";
            Array.Clear(_completedChannelSums, 0, _completedChannelSums.Length);
            _activeChannel = -1;
            RefreshChannelProgress();
            _notebook.SetActive(true);
            _next.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
        }
        public void BeginChannel(int channel)
        {
            _activeChannel = channel;
            RefreshChannelProgress();
        }
        public void ResetChannelProgress()
        {
            Array.Clear(_completedChannelSums, 0, _completedChannelSums.Length);
            _activeChannel = -1;
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
                    _channelProgress[channel].color = WorkbenchColors.Channel(channel);
                    _channelProgressBackgrounds[channel].color = new Color(0.08f, 0.12f, 0.14f);
                }
                else if (channel == _activeChannel)
                {
                    _channelProgress[channel].text = name + " · 진행 중";
                    _channelProgress[channel].color = WorkbenchColors.Channel(channel);
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
        public void ShowOutputSummary(int[] outputs, int[] firstSums)
        {
            _exerciseRoot.SetActive(false);
            _observationUi.SetActive(true);
            _finish.interactable = true;
            _finish.gameObject.SetActive(true);
            _summary.text = "<size=48><b>필터가 지나간 자리</b></size>\n"
                + "입력판 위를 이동하며 출력값 9개를 만들었어요.\n"
                + "체험 마치기를 누르면 다음 체험으로 이동할 수 있어요.";
            for (int index = 0; index < outputs.Length; index++)
            {
                _summaryOutputs[index].text = outputs[index].ToString();
            }
            _summaryFormula.text = $"<color=#FF666B>R {firstSums[0]}</color> + <color=#69EDA6>G {firstSums[1]}</color> + <color=#61ADFF>B {firstSums[2]}</color> = {outputs[0]}";
        }
        public void DisableFinish()
        {
            _finish.interactable = false;
        }
        public void ShowNext(string label)
        {
            _next.interactable = true;
            _next.gameObject.SetActive(true);
            _next.GetComponentInChildren<Text>().text = label;
        }
        public void HideNext()
        {
            _next.gameObject.SetActive(false);
        }
    }
}
