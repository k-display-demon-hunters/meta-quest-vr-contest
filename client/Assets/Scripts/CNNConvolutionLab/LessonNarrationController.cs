using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    // The clip list saved in the scene follows this order.
    public enum NarrationCue
    {
        PlaceFilter,
        FilterPlaced,
        MultiplyIntro,
        MultiplyFirstCell,
        MultiplyCenterCell,
        MultiplyDone,
        ChannelSum,
        GreenChannel,
        BlueChannel,
        RgbSum,
        FirstOutput,
        Checkpoint,
        MoveFilter,
        Quiz,
        QuizWrong,
        QuizCorrect,
        AutoIntro,
        NextRow,
        AllDone,
        Summary,
        Finished
    }

    // Speaks one lesson line at a time. The scene is preloaded before the player enters the room,
    // so a line requested while nobody is near is held and spoken when the listener arrives.
    public sealed class LessonNarrationController : MonoBehaviour
    {
        [SerializeField] private AudioSource _voice;
        [SerializeField] private AudioClip[] _clips;
        [SerializeField, Min(0f)] private float _listenerRange = 9f;
        private AudioClip _held;
        private bool _paused;

        public void Play(NarrationCue cue)
        {
            _voice.Stop();
            _held = _clips[(int)cue];
        }

        public IEnumerator WaitUntilDone()
        {
            while (_held != null || _paused || _voice.isPlaying) { yield return null; }
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (paused) { _voice.Pause(); }
            else { _voice.UnPause(); }
        }

        public void Stop()
        {
            _held = null;
            _voice.Stop();
        }

        private void Update()
        {
            if (_held == null || _paused || !IsListenerInRange()) { return; }
            _voice.clip = _held;
            _held = null;
            _voice.Play();
        }

        private bool IsListenerInRange()
        {
            // A standalone run of this scene has no player rig, so it speaks immediately.
            Camera listener = Camera.main;
            return listener == null
                || (listener.transform.position - transform.position).sqrMagnitude <= _listenerRange * _listenerRange;
        }
    }
}
