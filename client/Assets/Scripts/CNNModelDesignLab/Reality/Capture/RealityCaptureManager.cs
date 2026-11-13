using System;
using UnityEngine;
namespace RealityLab
{
    public sealed class RealityCaptureManager : MonoBehaviour
    {
        private RealityCameraController _camera;
        private void Awake() { _camera = gameObject.AddComponent<RealityCameraController>(); }
        public bool IsReady => _camera.IsReady;

        internal void Initialize(Camera view) => _camera.Initialize(view);

        internal void GetSelectionGuide(out Pose pose, out Vector2 size)
        {
            _camera.GetSelectionGuide(out pose, out size);
        }

        internal RealityRecognitionData CaptureSelection()
        {
            Texture2D image = _camera.Capture();
            Rect cameraViewportRegion = RealityCameraController.GetSelectionRegion(image.width, image.height);
            try
            {
                float[] pixels = RealityImagePreprocessor.Convert(image.GetPixels32(), image.width, image.height, cameraViewportRegion);
                return new RealityRecognitionData("reality:" + Guid.NewGuid().ToString("N"), pixels);
            }
            finally { Destroy(image); }
        }
    }
}
