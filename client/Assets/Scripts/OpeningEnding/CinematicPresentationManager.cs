using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cinematic
{
    public sealed class CinematicPresentationManager : MonoBehaviour
    {
        [SerializeField] private CinematicWorldController _world;
        [SerializeField] private CinematicAudioController _audio;
        [SerializeField] private CinematicTypeController _type;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _restartButton;
        private CinematicText _content;
        private bool _ending;
        public event Action<float, float> HapticRequested;
        public event Action StartRequested;
        public event Action RestartRequested;

        private void OnEnable()
        {
            _world.Impact += Impact;
            _world.Recognized += Recognized;
            _type.VoiceRequested += _audio.Voice;
            _type.CharacterTyped += _audio.Type;
        }
        public void Bind(CinematicText content, CinematicModelResult model, bool ending, Camera camera)
        {
            _content = content; _ending = ending;
            _audio.Bind(content, ending);
            _world.Bind(content, model, ending, camera.transform);
            _type.Bind(ending ? content.Ending : content.Opening, ending);
            if (ending)
            {
                _restartButton.GetComponentInParent<Canvas>(true).worldCamera = camera;
                _restartButton.onClick.AddListener(RequestRestart);
            }
            else
            {
                _startButton.GetComponentInParent<Canvas>().worldCamera = camera;
                _startButton.onClick.AddListener(RequestStart);
                _world.Evaluate(0);
            }
        }

        private void RequestStart() => StartRequested?.Invoke();
        private void RequestRestart() => RestartRequested?.Invoke();

        public void BeginOpening()
        {
            _startButton.onClick.RemoveListener(RequestStart);
            _startButton.GetComponentInParent<Canvas>().gameObject.SetActive(false);
            _world.PrepareOpeningMedia();
            _audio.BeginOpening();
        }
        public void Evaluate(float time, float held)
        {
            _audio.Evaluate(time);
            _world.Evaluate(time);
            _type.Evaluate(time, !_ending && time >= OpeningTiming.Arrival && time < OpeningTiming.Digit);
            _type.SetHoldProgress(held);
        }
        public void Complete()
        {
            _audio.StopAll(); _world.Finish();
            _type.ClearLine();
            _type.SetHoldProgress(0);
            if (_ending) { _restartButton.GetComponentInParent<Canvas>(true).gameObject.SetActive(true); }
        }
        public void ShowFailure() { _type.ShowMessage(_content.LoadFailure); }
        public void SetCompletedHold(float held) { _type.SetHoldProgress(held); }
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
            if (_startButton != null) { _startButton.onClick.RemoveListener(RequestStart); }
            if (_restartButton != null) { _restartButton.onClick.RemoveListener(RequestRestart); }
            _type.VoiceRequested -= _audio.Voice; _type.CharacterTyped -= _audio.Type;
        }
    }
}
