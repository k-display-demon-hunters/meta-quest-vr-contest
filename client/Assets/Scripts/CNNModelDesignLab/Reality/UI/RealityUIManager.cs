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
            _guideStatus.text = cameraReady ? "촬영 준비 완료" : "헤드셋 카메라 권한을 확인하세요.";
            switch (stage)
            {
                case RealityStage.Ready:
                    _stageText.text = "숫자 촬영";
                    _message.text = "숫자가 프레임 안에 들어오면 ‘촬영하기’를 눌러 주세요.";
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

        internal void ShowSelectionGuide(Pose pose, Vector2 size) => _view.ShowSelectionGuide(pose, size);
        internal void FollowSelectionGuide() => _view.FollowSelectionGuide();

        internal void ShowInput(RealityRecognitionData input) => _results.ShowInput(input);
        internal void ShowPrediction(IReadOnlyList<double> scores) => _results.ShowPrediction(scores);

        internal void ShowError(string context, Exception exception)
        {
            ShowMessage(context + GetErrorMessage(exception));
        }

        private static string GetErrorMessage(Exception exception)
        {
            switch (exception.Message)
            {
                case "Selection must be inside the camera viewport.":
                    return "촬영 영역을 처리하지 못했습니다. 다시 촬영하세요.";
                case "Camera is not ready. Grant headset camera access first.":
                    return "카메라가 준비되지 않았습니다. 헤드셋 카메라 권한을 확인하세요.";
                case "Invalid camera image.":
                    return "촬영한 영상을 읽을 수 없습니다. 다시 촬영하세요.";
                case "The trained model is not available for inference.":
                    return "현재 학습한 모델을 사용할 수 없습니다.";
                case "숫자 0~3의 출력 네 개가 필요합니다.":
                    return "모델의 숫자 0~3 예측 결과를 확인할 수 없습니다.";
                case "Wait for the current transition.":
                    return "화면 전환이 끝날 때까지 기다려 주세요.";
                default:
                    Debug.LogException(exception);
                    return "알 수 없는 오류가 발생했습니다.";
            }
        }

        private void ShowMessage(string message)
        {
            _message.text = message;
            _results.ShowMessage(message);
        }
    }
}
