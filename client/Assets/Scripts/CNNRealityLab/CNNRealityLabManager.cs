using System;
using System.Threading.Tasks;
using ModelDesignScene;
using ModelDesignScene.Execution.Engine;
using UnityEngine;
namespace RealityLab
{
    public sealed class CNNRealityLabManager : MonoBehaviour
    {
        private CNNModelDesignLabManager _source;
        private RealityCaptureManager _capture;
        private RealityUIManager _ui;
        private readonly RealitySession _session = new RealitySession();
        private RealityStage _stage = RealityStage.AwaitingModel;
        private RealityStage _resumeStage;
        private RealityRecognitionData _input;
        private InferenceResult _result;
        private RealityAttempt _record;
        private int _generation;
        private bool _previewOnly;
        private bool _lastModelReady;
        private bool _lastCameraReady;
        private bool IsModelReady => _source != null && _source.CanExit;
        public bool IsCameraReady => _capture.IsReady;
        public event Action<RealityRecognitionData> InputPrepared;
        public event Action<InferenceResult> InferenceCompleted;
        public event Action<string> Failed;
        private void Awake()
        {
            _capture = GetComponent<RealityCaptureManager>();
            _ui = GetComponent<RealityUIManager>();
        }
        private void OnEnable()
        {
            _ui.ApplyRequested += ApplyModel;
            _ui.CaptureRequested += CaptureSelection;
            _ui.RecognizeRequested += HandleRecognizeRequested;
            _ui.RetakeRequested += RetakeUnrecognizedInput;
            _ui.SameDigitRequested += PrepareSameDigit;
            _ui.NextDigitRequested += PrepareNextDigit;
            _ui.LabelRequested += RecordLabel;
            _ui.SummaryRequested += ShowSummary;
            _ui.ResumeRequested += Resume;
        }
        private void Update()
        {
            bool modelReady = IsModelReady;
            bool cameraReady = IsCameraReady;
            if (modelReady == _lastModelReady && cameraReady == _lastCameraReady) { return; }
            _lastModelReady = modelReady;
            _lastCameraReady = cameraReady;
            _ui.SetAvailability(modelReady, cameraReady);
        }
        public void Initialize(CNNModelDesignLabManager source)
        {
            _source = source;
            _ui.SetAvailability(IsModelReady, IsCameraReady);
        }
        public void InitializeCapturePreview()
        {
            _previewOnly = true;
            _ui.EnableCapturePreview();
            _ui.ShowInput(null, null);
            SetStage(RealityStage.Ready);
            _ui.SetAvailability(false, IsCameraReady);
        }
        public Vector2 WorldToCameraImage(Vector3 worldPoint) => _capture.WorldToImage(worldPoint);
        public RealityRecognitionData Capture(Rect cameraViewportRegion)
        {
            RealityRecognitionData input = _capture.Capture(cameraViewportRegion);
            InputPrepared?.Invoke(input);
            return input;
        }
        public async Task RecognizeAsync(RealityRecognitionData input)
        {
            int generation = _generation;
            try
            {
                InferenceResult result = await _source.InferRealityAsync(input.CopyInput(), input.RequestId);
                if (this == null || !isActiveAndEnabled || generation != _generation) { return; }
                if (_stage == RealityStage.Recognizing && _input.RequestId == result.ImageId)
                {
                    if (result.Output.Count != 4) { throw new InvalidOperationException("숫자 0~3의 출력 네 개가 필요합니다."); }
                    _result = result;
                    SetStage(RealityStage.Result);
                    _ui.ShowPrediction(result.Output);
                    if (_session.Previous != null) { RecordLabel(_session.Previous.Actual); }
                }
                InferenceCompleted?.Invoke(result);
            }
            catch (Exception exception)
            {
                if (this == null || !isActiveAndEnabled || generation != _generation) { return; }
                if (_stage == RealityStage.Recognizing) { SetStage(RealityStage.Captured); }
                _ui.ShowMessage("인식하지 못했습니다. 다시 시도하세요.\n" + exception.Message);
                Failed?.Invoke(exception.Message);
            }
        }
        public Task ExitAsync() => _source.ExitRealityAsync();
        private void ApplyModel()
        {
            if (_stage != RealityStage.AwaitingModel || !IsModelReady) { return; }
            _ui.ShowInput(null, null);
            SetStage(RealityStage.Ready);
        }
        private void CaptureSelection()
        {
            if (_stage != RealityStage.Ready || (!_previewOnly && !IsModelReady) || !IsCameraReady) { return; }
            try
            {
                _input = Capture(_capture.GetSelectionRegion());
                _ui.ShowInput(_input, _session.Previous);
                SetStage(RealityStage.Captured);
            }
            catch (Exception exception)
            {
                _ui.ShowMessage("촬영하지 못했습니다. 프레임과 카메라 권한을 확인하세요.\n" + exception.Message);
                Failed?.Invoke(exception.Message);
            }
        }
        private async void HandleRecognizeRequested()
        {
            if (_stage != RealityStage.Captured || !IsModelReady) { return; }
            SetStage(RealityStage.Recognizing);
            await RecognizeAsync(_input);
        }
        private void RecordLabel(int digit)
        {
            if (_stage != RealityStage.Result || _record != null || digit < 0 || digit > 3) { return; }
            _record = _session.Record(_input, _result.Output, digit);
            _ui.ShowRecorded(_record, _session.Previous);
        }
        private void RetakeUnrecognizedInput()
        {
            if (_stage != RealityStage.Captured) { return; }
            ClearCapture();
        }
        private void PrepareSameDigit() => PrepareNext(true);
        private void PrepareNextDigit() => PrepareNext(false);
        private void PrepareNext(bool retry)
        {
            if (_stage != RealityStage.Result || _record == null) { return; }
            _session.PrepareNext(retry);
            ClearCapture();
        }
        private void ClearCapture()
        {
            _input = null;
            _result = null;
            _record = null;
            _ui.ShowInput(null, _session.Previous);
            SetStage(RealityStage.Ready);
        }
        private void ShowSummary()
        {
            if (_stage != RealityStage.Ready && _stage != RealityStage.Captured
                && !(_stage == RealityStage.Result && _record != null)) { return; }
            _resumeStage = _stage;
            SetStage(RealityStage.Summary);
            _ui.ShowHistory(_session.Attempts);
        }
        private void Resume()
        {
            if (_stage != RealityStage.Summary) { return; }
            _ui.ShowInput(_input, _session.Previous);
            SetStage(_resumeStage);
            if (_result != null) { _ui.ShowPrediction(_result.Output); }
            if (_record != null) { _ui.ShowRecorded(_record, _session.Previous); }
        }
        private void SetStage(RealityStage stage)
        {
            _stage = stage;
            _ui.ShowStage(stage);
        }
        private void OnDisable()
        {
            _generation++;
            if (_stage == RealityStage.Recognizing) { SetStage(RealityStage.Captured); }
            _ui.ApplyRequested -= ApplyModel;
            _ui.CaptureRequested -= CaptureSelection;
            _ui.RecognizeRequested -= HandleRecognizeRequested;
            _ui.RetakeRequested -= RetakeUnrecognizedInput;
            _ui.SameDigitRequested -= PrepareSameDigit;
            _ui.NextDigitRequested -= PrepareNextDigit;
            _ui.LabelRequested -= RecordLabel;
            _ui.SummaryRequested -= ShowSummary;
            _ui.ResumeRequested -= Resume;
        }
    }
}
