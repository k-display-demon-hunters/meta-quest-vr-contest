using UnityEngine;

namespace Cinematic
{
    internal readonly struct EndingFocusTarget
    {
        internal readonly Vector3 Position;
        internal readonly Vector2 Size;

        internal EndingFocusTarget(Vector3 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }
    }
}
