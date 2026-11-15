using System;
using UnityEngine;

namespace ModelDesignScene
{
    [Serializable]
    internal sealed class ModelDesignNarration
    {
        [SerializeField] private AudioClip _entry;
        [SerializeField] private AudioClip _reality;
        [SerializeField] private AudioClip _missingFlatten;
        [SerializeField] private AudioClip _extraFlatten;
        [SerializeField] private AudioClip _spatialAfterFlatten;
        [SerializeField] private AudioClip _invalidKernel;
        [SerializeField] private AudioClip _invalidPooling;
        private AudioSource _source;
        private bool _entrySpoken;
        private bool _realitySpoken;
        private bool _speakingError;

        internal void Initialize(GameObject owner)
        {
            _source = owner.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        internal void SpeakEntry()
        {
            if (_entrySpoken) { return; }
            _entrySpoken = true;
            Play(_entry);
        }

        internal void SpeakReality()
        {
            if (_realitySpoken) { return; }
            _realitySpoken = true;
            Play(_reality);
        }

        internal void SpeakValidationIssue(string message)
        {
            AudioClip clip = message switch
            {
                "Spatial layers require Flatten before Dense or Output." => _missingFlatten,
                "Flatten requires spatial input." => _extraFlatten,
                "Spatial layers cannot follow Dense or Flatten." => _spatialAfterFlatten,
                "Invalid Conv2D shape or placement." => _invalidKernel,
                "Invalid MaxPooling2D shape or placement." => _invalidPooling,
                _ => null,
            };
            if (clip == null) { return; }
            Play(clip);
            _speakingError = true;
        }

        internal void StopError()
        {
            if (_speakingError) { Stop(); }
        }

        internal void Stop()
        {
            _source.Stop();
            _speakingError = false;
        }

        private void Play(AudioClip clip)
        {
            Stop();
            _source.clip = clip;
            _source.Play();
        }
    }
}
