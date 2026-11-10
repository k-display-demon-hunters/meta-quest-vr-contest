using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RealityLab
{
    public sealed class RealityUIManager : MonoBehaviour
    {
        [SerializeField] private RealityResultController _results;
        [SerializeField] private RealityViewController _view;
        [SerializeField] private Text _cameraStatus;
        [SerializeField] private Text _modelStatus;
        [SerializeField] private Text _guideStatus;
        [SerializeField] private Text _stageText;
        [SerializeField] private Text _message;
        [SerializeField] private Button _apply;
        [SerializeField] private Button _capture;
        [SerializeField] private Button _recognize;
        [SerializeField] private Button _retake;
        [SerializeField] private Button _same;
        [SerializeField] private Button _next;
        [SerializeField] private Button _finish;
        [SerializeField] private Button[] _labels;
        [SerializeField] private GameObject _historyControls;
        [SerializeField] private GameObject _interactionControls;
        [SerializeField] private GameObject _summaryPanel;
        private RealityStage _stage;
        private bool _modelReady;
        private bool _cameraReady;
        private bool _recorded;
        private bool _previewOnly;

        public event Action ApplyRequested;
        public event Action CaptureRequested;
        public event Action RecognizeRequested;
        public event Action RetakeRequested;
        public event Action SameDigitRequested;
        public event Action NextDigitRequested;
        public event Action SummaryRequested;
        public event Action ResumeRequested;
        public event Action<int> LabelRequested;

        private void Awake() { ShowStage(RealityStage.AwaitingModel); }
        public void RequestApply() => ApplyRequested?.Invoke();
        public void RequestCapture() => CaptureRequested?.Invoke();
        public void RequestRecognize() => RecognizeRequested?.Invoke();
        public void RequestRetake() => RetakeRequested?.Invoke();
        public void RequestSameDigit() => SameDigitRequested?.Invoke();
        public void RequestNextDigit() => NextDigitRequested?.Invoke();
        public void RequestSummary() => SummaryRequested?.Invoke();
        public void RequestResume() => ResumeRequested?.Invoke();
        public void RequestLabel(int digit) => LabelRequested?.Invoke(digit);
        public void PreviousRecord() => _results.PreviousRecord();
        public void NextRecord() => _results.NextRecord();

        public void EnableCapturePreview()
        {
            _previewOnly = true;
        }

        public void SetAvailability(bool modelReady, bool cameraReady)
        {
            _modelReady = modelReady;
            _cameraReady = cameraReady;
            _guideStatus.text = cameraReady ? "숫자를 프레임 안에 맞춰 주세요" : "카메라 권한 / 준비 확인 중";
            _cameraStatus.text = cameraReady ? "카메라 · 촬영 가능" : "카메라 · 준비 / 권한 확인 대기";
            _modelStatus.text = _stage == RealityStage.AwaitingModel
                ? (modelReady ? "내 모델 · 적용 가능" : "내 모델 · 학습 모델 연결 대기")
                : (modelReady ? "내 모델 · 적용됨 · 숫자 0~3" : "내 모델 · 현재 사용할 수 없음");
            if (_previewOnly) { _modelStatus.text = "학습 모델 · 연결 대기"; }
            RefreshButtons();
        }

        public void ShowStage(RealityStage stage)
        {
            _stage = stage;
            _view.ShowStage(stage);
            _summaryPanel.SetActive(stage == RealityStage.Summary);
            _historyControls.SetActive(stage == RealityStage.Summary);
            _interactionControls.SetActive(stage != RealityStage.Summary);
            switch (stage)
            {
                case RealityStage.AwaitingModel:
                    _stageText.text = "01 · 내 모델 적용";
                    _message.text = "이전 방에서 학습을 완료한 모델을 적용하세요.";
                    break;
                case RealityStage.Ready:
                    _stageText.text = "02 · 숫자 촬영";
                    _message.text = "0~3 중 숫자 하나를 프레임 안에 맞춘 뒤 촬영하세요.";
                    break;
                case RealityStage.Captured:
                    _stageText.text = "03 · 모델 입력 확인";
                    _message.text = "숫자가 잘리지 않았는지 확인하고 인식하기를 누르세요.";
                    break;
                case RealityStage.Recognizing:
                    _stageText.text = "04 · 인식 중";
                    _message.text = "촬영한 입력으로 예측하고 있습니다. 모델을 새로 학습하지 않습니다.";
                    break;
                case RealityStage.Result:
                    _stageText.text = "05 · 결과 확인";
                    _message.text = "실제 숫자를 선택하면 이번 결과가 기록됩니다.";
                    break;
                case RealityStage.Summary:
                    _stageText.text = "06 · 이번 체험 결과";
                    _message.text = "첫 촬영과 재촬영을 구분해 확인하세요. 체험은 여기서 마쳐도 됩니다.";
                    break;
            }
            if (_previewOnly)
            {
                _stageText.text = stage == RealityStage.Captured ? "모델 입력 확인 · 단독 모드" : "숫자 촬영 · 단독 모드";
                _message.text = stage == RealityStage.Captured
                    ? "실제 카메라 입력입니다. [다시 촬영]으로 돌아갈 수 있습니다. 숫자 예측은 모델 연결 후 사용할 수 있습니다."
                    : "카메라 접근을 허용한 뒤 숫자를 촬영하세요. 모델 적용과 숫자 예측은 연결 대기 상태입니다.";
            }
            _guideStatus.text = stage == RealityStage.Ready ? "버튼을 누를 때 선택 영역을 촬영합니다" : "실제 숫자 0~3 · 한 번에 한 자리";
            SetAvailability(_modelReady, _cameraReady);
        }

        public void ShowInput(RealityRecognitionData input, RealityAttempt previous)
        {
            _recorded = false;
            _results.ShowInput(input, previous);
            RefreshButtons();
        }

        public void ShowPrediction(IReadOnlyList<double> scores) => _results.ShowPrediction(scores);

        public void ShowRecorded(RealityAttempt current, RealityAttempt previous)
        {
            _recorded = true;
            _results.ShowRecorded(current, previous);
            _message.text = "기록 완료 · 같은 숫자 재촬영 / 다른 숫자 / 결과 정리 중 선택하세요.";
            RefreshButtons();
        }

        public void ShowHistory(IReadOnlyList<RealityAttempt> history) => _results.ShowHistory(history);
        public void ShowMessage(string text) { _message.text = text; }

        private void RefreshButtons()
        {
            _apply.gameObject.SetActive(_stage == RealityStage.AwaitingModel && !_previewOnly);
            _capture.gameObject.SetActive(_stage == RealityStage.Ready);
            _recognize.gameObject.SetActive(_stage == RealityStage.Captured && !_previewOnly);
            _retake.gameObject.SetActive(_stage == RealityStage.Captured);
            _apply.interactable = _stage == RealityStage.AwaitingModel && _modelReady;
            _capture.interactable = _stage == RealityStage.Ready && _cameraReady && (_modelReady || _previewOnly);
            _recognize.interactable = _stage == RealityStage.Captured && _modelReady;
            _retake.interactable = _stage == RealityStage.Captured;
            bool recordedResult = _stage == RealityStage.Result && _recorded;
            _same.interactable = recordedResult;
            _next.interactable = recordedResult;
            _finish.interactable = !_previewOnly && (_stage == RealityStage.Ready || _stage == RealityStage.Captured || recordedResult);
            foreach (Button label in _labels) { label.interactable = _stage == RealityStage.Result && !_recorded; }
        }
    }
}
