using System;
using UnityEngine;
namespace RealityLab
{
    public sealed class RealityCaptureManager : MonoBehaviour
    {
        private RealityCameraController _camera;
        private void Awake() { _camera = gameObject.AddComponent<RealityCameraController>(); }
        public bool IsReady => _camera.IsReady;
        public RealityRecognitionData Capture(Rect cameraViewportRegion)
        {
            Texture2D image = _camera.Capture();
            try
            {
                float[] pixels = RealityImagePreprocessor.Convert(image.GetPixels32(), image.width, image.height, cameraViewportRegion);
                return new RealityRecognitionData("reality:" + Guid.NewGuid().ToString("N"), pixels);
            }
            finally { Destroy(image); }
        }
        public Vector2 WorldToImage(Vector3 point) => _camera.WorldToImage(point);
    }
}
