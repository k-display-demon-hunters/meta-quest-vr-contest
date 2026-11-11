using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // Layout is saved in PixelCalculationUI.prefab; this component only presents state.
    public sealed class LessonUIController : MonoBehaviour
    {
        private const float ActionGlowPeriod = 1.4f;
        [SerializeField] private GameObject _exerciseRoot;
        [SerializeField] private GameObject _exerciseControls;
        [SerializeField] private Text _title, _explanation;
        [SerializeField] private Text _guideInstruction, _playbackLabel;
        [SerializeField] private Text _stageProgress;
        [SerializeField] private Text[] _channelProgress;
        [SerializeField] private Image[] _channelProgressBackgrounds;
        [SerializeField] private Button _next, _skip, _finish, _replay;
        [SerializeField] private Image _actionGlow;
        // Display history only; the lesson and quiz decide when a channel is complete.
        private readonly int?[] _completedChannelSums = new int?[3];
        private int _activeChannel = -1;
        private float _actionGlowStartTime;
        public event Action NextRequested;
        public event Action SkipRequested;
        public event Action FinishRequested;
        public event Action PlaybackToggleRequested;
        public event Action RestartRequested;
        public event Action ReplayRequested;
        public string Title { set => _title.text = value; }
        public string Explanation { set => _explanation.text = value; }
        public string Instruction { set => _guideInstruction.text = value; }

        private void LateUpdate()
        {
            bool showGlow = (_next.isActiveAndEnabled && _next.IsInteractable())
                || (_finish.isActiveAndEnabled && _finish.IsInteractable());
            if (_actionGlow.gameObject.activeSelf != showGlow)
            {
                _actionGlow.gameObject.SetActive(showGlow);
                _actionGlowStartTime = Time.unscaledTime;
            }
            if (!showGlow) { return; }

            float cycle = (Time.unscaledTime - _actionGlowStartTime) / ActionGlowPeriod;
            float pulse = (Mathf.Cos(cycle * 2f * Mathf.PI) + 1f) * 0.5f;
            _actionGlow.canvasRenderer.SetAlpha(Mathf.Lerp(0.2f, 1f, pulse));
        }

        public void Initialize()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true)) { canvas.worldCamera = Camera.main; }
        }
        public void RequestNext() { NextRequested?.Invoke(); }
        public void RequestSkip() { SkipRequested?.Invoke(); }
        public void RequestFinish() { FinishRequested?.Invoke(); }
        public void RequestPlaybackToggle() { PlaybackToggleRequested?.Invoke(); }
        public void RequestRestart() { RestartRequested?.Invoke(); }
        public void RequestReplay() { ReplayRequested?.Invoke(); }

        public void SetPaused(bool paused)
        {
            _playbackLabel.text = paused ? "체험 재개" : "체험 멈추기";
            _skip.interactable = !paused;
            _next.interactable = !paused;
            _replay.interactable = !paused;
        }

        public void ShowSkip(bool pendingKernel, string label = "필터 자동 배치")
        {
            // Quiz answers are never bypassed: this assistance only finishes a physical action.
            _skip.GetComponentInChildren<Text>(true).text = label;
            _skip.gameObject.SetActive(pendingKernel);
        }
        public void ShowReplay(bool visible) => _replay.gameObject.SetActive(visible);

        public void SetStage(int active)
        {
            string[] labels = { "① 배치", "② 곱하기", "③ 더하기", "④ 출력" };
            for (int index = 0; index < labels.Length; index++)
            {
                if (index == active) { labels[index] = "<color=#FFD582><b>" + labels[index] + "</b></color>"; }
            }
            _stageProgress.text = string.Join("   →   ", labels);
        }
        public void BeginCalculation()
        {
            gameObject.SetActive(true);
            _exerciseRoot.SetActive(true);
            _exerciseControls.SetActive(true);
            Array.Clear(_completedChannelSums, 0, _completedChannelSums.Length);
            _activeChannel = -1;
            RefreshChannelProgress();
            _next.gameObject.SetActive(false);
            _skip.gameObject.SetActive(false);
            _finish.gameObject.SetActive(false);
            _replay.gameObject.SetActive(false);
            SetStage(0);
        }
        public void BeginChannel(int channel)
        {
            _completedChannelSums[channel] = null;
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
            _exerciseRoot.SetActive(true);
            _exerciseControls.SetActive(false);
            Instruction = "‘체험 마치기’를 눌러 다음 체험으로 이동하세요.";
            _finish.interactable = true;
            _finish.gameObject.SetActive(true);
            Title = "5×5 RGB 입력에서 3×3 출력이 완성됐어요";
            Explanation = $"첫 위치: <color=#FF666B>R {firstSums[0]}</color> + <color=#69EDA6>G {firstSums[1]}</color> + <color=#61ADFF>B {firstSums[2]}</color> = {outputs[0]}\n같은 필터를 한 칸씩 옮겨 아홉 위치를 계산했어요.";
            ResetChannelProgress();
            for (int channel = 0; channel < firstSums.Length; channel++) { CompleteChannel(channel, firstSums[channel]); }
        }
        public void DisableFinish()
        {
            _finish.interactable = false;
            Instruction = "체험을 마쳤어요. 다음 체험으로 이동할 수 있어요.";
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
