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
        private readonly float[] _levels = { .18f, .15f, .15f, .16f, .25f, .12f, .08f, .08f };

        public void Bind(CinematicText content, bool ending)
        {
            _content = content; _ending = ending;
            _room[0].clip = _room[1].clip = _room[2].clip = content.Crowd;
            _room[3].clip = content.Footsteps;
            _room[4].clip = content.Machine;
            _room[5].clip = content.Radio;
            _room[6].clip = content.CRT;
            _room[7].clip = content.Clerk;
            _screen.clip = content.Crowd;
            _rewind.clip = content.Rewind;
            _pad.clip = content.Pad;
            if (_pad.clip != null) { _pad.Play(); }
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
                if (Crossed(3.2f, time)) { if (_rewind.clip != null) { _rewind.Play(); } }
                if (Crossed(5.2f, time)) { if (_screen.clip != null) { _screen.Play(); } }
                if (Crossed(6.7f, time)) { StopAll(); }
                if (Crossed(7f, time))
                {
                    for (int i = 0; i < _room.Length; i++)
                    {
                        if (_room[i].clip == null) { continue; }
                        _room[i].time = i < 3 ? i * 5.1f : 0;
                        _room[i].Play();
                    }
                }
                if (time >= 7f && time < 15f)
                {
                    float muffling = Mathf.Clamp01((time - 12f) / 2f);
                    float duck = _voice.isPlaying ? .501f : 1f;
                    for (int i = 0; i < _room.Length; i++)
                    {
                        _room[i].volume = _levels[i] * duck * Mathf.Lerp(1, .5f, muffling);
                        _room[i].GetComponent<AudioLowPassFilter>().cutoffFrequency = Mathf.Lerp(22000, 350, muffling);
                    }
                    foreach (float cue in new[] { 7.12f, 8.03f, 9.17f, 10.04f, 11.12f })
                    { if (Crossed(cue, time)) { Play(_stamp, _content.Stamp, .45f); } }
                    if (Crossed(8.2f, time) || Crossed(10.4f, time)) { Play(_ring, _content.Ring, .28f); }
                    if (Crossed(9f, time)) { Play(_printer, _content.Printer, .22f); }
                    if (Crossed(10.6f, time)) { _printer.Stop(); }
                }
                if (Crossed(15f, time)) { StopRoom(); }
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
