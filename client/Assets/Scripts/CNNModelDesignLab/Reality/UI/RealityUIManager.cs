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
        [SerializeField] private Text _guideStatus;
        [SerializeField] private Text _stageText;
        [SerializeField] private Text _message;
        [SerializeField] private Button _capture;
        [SerializeField] private Button _recognize;
        [SerializeField] private Button _retake;
        [SerializeField] private Button _resultRetake;
        [SerializeField] private Button _exit;
        [SerializeField] private Button _resultExit;

        public event Action CaptureRequested;
        public event Action RecognizeRequested;
        public event Action RetakeRequested;
        public event Action ExitRequested;

        public void RequestCapture() => CaptureRequested?.Invoke();
        public void RequestRecognize() => RecognizeRequested?.Invoke();
        public void RequestRetake() => RetakeRequested?.Invoke();
        public void RequestExit() => ExitRequested?.Invoke();

        internal void ShowStage(RealityStage stage, bool cameraReady)
        {
            _view.ShowStage(stage);
            _capture.gameObject.SetActive(stage == RealityStage.Ready);
            _capture.interactable = cameraReady;
            _recognize.gameObject.SetActive(stage == RealityStage.Captured);
            _recognize.interactable = stage == RealityStage.Captured;
            _retake.gameObject.SetActive(stage == RealityStage.Captured);
            _retake.interactable = stage == RealityStage.Captured;
            _exit.interactable = stage != RealityStage.Recognizing;
            _resultRetake.interactable = stage == RealityStage.Result;
            _resultExit.interactable = stage == RealityStage.Result;

            _cameraStatus.text = cameraReady ? "카메라 · 촬영 가능" : "카메라 · 권한 확인 중";
            _guideStatus.text = cameraReady ? "숫자 0~3 중 한 자리" : "헤드셋 카메라 권한을 확인하세요.";
            switch (stage)
            {
                case RealityStage.Ready:
                    _stageText.text = "숫자 촬영";
                    _message.text = "숫자 하나를 프레임에 맞춘 뒤 촬영하세요.";
                    break;
                case RealityStage.Captured:
                    _stageText.text = "모델 입력 확인";
                    _message.text = "숫자가 잘리지 않았는지 확인하고 인식하기를 누르세요.";
                    break;
                case RealityStage.Recognizing:
                    _stageText.text = "인식 중";
                    _message.text = "촬영한 입력으로 숫자를 예측하고 있습니다.";
                    break;
            }
        }

        internal void ShowInput(RealityRecognitionData input) => _results.ShowInput(input);
        internal void ShowPrediction(IReadOnlyList<double> scores) => _results.ShowPrediction(scores);

        internal void ShowMessage(string message)
        {
            _message.text = message;
            _results.ShowMessage(message);
        }
    }
}
