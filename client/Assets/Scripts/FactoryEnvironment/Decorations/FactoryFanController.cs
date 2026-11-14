using UnityEngine;

namespace FactoryEnvironment
{
    [DisallowMultipleComponent]
    public sealed class FactoryFanController : MonoBehaviour
    {
        private const float CycleSeconds = 44f;
        private const float SecondStart = 21.5f;

        [SerializeField] private AudioSource _bearingAudio;
        [SerializeField] private AudioClip _bearingSqueak;
        [SerializeField, Range(0f, CycleSeconds)] private float _startOffset;

        private float _phase;

        private void OnEnable()
        {
            _phase = _startOffset;
        }

        private void Update()
        {
            float previousPhase = _phase;
            _phase = Mathf.Repeat(_phase + Time.deltaTime, CycleSeconds);

            float flutter = 1f + 0.11f * Mathf.Sin(_phase * 25f);
            float rpm = BearingSpeed(_phase) * flutter;
            transform.Rotate(Vector3.forward, rpm * 6f * Time.deltaTime, Space.Self);

            if (PassedCue(previousPhase, _phase, 0.82f))
                Squeak(1.07f);
            else if (PassedCue(previousPhase, _phase, SecondStart + 0.15f))
                Squeak(0.91f);
        }

        private static float BearingSpeed(float phase)
        {
            if (phase < 0.65f)
                return 0f;
            if (phase < 1.4f)
                return Mathf.SmoothStep(0f, 61f, (phase - 0.65f) / 0.75f);
            if (phase < 3.7f)
                return Mathf.SmoothStep(61f, 0f, (phase - 1.4f) / 2.3f);

            float secondPhase = phase - SecondStart;
            if (secondPhase < 0f || secondPhase >= 4.9f)
                return 0f;
            if (secondPhase < 0.6f)
                return Mathf.SmoothStep(0f, 75f, secondPhase / 0.6f);
            return Mathf.SmoothStep(75f, 0f, (secondPhase - 0.6f) / 4.3f);
        }

        private static bool PassedCue(float previous, float current, float cue)
        {
            return current >= previous
                ? previous < cue && current >= cue
                : previous < cue || current >= cue;
        }

        private void Squeak(float pitch)
        {
            _bearingAudio.pitch = pitch;
            _bearingAudio.PlayOneShot(_bearingSqueak);
        }

        private void OnDisable()
        {
            _bearingAudio.Stop();
        }
    }
}
