using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace FeatureMapCorridor
{
    // Desktop-only companion to TrackedDeviceGraphicRaycaster, never a second XR input path.
    public sealed class CorridorMouseRaycaster : GraphicRaycaster
    {
        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
#if UNITY_EDITOR
            if (!UnityEngine.XR.XRSettings.isDeviceActive && !(eventData is TrackedDeviceEventData))
            base.Raycast(eventData, results);
#endif
        }
    }
}
