using System;
using UnityEngine;

namespace Cinematic
{
    public sealed class CinematicPresentationManager : MonoBehaviour
    {
        [SerializeField] private CinematicWorldController _world;
        [SerializeField] private CinematicAudioController _audio;
        [SerializeField] private CinematicTypeController _type;
        private CinematicText _content;
        private bool _ending;
        public event Action<float, float> HapticRequested;

        private void OnEnable()
        {
            _world.Impact += Impact;
            _world.Recognized += Recognized;
            _type.VoiceRequested += _audio.Voice;
            _type.CharacterTyped += _audio.Type;
        }
        public void Bind(CinematicText content, CinematicModelResult model, bool ending, Transform head)
        {
            _content = content; _ending = ending;
            _audio.Bind(content, ending);
            _world.Bind(content, model, ending, head);
            _type.Bind(ending ? content.Ending : content.Opening, ending);
        }
        public void Evaluate(float time, float held)
        {
            _audio.Evaluate(time);
            _world.Evaluate(time);
            _type.Evaluate(time, !_ending && time >= 7 && time < 15);
            _type.SetHint(_content.Skip, held);
        }
        public void Complete(bool credits)
        {
            _audio.StopAll(); _world.Finish();
            _type.ShowCredits(credits ? _content.Credits + "\n\n" + _content.AudioCredits : "");
            _type.SetHint(credits ? _content.Replay : "", 0);
        }
        public void ShowFailure() { _type.ShowCredits(_content.LoadFailure); }
        public void SetCompletedHold(float held) { _type.SetHint(_content.Replay, held); }
        private void Impact(bool big)
        {
            _audio.Hit(big);
            if (big) { HapticRequested?.Invoke(.5f, .12f); }
        }
        private void Recognized(Vector3 location)
        { _audio.Beep(location); HapticRequested?.Invoke(.3f, .04f); }
        private void OnDisable()
        {
            _world.Impact -= Impact; _world.Recognized -= Recognized;
            _type.VoiceRequested -= _audio.Voice; _type.CharacterTyped -= _audio.Type;
        }
    }
}
