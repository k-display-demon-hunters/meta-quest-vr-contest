using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RealityLab
{
    public sealed class RealityResultController : MonoBehaviour
    {
        [SerializeField] private RawImage _currentImage;
        [SerializeField] private RawImage _previousImage;
        [SerializeField] private Text _currentCaption;
        [SerializeField] private Text _previousCaption;
        [SerializeField] private Text _prediction;
        [SerializeField] private Text _comparison;
        [SerializeField] private Text[] _scores;
        [SerializeField] private Image[] _bars;
        [SerializeField] private Text _summary;
        [SerializeField] private Text _historyPosition;
        [SerializeField] private Button _previousRecord;
        [SerializeField] private Button _nextRecord;
        private Texture2D _currentTexture;
        private Texture2D _previousTexture;
        private IReadOnlyList<RealityAttempt> _history;
        private int _historyIndex;

        public void ShowInput(RealityRecognitionData input, RealityAttempt previous)
        {
            SetImage(_currentImage, ref _currentTexture, input);
            SetImage(_previousImage, ref _previousTexture, previous?.Input);
            _currentCaption.text = input == null ? "촬영 전" : "실제 모델 입력 · 28×28 RGB";
            _previousCaption.text = previous == null ? "이전 촬영 없음" :
                $"직전 촬영 · 실제 {previous.Actual} / 예측 {previous.Predicted}";
            _comparison.text = previous == null ? "숫자 하나를 프레임에 맞춰 촬영하세요." :
                "같은 숫자를 촬영하세요. 거리나 각도 중 하나만 바꾸어 비교합니다.";
            _prediction.text = "—";
            for (int i = 0; i < _scores.Length; i++)
            {
                _scores[i].text = "—";
                _bars[i].fillAmount = 0;
            }
        }

        public void ShowPrediction(IReadOnlyList<double> scores)
        {
            int best = 0;
            for (int i = 0; i < scores.Count; i++)
            {
                _scores[i].text = $"{scores[i] * 100:0.0}%";
                _bars[i].fillAmount = (float)scores[i];
                if (scores[i] > scores[best]) { best = i; }
            }
            _prediction.text = best.ToString();
            _comparison.text = "실제 숫자를 아래에서 선택해 기록하세요. 선택은 학습에 사용되지 않습니다.";
        }

        public void ShowRecorded(RealityAttempt current, RealityAttempt previous)
        {
            string outcome = current.IsCorrect ? "일치" : "불일치";
            _currentCaption.text = $"실제 {current.Actual} / 예측 {current.Predicted} · {outcome}";
            _comparison.text = previous == null
                ? "기록 완료. 같은 숫자를 다시 촬영하거나 다른 숫자를 선택하세요."
                : $"직전 예측 {previous.Predicted} → 이번 예측 {current.Predicted}\n모델은 그대로이며 촬영한 입력만 달라졌습니다.";
        }

        public void ShowHistory(IReadOnlyList<RealityAttempt> history)
        {
            _history = history;
            _historyIndex = history.Count - 1;
            int first = 0, firstCorrect = 0, retry = 0, retryCorrect = 0;
            foreach (RealityAttempt item in history)
            {
                if (item.Attempt == 1) { first++; if (item.IsCorrect) { firstCorrect++; } }
                else { retry++; if (item.IsCorrect) { retryCorrect++; } }
            }
            _summary.text = $"첫 촬영: {firstCorrect}/{first}회 일치   ·   재촬영: {retryCorrect}/{retry}회 일치\n"
                + "이번 체험의 기록이며 모델 전체의 정확도를 뜻하지 않습니다.";
            ShowHistoryItem();
        }

        public void PreviousRecord() { if (_historyIndex > 0) { _historyIndex--; ShowHistoryItem(); } }
        public void NextRecord() { if (_historyIndex + 1 < _history.Count) { _historyIndex++; ShowHistoryItem(); } }

        private void ShowHistoryItem()
        {
            _previousRecord.interactable = _historyIndex > 0;
            _nextRecord.interactable = _historyIndex >= 0 && _historyIndex + 1 < _history.Count;
            if (_historyIndex < 0)
            {
                ShowInput(null, null);
                _historyPosition.text = "기록한 예측이 없습니다.";
                return;
            }
            RealityAttempt item = _history[_historyIndex];
            RealityAttempt previous = _historyIndex > 0 && _history[_historyIndex - 1].Sample == item.Sample
                ? _history[_historyIndex - 1] : null;
            ShowInput(item.Input, previous);
            ShowPrediction(item.Scores);
            ShowRecorded(item, previous);
            _historyPosition.text = $"기록 {_historyIndex + 1}/{_history.Count} · 숫자 표본 {item.Sample} · {item.Attempt}번째 촬영";
        }

        private static void SetImage(RawImage view, ref Texture2D texture, RealityRecognitionData input)
        {
            if (texture != null) { Destroy(texture); texture = null; }
            view.texture = null;
            view.enabled = input != null;
            if (input == null) { return; }
            texture = new Texture2D(input.Width, input.Height, TextureFormat.RGB24, false, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] colors = new Color32[input.Width * input.Height];
            for (int y = 0; y < input.Height; y++)
            for (int x = 0; x < input.Width; x++)
            {
                int source = (y * input.Width + x) * input.Channels;
                colors[(input.Height - 1 - y) * input.Width + x] = new Color(
                    input.Pixels[source], input.Pixels[source + 1], input.Pixels[source + 2], 1);
            }
            texture.SetPixels32(colors);
            texture.Apply();
            view.texture = texture;
        }

        private void OnDestroy()
        {
            if (_currentTexture != null) { Destroy(_currentTexture); }
            if (_previousTexture != null) { Destroy(_previousTexture); }
        }
    }
}
