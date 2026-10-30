using UnityEngine;

namespace UI
{
    public sealed class PixelBoardController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _boardRenderer;
        private Material _material;

        public void ShowPhoto(Texture2D photo)
        {
            _material = _boardRenderer.material;
            _material.mainTexture = photo;
        }

        public void SetPhotoVisible(bool visible) { _boardRenderer.enabled = visible; }

        private void OnDestroy()
        {
            Destroy(_material);
        }
    }
}
