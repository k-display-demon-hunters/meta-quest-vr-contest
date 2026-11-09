using UnityEngine;
using UnityEngine.XR;

namespace FactoryTutorial
{
    // Highlights controls and recognizes the requested practice input.
    public sealed class TouchPlusHandController : MonoBehaviour
    {
        private bool _left;
        private Transform _model;
        private int _cue = -1;
        private bool _cueArmed;
        private Transform _marker;
        private Material _markerMaterial;

        public bool CuePerformed { get; private set; }

        public void Initialize(bool left, Transform model)
        {
            _left = left;
            _model = model;
        }

        public void SetCue(int cue)
        {
            _cue = cue;
            CuePerformed = false;
            _cueArmed = false;
            if (_marker != null) { Destroy(_marker.gameObject); _marker = null; }
            if (cue < 0) { return; }
            string part = cue == 0 ? (_left ? "b_button_x" : "b_button_a")
                : cue == 1 ? (_left ? "b_button_y" : "b_button_b")
                : cue == 2 ? (_left ? "left_b_button_oculus" : "right_b_button_oculus")
                : cue == 3 ? (_left ? "left_b_trigger_grip" : "right_b_trigger_grip")
                : cue == 4 ? (_left ? "left_b_trigger_front" : "right_b_trigger_front")
                : (_left ? "left_b_thumbstick" : "right_b_thumbstick");
            foreach (Transform node in _model.GetComponentsInChildren<Transform>(true))
            {
                if (node.name != part) { continue; }
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(marker.GetComponent<Collider>());
                _marker = marker.transform;
                _marker.SetParent(node, false);
                // The imported model uses centimetres; keep the cue visible at hand scale
                // and let the tutorial parent enlarge it together with the controller.
                float modelUnitsPerMetre = Mathf.Abs(transform.lossyScale.x / node.lossyScale.x);
                _marker.localPosition = Vector3.up * (0.008f * modelUnitsPerMetre);
                _marker.localScale = Vector3.one * (0.012f * modelUnitsPerMetre);
                if (_markerMaterial == null) { _markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); }
                marker.GetComponent<Renderer>().sharedMaterial = _markerMaterial;
                break;
            }
        }

        private void Update()
        {
            if (_cue < 0) { return; }

            InputDevice device = InputDevices.GetDeviceAtXRNode(_left ? XRNode.LeftHand : XRNode.RightHand);
            bool performed = _cue switch
            {
                0 => device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary,
                1 => device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary,
                2 => device.TryGetFeatureValue(CommonUsages.menuButton, out bool menu) && menu,
                3 => device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > 0.55f,
                4 => device.TryGetFeatureValue(CommonUsages.trigger, out float trigger) && trigger > 0.55f,
                _ => device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick) && stick.magnitude > 0.45f,
            };
            if (!performed) { _cueArmed = true; }
            CuePerformed |= _cueArmed && performed;
            if (_markerMaterial != null) { _markerMaterial.color = performed ? Color.green : new Color(1f, 0.75f, 0.15f); }
        }

        private void OnDestroy()
        {
            if (_markerMaterial != null) { Destroy(_markerMaterial); }
        }
    }
}
