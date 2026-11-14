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
        private int? _openingRunId;
        private static readonly int UpscaleSweep = Shader.PropertyToID("_UpscaleSweep");

        internal void SetOpeningRun(int? runId) => _openingRunId = runId;

        private void Awake()
        {
            _material = _surface.material;
            _videoTexture = new RenderTexture(1280, 720, 0);
            _player.targetTexture = _videoTexture;
            _player.prepareCompleted += HandlePrepareCompleted;
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
            SetUpscaleSweep(-1f);
            SetPhotoBlend(1f, false);
            if (ready) { Prepared(_player); }
            else if (clip.Video != null) { PrepareVideo(clip.Video); }
        }

        public void PrepareVideo(VideoClip video)
        {
            _player.clip = video;
            Log($"VideoPrepareRequested clip={video?.name}");
            _player.Prepare();
        }
        public void SetFraming(Rect uv)
        {
            _material.mainTextureScale = uv.size;
            _material.mainTextureOffset = uv.position;
        }

        internal void SetPreviousPhoto(Texture2D photo, Rect crop)
        {
            _material.SetTexture("_PreviousPhoto", photo);
            _material.SetVector("_PreviousPhotoUV", new Vector4(crop.width, crop.height, crop.x, crop.y));
        }

        internal void SetPhotoBlend(float blend, bool hasPrevious) =>
            _material.SetFloat("_PhotoBlend", hasPrevious ? blend : 1f);

        internal void SetUpscaleFraming(Rect crop) =>
            _material.SetVector("_UpscaleFrameUV", new Vector4(crop.width, crop.height, crop.x, crop.y));

        internal void SetUpscaleSweep(float position) => _material.SetFloat(UpscaleSweep, position);

        public void SetPeopleMotion(float time) => _material.SetFloat("_PeopleMotionTime", time);

        public void SetTone(float monochrome, float brightness)
        { _material.SetFloat("_Mono", monochrome); _material.SetFloat("_Brightness", brightness); }
        private void HandlePrepareCompleted(VideoPlayer player)
        {
            Log($"VideoPrepared clip={player.clip?.name}");
            Prepared(player);
        }

        private void Prepared(VideoPlayer player)
        {
            if (_clip.Video != player.clip) { return; }
            player.time = _clip.StartSeconds;
            _material.mainTexture = _videoTexture;
            SetFraming(_frameVideo ? _clip.UV : new Rect(0, 0, 1, 1));
            Log($"VideoPlaybackRequested clip={player.clip?.name}");
            player.Play();
        }
        private void Failed(VideoPlayer player, string message)
        {
            string prefix = _openingRunId.HasValue ? $"[Opening][run={_openingRunId.Value}] " : "";
            Debug.LogWarning(prefix + "Archive video unavailable; retaining the reconstruction still. " + message, this);
            _material.mainTexture = _clip.Still;
        }
        private void Update()
        {
            if (!_player.isPlaying) { return; }
            _elapsed += Time.deltaTime;
            if (_elapsed >= _clip.Duration) { _player.Pause(); }
        }
        public void Stop() { _player.Stop(); }
        private void Log(string message)
        {
            if (_openingRunId.HasValue) { Debug.Log($"[Opening][run={_openingRunId.Value}] {message}", this); }
        }

        private void OnDestroy()
        {
            _player.prepareCompleted -= HandlePrepareCompleted; _player.errorReceived -= Failed;
            _videoTexture.Release(); Destroy(_videoTexture); Destroy(_material);
        }
    }
}
