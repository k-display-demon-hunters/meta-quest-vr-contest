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
        private bool _frameVideo;

        private void Awake()
        {
            _material = _surface.material;
            _videoTexture = new RenderTexture(1280, 720, 0);
            _player.targetTexture = _videoTexture;
            _player.prepareCompleted += Prepared;
            _player.errorReceived += Failed;
        }

        public void Show(CinematicText.ArchiveClip clip, bool frameVideo = false)
        {
            bool ready = clip.Video != null && _player.clip == clip.Video && _player.isPrepared;
            if (!ready && _player.clip != clip.Video) { _player.Stop(); }
            _clip = clip;
            _frameVideo = frameVideo;
            _elapsed = 0;
            _material.mainTexture = clip.Still;
            SetFraming(clip.UV);
            SetPeopleMotion(0f);
            if (ready) { Prepared(_player); }
            else if (clip.Video != null) { PrepareVideo(clip.Video); }
        }

        public void PrepareVideo(VideoClip video)
        {
            _player.clip = video;
            _player.Prepare();
        }
        public void SetFraming(Rect uv)
        {
            _material.mainTextureScale = uv.size;
            _material.mainTextureOffset = uv.position;
        }

        public void SetPeopleMotion(float time) => _material.SetFloat("_PeopleMotionTime", time);

        public void SetTone(float monochrome, float brightness)
        { _material.SetFloat("_Mono", monochrome); _material.SetFloat("_Brightness", brightness); }
        private void Prepared(VideoPlayer player)
        {
            if (_clip.Video != player.clip) { return; }
            player.time = _clip.StartSeconds;
            _material.mainTexture = _videoTexture;
            SetFraming(_frameVideo ? _clip.UV : new Rect(0, 0, 1, 1));
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
