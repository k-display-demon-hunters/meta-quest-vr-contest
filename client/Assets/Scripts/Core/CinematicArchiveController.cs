using UnityEngine;
using UnityEngine.Video;

namespace Cinematic
{
    public sealed class CinematicArchiveController : MonoBehaviour
    {
        [SerializeField] private Renderer _surface;
        [SerializeField] private VideoPlayer _player;
        private Material _material;
        private RenderTexture _videoTexture;
        private CinematicText.ArchiveClip _clip;
        private float _elapsed;

        private void Awake()
        {
            _material = _surface.material;
            _videoTexture = new RenderTexture(1280, 720, 0);
            _player.targetTexture = _videoTexture;
            _player.prepareCompleted += Prepared;
            _player.errorReceived += Failed;
        }

        public void Show(CinematicText.ArchiveClip clip)
        {
            _player.Stop(); _clip = clip; _elapsed = 0;
            _material.mainTexture = clip.Still;
            _material.mainTextureScale = clip.UV.size;
            _material.mainTextureOffset = clip.UV.position;
            if (clip.Video != null) { _player.clip = clip.Video; _player.Prepare(); }
        }
        public void SetTone(float monochrome, float brightness)
        { _material.SetFloat("_Mono", monochrome); _material.SetFloat("_Brightness", brightness); }
        private void Prepared(VideoPlayer player)
        {
            player.time = _clip.StartSeconds;
            _material.mainTexture = _videoTexture;
            _material.mainTextureScale = Vector2.one; _material.mainTextureOffset = Vector2.zero;
            player.Play();
        }
        private void Failed(VideoPlayer player, string message)
        {
            Debug.LogWarning("Archive video unavailable; retaining the reconstruction still. " + message);
            _material.mainTexture = _clip.Still;
        }
        private void Update()
        {
            if (!_player.isPlaying) { return; }
            _elapsed += Time.deltaTime;
            if (_elapsed >= _clip.Duration) { _player.Pause(); }
        }
        public void Stop() { _player.Stop(); }
        private void OnDestroy()
        {
            _player.prepareCompleted -= Prepared; _player.errorReceived -= Failed;
            _videoTexture.Release(); Destroy(_videoTexture); Destroy(_material);
        }
    }
}
