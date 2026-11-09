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
        public bool IsCameraReady => _capture.IsReady;
        public event Action<RealityRecognitionData> InputPrepared;
        public event Action<InferenceResult> InferenceCompleted;
        public event Action<string> Failed;
        public void Initialize(CNNModelDesignLabManager source)
        {
            _source = source;
            _capture = gameObject.AddComponent<RealityCaptureManager>();
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
            try
            {
                InferenceResult result = await _source.InferRealityAsync(input.CopyInput(), input.RequestId);
                if (this != null && isActiveAndEnabled) { InferenceCompleted?.Invoke(result); }
            }
            catch (Exception exception)
            {
                if (this != null && isActiveAndEnabled) { Failed?.Invoke(exception.Message); }
            }
        }
        public Task ExitAsync() => _source.ExitRealityAsync();
    }
}
