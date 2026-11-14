using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource _screen, _voice, _type, _impact, _rewind, _pad;
        [SerializeField] private AudioSource _stamp, _beep;
        private const float OpeningScoreVolume = .38f;
        private const float EndingScoreVolume = 1f;
        private const float EndingScoreUnderNarration = .8f;
        private CinematicText _content;
        private AudioSource _endingScoreContinuation;
        private float _endingScoreLevel;
        private bool _endingScoreContinued;
        private const float ScoreOverlap = 2.4f;

        public void Bind(CinematicText content, bool ending)
        {
            _content = content;
            // Play narration and music in 2D so their levels do not depend on head position.
            _voice.spatialBlend = 0f;
            _voice.volume = 1f;
            _voice.priority = 0;
            _pad.spatialBlend = 0f;
            if (!ending) { _screen.spatialBlend = 0f; }
            _screen.clip = content.OpeningCrowd;
            _rewind.clip = content.Rewind;
            if (!ending) { _pad.clip = content.OpeningScore; }
            else
            {
                _endingScoreContinuation = _pad.gameObject.AddComponent<AudioSource>();
                _endingScoreContinuation.playOnAwake = false;
                _endingScoreContinuation.spatialBlend = 0f;
                _endingScoreContinuation.clip = _pad.clip;
                _endingScoreContinuation.volume = 0f;
                _endingScoreContinuation.outputAudioMixerGroup = _pad.outputAudioMixerGroup;
            }
        }

        internal void BeginEnding()
        {
            _pad.loop = false;
            _endingScoreLevel = EndingScoreVolume;
            _endingScoreContinued = false;
            _pad.volume = EndingScoreVolume;
            if (_pad.clip != null) { _pad.Play(); }
        }

        internal void MixEnding(float gain)
        {
            if (_pad.clip == null || gain <= 0f) { return; }

            float target = _voice.isPlaying ? EndingScoreUnderNarration : EndingScoreVolume;
            _endingScoreLevel = Mathf.MoveTowards(_endingScoreLevel, target, Time.unscaledDeltaTime * 2f);
            float volume = _endingScoreLevel * gain;
            if (_endingScoreContinued)
            {
                _endingScoreContinuation.volume = volume;
                return;
            }

            // Re-enter within the source so its opening fade does not produce a gap.
            float overlapStart = _pad.clip.length - ScoreOverlap;
            if (!_pad.isPlaying || _pad.time >= overlapStart)
            {
                float offset = _pad.isPlaying ? _pad.time - overlapStart : ScoreOverlap;
                if (!_endingScoreContinuation.isPlaying)
                {
                    _endingScoreContinuation.time = _pad.clip.length * .25f + offset;
                    _endingScoreContinuation.Play();
                }
                float mix = Mathf.Clamp01(offset / ScoreOverlap);
                _pad.volume = volume * Mathf.Cos(mix * Mathf.PI * .5f);
                _endingScoreContinuation.volume = volume * Mathf.Sin(mix * Mathf.PI * .5f);
                _endingScoreContinued = mix >= 1f;
            }
            else { _pad.volume = volume; }
        }

        public void BeginOpening()
        {
            _pad.loop = false;
            _pad.volume = OpeningScoreVolume;
            _pad.Play();
            Play(_impact, _content.Beep, .7f);
        }

        public void Voice(AudioClip clip)
        {
            _voice.Stop(); _voice.clip = clip;
            if (clip != null) { _voice.Play(); }
        }
        public void Type() { _type.pitch = Random.Range(.95f, 1.05f); Play(_type, _content.Type, .12f); }
        public void Hit(bool large) { Play(_impact, large ? _content.BigHit : _content.Hit, large ? .55f : .4f); }
        public void Beep(Vector3 position) { _beep.transform.position = position; Play(_beep, _content.Beep, .25f); }

        internal void StartRewind() => _rewind.Play();

        internal void StartCrowd()
        {
            _rewind.Stop();
            _screen.time = 0;
            _screen.Play();
        }

        internal void PlayStamp() => Play(_stamp, _content.Stamp, .7f);
        internal void StopCrowd() => _screen.Stop();

        internal void MixOpening(float crowdFade, bool quietCue)
        {
            bool underNarration = _voice.isPlaying;
            _screen.volume = (underNarration ? .24f : .46f) * crowdFade;
            float scoreLevel = quietCue ? .10f : underNarration ? .12f : OpeningScoreVolume;
            _pad.volume = Mathf.MoveTowards(_pad.volume, scoreLevel, Time.deltaTime * 2f);
        }

        private static void Play(AudioSource source, AudioClip clip, float volume)
        { if (clip != null) { source.PlayOneShot(clip, volume); } }
        public void StopAll()
        {
            Stop(_screen);
            Stop(_stamp);
            Stop(_voice);
            Stop(_type);
            Stop(_impact);
            Stop(_pad);
            Stop(_endingScoreContinuation);
            Stop(_rewind);
            Stop(_beep);
        }

        // Scene roots may be destroyed before this controller during single-scene loading.
        private static void Stop(AudioSource source) { if (source != null) { source.Stop(); } }
        private void OnDisable() { StopAll(); }
    }
}
