using System;
using System.Threading.Tasks;
using ModelDesignScene.Execution.Engine;
using UnityEngine;

namespace RealityLab
{
    internal enum RealityStage { Ready, Captured, Recognizing, Result }

    public sealed class CNNRealityLabManager : MonoBehaviour
    {
        private RealityCaptureManager _capture;
        private RealityUIManager _ui;
        private Func<float[], string, Task<InferenceResult>> _inferAsync;
        private Func<Task> _exitAsync;
        private RealityStage _stage;
        private RealityRecognitionData _input;
        private bool _cameraReady;
        private bool _exiting;
        private int _generation;

        private void Awake()
        {
            _capture = GetComponent<RealityCaptureManager>();
            _ui = GetComponent<RealityUIManager>();
        }

        internal void Initialize(Func<float[], string, Task<InferenceResult>> inferAsync, Func<Task> exitAsync)
        {
            _inferAsync = inferAsync;
            _exitAsync = exitAsync;
            _cameraReady = _capture.IsReady;
            ShowStage(RealityStage.Ready);
        }

        private void OnEnable()
        {
            _ui.CaptureRequested += Capture;
            _ui.RecognizeRequested += Recognize;
            _ui.RetakeRequested += Retake;
            _ui.ExitRequested += Exit;
        }

        private void OnDisable()
        {
            _generation++;
            _ui.CaptureRequested -= Capture;
            _ui.RecognizeRequested -= Recognize;
            _ui.RetakeRequested -= Retake;
            _ui.ExitRequested -= Exit;
        }

        private void Update()
        {
            bool cameraReady = _capture.IsReady;
            if (_cameraReady == cameraReady) { return; }
            _cameraReady = cameraReady;
            _ui.ShowStage(_stage, cameraReady);
        }

        private void LateUpdate()
        {
            if (_stage != RealityStage.Ready) { return; }
            if (_capture.IsReady)
            {
                _capture.GetSelectionGuide(out Pose pose, out Vector2 size);
                _ui.ShowSelectionGuide(pose, size);
            }
            else
            {
                _ui.FollowSelectionGuide();
            }
        }

        private void Capture()
        {
            if (_stage != RealityStage.Ready || !_cameraReady) { return; }
            try
            {
                _input = _capture.CaptureSelection();
                _ui.ShowInput(_input);
                ShowStage(RealityStage.Captured);
            }
            catch (Exception exception)
            {
                _ui.ShowError("촬영하지 못했습니다. ", exception);
            }
        }

        private async void Recognize()
        {
            if (_stage != RealityStage.Captured) { return; }
            int generation = _generation;
            ShowStage(RealityStage.Recognizing);
            try
            {
                InferenceResult result = await _inferAsync(_input.CopyInput(), _input.RequestId);
                if (this == null || generation != _generation) { return; }
                if (result.Output.Count != 4)
                    throw new InvalidOperationException("숫자 0~3의 출력 네 개가 필요합니다.");

                _ui.ShowPrediction(result.Output);
                ShowStage(RealityStage.Result);
            }
            catch (Exception exception)
            {
                if (this == null || generation != _generation) { return; }
                ShowStage(RealityStage.Captured);
                _ui.ShowError("인식하지 못했습니다. ", exception);
            }
        }

        private void Retake()
        {
            if (_stage != RealityStage.Captured && _stage != RealityStage.Result) { return; }
            _input = null;
            _ui.ShowInput(null);
            ShowStage(RealityStage.Ready);
        }

        private async void Exit()
        {
            if (_exiting || _stage == RealityStage.Recognizing) { return; }
            _exiting = true;
            try
            {
                await _exitAsync();
            }
            catch (Exception exception)
            {
                if (this == null) { return; }
                _exiting = false;
                _ui.ShowError("모델방으로 돌아가지 못했습니다. ", exception);
            }
        }

        private void ShowStage(RealityStage stage)
        {
            _stage = stage;
            _ui.ShowStage(stage, _cameraReady);
        }
    }
}
