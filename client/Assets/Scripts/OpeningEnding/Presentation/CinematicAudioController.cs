using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource _screen, _voice, _type, _impact, _rewind, _pad;
        [SerializeField] private AudioSource _stamp, _beep;
        private const float OpeningScoreVolume = .95f;
        private CinematicText _content;

        public void Bind(CinematicText content, bool ending)
        {
            _content = content;
            _screen.clip = content.OpeningCrowd;
            _rewind.clip = content.Rewind;
            _pad.clip = ending ? content.Pad : content.OpeningScore;
            if (ending && _pad.clip != null) { _pad.Play(); }
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
            _screen.volume = (underNarration ? .12f : .28f) * crowdFade;
            float scoreLevel = quietCue ? .18f : underNarration ? .55f : OpeningScoreVolume;
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
            Stop(_rewind);
            Stop(_beep);
        }

        // Scene roots may be destroyed before this controller during single-scene loading.
        private static void Stop(AudioSource source) { if (source != null) { source.Stop(); } }
        private void OnDisable() { StopAll(); }
    }
}
