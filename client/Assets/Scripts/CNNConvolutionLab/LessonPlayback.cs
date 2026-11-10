using System.Collections;
using UnityEngine;

namespace CNNConvolution
{
    internal sealed class LessonPlayback
    {
        public bool IsPaused { get; set; }
        public float Speed { get; set; } = 1f;
        public float DeltaTime => IsPaused ? 0f : Time.unscaledDeltaTime * Speed;

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
