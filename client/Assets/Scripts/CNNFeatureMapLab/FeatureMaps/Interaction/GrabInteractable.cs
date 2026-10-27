using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNFeatureMaps.FeatureMaps.Interaction
{
    // Supply the owned colliders before XRI Awake/OnEnable can claim child pixels.
    public sealed class GrabInteractable : XRGrabInteractable
    {
        protected override void Awake()
        {
            GrabTarget target = GetComponent<GrabTarget>();
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
