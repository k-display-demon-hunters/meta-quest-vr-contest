using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNFeatureMaps
{
    // Supply the owned colliders before XRI Awake/OnEnable can claim child pixels.
    public sealed class CNNFeatureMapGrabInteractable : XRGrabInteractable
    {
        protected override void Awake()
        {
            CNNFeatureMapGrabTarget target = GetComponent<CNNFeatureMapGrabTarget>();
            if (target == null || target.GrabColliders == null || target.GrabColliders.Length == 0)
            {
                Debug.LogError("Feature map grab target requires explicit colliders.", this);
                enabled = false;
                return;
            }
            colliders.Clear();
            foreach (Collider collider in target.GrabColliders)
            {
                if (collider != null) { colliders.Add(collider); }
            }
            if (colliders.Count == 0)
            {
                enabled = false;
                return;
            }
            base.Awake();
        }
    }
}
