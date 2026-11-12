using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class LessonPlayback
    {
        private const float AnimationSpeedMultiplier = 1.5f;

        public bool IsPaused { get; set; }
        public float Speed { get; set; } = 1f;
        public float DeltaTime => IsPaused ? 0f : Time.unscaledDeltaTime * AnimationSpeedMultiplier * Speed;

        public IEnumerator Wait(float seconds)
        {
            while (seconds > 0f || IsPaused)
            {
                yield return null;
                seconds -= DeltaTime;
            }
        }
    }
}
