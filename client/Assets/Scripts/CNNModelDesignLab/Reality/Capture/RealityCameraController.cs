using System;
using Meta.XR;
using UnityEngine;
namespace RealityLab
{
    public sealed class RealityCameraController : MonoBehaviour
    {
        private PassthroughCameraAccess _access;
        private void Awake()
        {
            _access = gameObject.AddComponent<PassthroughCameraAccess>();
        }
        public bool IsReady => _access.IsPlaying;
        // Use a square covering 40% of the image's shorter dimension.
        internal static Rect GetSelectionRegion(int width, int height)
        {
            float side = Mathf.Min(width, height) * 0.4f;
            Vector2 size = new Vector2(side / width, side / height);
            return new Rect((Vector2.one - size) * 0.5f, size);
        }

        internal void GetSelectionGuide(out Pose pose, out Vector2 size)
        {
            Vector2Int resolution = _access.CurrentResolution;
            Rect region = GetSelectionRegion(resolution.x, resolution.y);
            Pose cameraPose = _access.GetCameraPose();
            Vector3 forward = cameraPose.rotation * Vector3.forward;
            Ray lowerRay = _access.ViewportPointToRay(region.min, cameraPose);
            Ray upperRay = _access.ViewportPointToRay(region.max, cameraPose);
            // Intersect both rays with the camera-facing guide plane at 1.2 metres.
            Vector3 lower = lowerRay.GetPoint(1.2f / Vector3.Dot(lowerRay.direction, forward));
            Vector3 upper = upperRay.GetPoint(1.2f / Vector3.Dot(upperRay.direction, forward));
            pose = new Pose((lower + upper) * 0.5f, cameraPose.rotation);
            Vector3 localSize = Quaternion.Inverse(cameraPose.rotation) * (upper - lower);
            size = new Vector2(localSize.x, localSize.y);
        }
        public Texture2D Capture()
        {
            if (!_access.IsPlaying) { throw new InvalidOperationException("Camera is not ready. Grant headset camera access first."); }
            Texture source = _access.GetTexture();
            RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                image = new Texture2D(source.width, source.height, TextureFormat.RGB24, false, false);
                image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                image.Apply();
                return image;
            }
            catch
            {
                if (image != null) { Destroy(image); }
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
