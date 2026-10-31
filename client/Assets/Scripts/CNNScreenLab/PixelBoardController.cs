using System.Collections;
using UnityEngine;

namespace UI
{
    public sealed class PixelBoardController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _boardRenderer;
        [SerializeField] private Transform _presentationRoot;
        private Material _material;
        private Vector3 _homePosition;
        private Vector3 _homeScale;
        public void ShowPhoto(Texture2D photo)
        {
            _homePosition = _presentationRoot.localPosition;
            _homeScale = _presentationRoot.localScale;
            _material = _boardRenderer.material;
            _material.mainTexture = photo;
        }
        public void SetPhotoVisible(bool visible) { _boardRenderer.enabled = visible; }
        public IEnumerator MoveAside()
        {
            Vector3 target = new Vector3(-2.8f, 2.4f, 1.4f);
            for (float time = 0; time < 1.6f; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / 1.6f);
                _presentationRoot.localPosition = Vector3.Lerp(_homePosition, target, t);
                _presentationRoot.localScale = Vector3.Lerp(_homeScale, _homeScale * 0.55f, t);
                yield return null;
            }
            _presentationRoot.localPosition = target;
            _presentationRoot.localScale = _homeScale * 0.55f;
        }
        public void RestorePresentation()
        {
            _presentationRoot.localPosition = _homePosition;
            _presentationRoot.localScale = _homeScale;
            SetPhotoVisible(true);
        }
        private void OnDestroy() { Destroy(_material); }
    }
}
