using System.Collections.Generic;
using UnityEngine;

namespace Cinematic
{
    [CreateAssetMenu(menuName = "CNN/Cinematic model result")]
    public sealed class CinematicModelResult : ScriptableObject
    {
        // Session-only display data; never serializes trained weights or changes model JSON.
        [System.NonSerialized] private string[] _layers = System.Array.Empty<string>();
        [System.NonSerialized] private double? _testAccuracy;

        public IReadOnlyList<string> Layers => _layers;
        public double? TestAccuracy => _testAccuracy;

        public void Record(IReadOnlyList<string> layers) => Record(layers, null);

        public void Record(IReadOnlyList<string> layers, double? testAccuracy)
        {
            _layers = new string[layers.Count];
            for (int i = 0; i < layers.Count; i++) { _layers[i] = layers[i]; }
            _testAccuracy = testAccuracy is >= 0d and <= 1d ? testAccuracy : null;
        }

        public void Clear()
        {
            _layers = System.Array.Empty<string>();
            _testAccuracy = null;
        }
    }
}
