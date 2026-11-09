using System.Collections.Generic;
using UnityEngine;

namespace Cinematic
{
    [CreateAssetMenu(menuName = "CNN/Cinematic model result")]
    public sealed class CinematicModelResult : ScriptableObject
    {
        // Session-only display data; never serializes trained weights or changes model JSON.
        [System.NonSerialized] private string[] _layers = System.Array.Empty<string>();
        public IReadOnlyList<string> Layers => _layers;
        public void Record(IReadOnlyList<string> layers)
        {
            _layers = new string[layers.Count];
            for (int i = 0; i < layers.Count; i++) { _layers[i] = layers[i]; }
        }
        public void Clear() { _layers = System.Array.Empty<string>(); }
    }
}
