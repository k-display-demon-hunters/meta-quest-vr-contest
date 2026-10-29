using UnityEngine;
using Unity.XR.CoreUtils;

namespace Tablet
{
    // Keep the tablet attached to the hand, without requiring wrist rotation to read it.
    [DisallowMultipleComponent]
    public sealed class TabletFacingController : MonoBehaviour
    {
        private XROrigin _origin;

        private void Awake()
        {
            _origin = GetComponentInParent<XROrigin>();
        }

        private void LateUpdate()
        {
            if (_origin == null || _origin.Camera == null) { return; }

            // The canvas is on the tablet's local -Z face, so +Z points away from the viewer.
            Vector3 forward = transform.position - _origin.Camera.transform.position;
            if (forward.sqrMagnitude < 0.0025f) { return; }
            forward.Normalize();
            Vector3 up = Vector3.ProjectOnPlane(_origin.transform.up, forward);
            if (up.sqrMagnitude < 0.01f)
            {
                up = Vector3.ProjectOnPlane(_origin.Camera.transform.up, forward);
            }
            if (up.sqrMagnitude < 0.0001f) { return; }
            transform.rotation = Quaternion.LookRotation(forward, up.normalized);
        }
    }
}
