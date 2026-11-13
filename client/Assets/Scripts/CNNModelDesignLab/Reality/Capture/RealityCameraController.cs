using System;
using Meta.XR;
using UnityEngine;
namespace RealityLab
{
    public sealed class RealityCameraController : MonoBehaviour
    {
        private PassthroughCameraAccess _access;
        private Transform _view;
        private void Awake()
        {
            _access = gameObject.AddComponent<PassthroughCameraAccess>();
        }
        public bool IsReady => _access.IsPlaying;

        internal void Initialize(Camera view) => _view = view.transform;

        // Keep the crop size, but lift its center to leave room for controls below.
        internal static Rect GetSelectionRegion(int width, int height)
        {
            float side = Mathf.Min(width, height) * 0.4f;
            Vector2 size = new Vector2(side / width, side / height);
            Vector2 center = new Vector2(0.5f, 0.6f);
            return new Rect(center - size * 0.5f, size);
        }

        internal void GetSelectionGuide(out Pose pose, out Vector2 size)
        {
            Vector2Int resolution = _access.CurrentResolution;
            Rect region = GetSelectionRegion(resolution.x, resolution.y);
            // The shared rig uses XROrigin, not OVRCameraRig. Compose the lens pose
            // with the rendered head pose so rig movement and Camera Offset are included.
            Pose lensOffset = _access.Intrinsics.LensOffset;
            Pose cameraPose = new Pose(
                _view.position + _view.rotation * lensOffset.position,
                _view.rotation * lensOffset.rotation);
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
