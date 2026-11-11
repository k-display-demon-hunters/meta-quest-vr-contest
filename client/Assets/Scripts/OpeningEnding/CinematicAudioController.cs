using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource _screen, _voice, _type, _impact, _rewind, _pad;
        [SerializeField] private AudioSource[] _room;
        [SerializeField] private AudioSource _stamp, _ring, _printer, _beep;
        private CinematicText _content;
        private float _last = -1;
        private bool _ending;

        public void Bind(CinematicText content, bool ending)
        {
            _content = content; _ending = ending;
            _screen.clip = content.OpeningCrowd;
            _rewind.clip = content.Rewind;
            _pad.clip = ending ? content.Pad : content.OpeningScore;
            if (ending && _pad.clip != null) { _pad.Play(); }
        }

        public void BeginOpening()
        {
            _pad.loop = false;
            _pad.volume = .27f;
            _pad.Play();
        }

        public void Voice(AudioClip clip)
        {
            _voice.Stop(); _voice.clip = clip;
            if (clip != null) { _voice.Play(); }
        }
        public void Type() { _type.pitch = Random.Range(.95f, 1.05f); Play(_type, _content.Type, .12f); }
        public void Hit(bool large) { Play(_impact, large ? _content.BigHit : _content.Hit, large ? .55f : .4f); }
        public void Beep(Vector3 position) { _beep.transform.position = position; Play(_beep, _content.Beep, .25f); }

        public void Evaluate(float time)
        {
            if (!_ending)
            {
                if (Crossed(OpeningTiming.Rewind, time)) { _rewind.Play(); }
                if (Crossed(OpeningTiming.Arrival, time))
                {
                    _rewind.Stop();
                    _screen.time = 0;
                    _screen.Play();
                }
                if (Crossed(OpeningTiming.StampContact, time)) { Play(_stamp, _content.Stamp, .7f); }
                if (Crossed(OpeningTiming.Pixels, time)) { _screen.Stop(); }

                bool underNarration = _voice.isPlaying;
                float crowdFade = 1f - Mathf.InverseLerp(OpeningTiming.Digit, OpeningTiming.Pixels, time);
                _screen.volume = (underNarration ? .12f : .28f) * crowdFade;
                bool quietCue = (time >= OpeningTiming.Rewind && time < OpeningTiming.Arrival)
                    || (time >= OpeningTiming.StampShot && time < OpeningTiming.Envelope);
                float scoreLevel = quietCue ? .055f : underNarration ? .17f : .32f;
                _pad.volume = Mathf.MoveTowards(_pad.volume, scoreLevel, Time.deltaTime * 2f);
            }
            else
            {
                if (Crossed(14f, time)) { StopAll(); }
            }
            _last = time;
        }

        private bool Crossed(float cue, float time) => _last < cue && time >= cue;
        private static void Play(AudioSource source, AudioClip clip, float volume)
        { if (clip != null) { source.PlayOneShot(clip, volume); } }
        private void StopRoom()
        {
            foreach (AudioSource source in _room) { Stop(source); }
            Stop(_screen); Stop(_stamp); Stop(_ring); Stop(_printer);
        }
        public void StopAll()
        { StopRoom(); Stop(_voice); Stop(_type); Stop(_impact); Stop(_pad); Stop(_rewind); Stop(_beep); }
        // Scene roots may be destroyed before this controller during single-scene loading.
        private static void Stop(AudioSource source) { if (source != null) { source.Stop(); } }
        private void OnDisable() { StopAll(); }
    }
}
