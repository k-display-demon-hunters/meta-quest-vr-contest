using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class ZoneSelectionController
    {
        [SerializeField] private XRSimpleInteractable[] _zoneHits;

        internal void Bind(Action<XRSimpleInteractable, int> bind)
        {
            for (int zone = 0; zone < _zoneHits.Length; zone++)
            {
                bind(_zoneHits[zone], zone);
            }
        }
    }
}
