using System;
using UnityEngine;
namespace RealityLab
{
    public sealed class RealityCaptureManager : MonoBehaviour
    {
        [SerializeField] private RectTransform _selectionFrame;
        [SerializeField] private Canvas _selectionCanvas;
        private readonly Vector3[] _corners = new Vector3[4];
        private RealityCameraController _camera;
        private void Awake() { _camera = gameObject.AddComponent<RealityCameraController>(); }
        public bool IsReady => _camera.IsReady;
        public Rect GetSelectionRegion()
        {
            _selectionFrame.GetWorldCorners(_corners);
            Camera view = _selectionCanvas.worldCamera;
            Vector2 minimum = Vector2.one;
            Vector2 maximum = Vector2.zero;
            foreach (Vector3 corner in _corners)
            {
                if (Vector3.Dot(view.transform.forward, corner - view.transform.position) <= 0)
                {
                    throw new InvalidOperationException("선택 프레임을 정면에서 바라보세요.");
                }
                Vector2 point = _camera.WorldToImage(corner);
                if (float.IsNaN(point.x) || float.IsNaN(point.y)
                    || point.x < 0 || point.y < 0 || point.x > 1 || point.y > 1)
                {
                    throw new InvalidOperationException("프레임 전체가 카메라 범위 안에 들어오도록 위치를 조정하세요.");
                }
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }
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
