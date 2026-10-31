using UnityEngine;

namespace OpeningEnding
{
    [DisallowMultipleComponent]
    public sealed class FactoryVillageDisplayController : MonoBehaviour
    {
        private const float UpscalingFrameTime = 3f;
        private enum BroadcastChannel { News, Football, Film }

        private static readonly int PlaybackTimeId = Shader.PropertyToID("_PlaybackTime");
        private static readonly int SignalTimeId = Shader.PropertyToID("_SignalTime");
        private static readonly int DamageId = Shader.PropertyToID("_Damage");
        private static readonly int InterferenceId = Shader.PropertyToID("_Interference");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int ChannelId = Shader.PropertyToID("_Channel");
        private static readonly int UpscalingPreviewId = Shader.PropertyToID("_UpscalingPreview");
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");

        [SerializeField] private Renderer _surface;
        [SerializeField, Min(0f)] private float _failureTime = 10.5f;
        [SerializeField, Range(0f, 1f)] private float _phase;
        [SerializeField] private BroadcastChannel _channel;

        private MaterialPropertyBlock _originalProperties;
        private MaterialPropertyBlock _properties;
        private float _storyPlaybackOffset;

        private void Awake()
        {
            CacheProperties();
        }

        public void ResetDisplay()
        {
            _storyPlaybackOffset = 0f;
            EvaluateIdle(0f);
        }

        public void EvaluateIdle(float elapsed)
        {
            float time = Mathf.Max(0f, elapsed);
            ApplyFrame(time, time, 0f, 0f);
        }

        public void BeginStory(float ambientTime)
        {
            _storyPlaybackOffset = Mathf.Max(0f, ambientTime);
            Evaluate(0f);
        }

        public void Evaluate(float elapsed)
        {
            float time = Mathf.Max(0f, elapsed);
            float failureAge = Mathf.Max(0f, time - _failureTime);
            float damage = Mathf.SmoothStep(0f, 1f, failureAge / 1.2f);
            float interference = time < _failureTime ? 0f : Mathf.Lerp(0.85f, 0.12f, Mathf.Clamp01(failureAge / 2f));

            // Continue the programme that was already playing on the title screen, then freeze its last frame.
            ApplyFrame(_storyPlaybackOffset + Mathf.Min(time, _failureTime),
                _storyPlaybackOffset + time, damage, interference);
        }

        public void BeginRestoration()
        {
            _storyPlaybackOffset = 0f;
            EvaluateRestoration(0f);
        }

        public void EvaluateUpscaling(float revealProgress)
        {
            // Compare the same frozen programme frame. Only sampling resolution changes;
            // this teaching presentation does not run neural-network inference.
            ApplyFrame(UpscalingFrameTime, UpscalingFrameTime, 0f, 0f, true, Mathf.Clamp01(revealProgress));
        }

        public void EvaluateUpscaled(float elapsed)
        {
            // Continue from the exact frame used by the before/after comparison.
            float time = UpscalingFrameTime + Mathf.Max(0f, elapsed);
            ApplyFrame(time, time, 0f, 0f);
        }

        public void EvaluateRestoration(float elapsed)
        {
            float time = Mathf.Max(0f, elapsed);
            float recoveryStart = 8f + (float)_channel * 1.35f;
            float recovery = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(recoveryStart, recoveryStart + 2.1f, time));
            // Each damaged broadcast clears and resumes in turn as power reaches its display.
            float playback = _failureTime + Mathf.Max(0f, time - recoveryStart) * recovery;
            float interference = (1f - recovery) * (time < recoveryStart ? 0.12f : 0.3f);
            ApplyFrame(playback, time + _failureTime, 1f - recovery, interference);
        }

        private void ApplyFrame(float playbackTime, float signalTime, float damage, float interference,
            bool upscalingPreview = false, float revealProgress = 0f)
        {
            if (!CacheProperties())
            {
                return;
            }

            _properties.SetFloat(PlaybackTimeId, playbackTime);
            _properties.SetFloat(SignalTimeId, signalTime);
            _properties.SetFloat(DamageId, damage);
            _properties.SetFloat(InterferenceId, interference);
            _properties.SetFloat(PhaseId, _phase);
            _properties.SetFloat(ChannelId, (float)_channel);
            _properties.SetFloat(UpscalingPreviewId, upscalingPreview ? 1f : 0f);
            _properties.SetFloat(RevealProgressId, revealProgress);
            _surface.SetPropertyBlock(_properties);
        }

        private bool CacheProperties()
        {
            if (_surface == null)
            {
                return false;
            }
            if (_properties != null)
            {
                return true;
            }

            _originalProperties = new MaterialPropertyBlock();
            _properties = new MaterialPropertyBlock();
            _surface.GetPropertyBlock(_originalProperties);
            _surface.GetPropertyBlock(_properties);
            return true;
        }

        private void OnDisable()
        {
            if (_surface != null && _originalProperties != null)
            {
                _surface.SetPropertyBlock(_originalProperties);
            }
        }
    }
}
