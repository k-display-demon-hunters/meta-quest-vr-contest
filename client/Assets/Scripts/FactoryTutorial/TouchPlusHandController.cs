using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace FactoryTutorial
{
    internal enum TutorialControl
    {
        PrimaryButton,
        SecondaryButton,
        MenuButton,
        Grip,
        Trigger,
        Stick
    }

    // Highlights controls and recognizes the requested practice input.
    public sealed class TouchPlusHandController : MonoBehaviour
    {
        private sealed class CueMarker
        {
            public readonly TutorialControl Control;
            public readonly Renderer Marker;
            public bool Armed;

            public CueMarker(TutorialControl control, Renderer marker)
            {
                Control = control;
                Marker = marker;
            }
        }

        private bool _left;
        private Transform _model;
        private readonly List<CueMarker> _cues = new List<CueMarker>();
        private Material _idleMarkerMaterial;
        private Material _pressedMarkerMaterial;

        public bool CuePerformed { get; private set; }

        public void Initialize(bool left, Transform model)
        {
            _left = left;
            _model = model;
        }

        internal void SetCues(TutorialControl[] controls)
        {
            CuePerformed = false;
            foreach (CueMarker cue in _cues)
            {
                if (cue.Marker != null) { Destroy(cue.Marker.gameObject); }
            }
            _cues.Clear();
            if (controls.Length == 0) { return; }

            Transform[] nodes = _model.GetComponentsInChildren<Transform>(true);
            foreach (TutorialControl control in controls)
            {
                _cues.Add(new CueMarker(control, AttachMarker(control, nodes)));
            }
        }

        private Renderer AttachMarker(TutorialControl control, Transform[] nodes)
        {
            string part = control switch
            {
                TutorialControl.PrimaryButton => _left ? "b_button_x" : "b_button_a",
                TutorialControl.SecondaryButton => _left ? "b_button_y" : "b_button_b",
                TutorialControl.MenuButton => _left ? "left_b_button_oculus" : "right_b_button_oculus",
                TutorialControl.Grip => _left ? "left_b_trigger_grip" : "right_b_trigger_grip",
                TutorialControl.Trigger => _left ? "left_b_trigger_front" : "right_b_trigger_front",
                _ => _left ? "left_b_thumbstick" : "right_b_thumbstick"
            };
            foreach (Transform node in nodes)
            {
                if (node.name != part) { continue; }
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(marker.GetComponent<Collider>());
                Transform markerTransform = marker.transform;
                markerTransform.SetParent(node, false);
                // The imported model uses centimetres; keep the cue visible at hand scale
                // and let the tutorial parent enlarge it together with the controller.
                float modelUnitsPerMetre = Mathf.Abs(transform.lossyScale.x / node.lossyScale.x);
                markerTransform.localPosition = Vector3.up * (0.008f * modelUnitsPerMetre);
                markerTransform.localScale = Vector3.one * (0.012f * modelUnitsPerMetre);
                if (_idleMarkerMaterial == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                    _idleMarkerMaterial = new Material(shader) { color = new Color(1f, 0.75f, 0.15f) };
                    _pressedMarkerMaterial = new Material(shader) { color = Color.green };
                }
                Renderer renderer = marker.GetComponent<Renderer>();
                renderer.sharedMaterial = _idleMarkerMaterial;
                return renderer;
            }

            return null;
        }

        private void Update()
        {
            if (_cues.Count == 0) { return; }

            InputDevice device = InputDevices.GetDeviceAtXRNode(_left ? XRNode.LeftHand : XRNode.RightHand);
            foreach (CueMarker cue in _cues)
            {
                bool performed = cue.Control switch
                {
                    TutorialControl.PrimaryButton => device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary,
                    TutorialControl.SecondaryButton => device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary,
                    TutorialControl.MenuButton => device.TryGetFeatureValue(CommonUsages.menuButton, out bool menu) && menu,
                    TutorialControl.Grip => device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip > 0.55f,
                    TutorialControl.Trigger => device.TryGetFeatureValue(CommonUsages.trigger, out float trigger) && trigger > 0.55f,
                    _ => device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick) && stick.magnitude > 0.45f,
                };
                if (!performed) { cue.Armed = true; }
                CuePerformed |= cue.Armed && performed;
                if (cue.Marker != null)
                {
                    cue.Marker.sharedMaterial = performed ? _pressedMarkerMaterial : _idleMarkerMaterial;
                }
            }
        }

        private void OnDestroy()
        {
            Destroy(_idleMarkerMaterial);
            Destroy(_pressedMarkerMaterial);
        }
    }
}
