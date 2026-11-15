using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    internal enum FeatureMapVoice
    {
        Entry, RgbStairs, FirstFilter, PoolStairs, Pooling, SecondStairs,
        SecondFilter, AdditionalFilters, Probabilities, Answer
    }

    [Serializable]
    internal sealed class FeatureMapNarration
    {
        [SerializeField] private AudioClip[] _clips;
        private AudioSource _source;
        private FeatureMapVoice? _current;
        private AudioClip _pendingAnswer;
        private bool _pendingPooling;
        private int _spoken;
        private int _section;

        internal void Initialize(GameObject owner)
        {
            _source = owner.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        internal void SetSection(int section)
        {
            if (_section == section) { return; }
            Stop();
            _section = section;
        }

        internal void Speak(FeatureMapVoice cue)
        {
            if (MarkFirst(cue)) { Play(cue, _clips[(int)cue]); }
        }

        internal void BeginPooling()
        {
            if (!MarkFirst(FeatureMapVoice.PoolStairs)) { return; }
            Play(FeatureMapVoice.PoolStairs, _clips[(int)FeatureMapVoice.PoolStairs]);
            _pendingPooling = true;
        }

        internal void SpeakAnswer(int winner)
        {
            if (winner is < 1 or > 3 || !MarkFirst(FeatureMapVoice.Answer)) { return; }
            AudioClip answer = _clips[(int)FeatureMapVoice.Answer + winner - 1];
            if (_current == FeatureMapVoice.Probabilities && _source.isPlaying)
            {
                _pendingAnswer = answer;
                return;
            }
            Play(FeatureMapVoice.Answer, answer);
        }

        internal void Tick(bool inPooling, bool answerVisible)
        {
            if (!inPooling) { _pendingPooling = false; }
            if (!answerVisible) { _pendingAnswer = null; }
            if (_source.isPlaying) { return; }
            if (_pendingPooling)
            {
                _pendingPooling = false;
                Speak(FeatureMapVoice.Pooling);
            }
            else if (_pendingAnswer != null)
            {
                AudioClip answer = _pendingAnswer;
                _pendingAnswer = null;
                Play(FeatureMapVoice.Answer, answer);
            }
        }

        internal void Stop()
        {
            _source.Stop();
            _current = null;
            _pendingPooling = false;
            _pendingAnswer = null;
        }

        private bool MarkFirst(FeatureMapVoice cue)
        {
            int mask = 1 << (int)cue;
            if ((_spoken & mask) != 0) { return false; }
            _spoken |= mask;
            return true;
        }

        private void Play(FeatureMapVoice cue, AudioClip clip)
        {
            _source.Stop();
            _current = cue;
            _source.clip = clip;
            _source.Play();
        }
    }
}
