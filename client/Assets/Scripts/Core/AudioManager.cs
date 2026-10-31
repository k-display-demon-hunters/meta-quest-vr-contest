using System.Collections;
using UnityEngine;

namespace Core
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        private AudioSource _music;
        private AudioSource _outgoing;
        private AudioSource _effects;
        private Coroutine _fade;

        private void Awake()
        {
            _music = CreateSource();
            _outgoing = CreateSource();
            _effects = CreateSource();
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        public void SetMasterVolume(float volume) => AudioListener.volume = Mathf.Clamp01(volume);

        public void PlayMusic(AudioClip clip, bool loop, float volume, float fadeSeconds = 0f)
        {
            CancelFade();
            _outgoing.Stop();
            AudioSource previous = _music;
            _music = _outgoing;
            _outgoing = previous;
            _music.clip = clip;
            _music.loop = loop;
            _music.volume = fadeSeconds > 0f ? 0f : volume;
            _music.Play();
            if (fadeSeconds > 0f)
                _fade = StartCoroutine(Crossfade(volume, fadeSeconds));
            else
                _outgoing.Stop();
        }

        private IEnumerator Crossfade(float targetVolume, float duration)
        {
            float startVolume = _outgoing.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float blend = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _outgoing.volume = startVolume * (1f - blend);
                _music.volume = targetVolume * blend;
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            _outgoing.Stop();
            _music.volume = targetVolume;
            _fade = null;
        }

        public void PlayEffect(AudioClip clip, float volume = 1f) => _effects.PlayOneShot(clip, volume);
        public void StopEffects() => _effects.Stop();

        public void StopMusic()
        {
            CancelFade();
            _music.Stop();
            _outgoing.Stop();
        }

        private void CancelFade()
        {
            if (_fade == null) return;
            StopCoroutine(_fade);
            _fade = null;
        }

        private void OnDisable()
        {
            StopMusic();
            StopEffects();
        }
    }
}
