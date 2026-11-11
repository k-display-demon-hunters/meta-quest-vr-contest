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
        public Vector2 WorldToImage(Vector3 point) => _access.WorldToViewportPoint(point);
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
