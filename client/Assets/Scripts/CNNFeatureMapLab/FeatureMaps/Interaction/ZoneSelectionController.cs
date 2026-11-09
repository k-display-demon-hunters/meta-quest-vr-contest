using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class ZoneSelectionController
    {
        [SerializeField] private Collider[] _zoneHits;
        internal event Action<int> Selected;
        internal void Bind(InteractionManager interaction)
        {
            for (int i = 0;i<_zoneHits.Length;i++)
            {
                int zone = i;
                interaction.Register(_zoneHits[i], _ => Selected?.Invoke(zone));
            }
        }
    }
}
