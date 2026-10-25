using UnityEngine;
using UnityEngine.XR;

namespace XR
{
    // Displays input only. Gameplay continues through the existing OpenXR/XRI inputs.
    public sealed class TouchPlusHandController : MonoBehaviour
    {
        private bool _left;
        private Animator _animator;
        private Material[] _materials;
        private int _cue = -1;
        private bool _cueArmed;
        private readonly float[] _values = new float[7];
        private readonly bool[] _hasParameter = new bool[7];
        private Transform _marker;
        private Material _markerMaterial;
        private static readonly string[] Parameters = { "Button 1", "Button 2", "Button 3", "Grip", "Trigger", "Joy X", "Joy Y" };
        public bool CuePerformed { get; private set; }

        public void Initialize(bool left, RuntimeAnimatorController animations)
        {
            _left = left;
            _animator = GetComponent<Animator>();
            if (_animator == null) { _animator = gameObject.AddComponent<Animator>(); }
            _animator.runtimeAnimatorController = animations;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                for (int i = 0; i < Parameters.Length; i++) { if (parameter.name == Parameters[i]) { _hasParameter[i] = true; } }
            }
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Keep the package materials untouched; adapt their textures to the project's URP renderer.
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            var materials = new System.Collections.Generic.List<Material>();
            foreach (Renderer renderer in renderers)
            {
                Material[] source = renderer.sharedMaterials;
                for (int i = 0; i < source.Length; i++)
                {
                    Material original = source[i];
                    Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (original != null)
                    {
                        material.mainTexture = original.mainTexture;
                        if (original.HasProperty("_Color")) { material.color = original.color; }
                    }
                    source[i] = material;
                    materials.Add(material);
                }
                renderer.sharedMaterials = source;
            }
            _materials = materials.ToArray();
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
            foreach (Transform node in GetComponentsInChildren<Transform>(true))
            {
                if (node.name != part) { continue; }
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(marker.GetComponent<Collider>());
                _marker = marker.transform;
                _marker.SetParent(node, false);
                // The imported model uses centimetres; keep the cue visible at hand scale
                // and let the tutorial parent enlarge it together with the controller.
                float modelUnitsPerMetre = 1f / Mathf.Max(Mathf.Abs(transform.localScale.x), 0.0001f);
                _marker.localPosition = Vector3.up * (0.008f * modelUnitsPerMetre);
                _marker.localScale = Vector3.one * (0.012f * modelUnitsPerMetre);
                if (_markerMaterial == null) { _markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); }
                marker.GetComponent<Renderer>().sharedMaterial = _markerMaterial;
                break;
            }
        }

        private void Update()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(_left ? XRNode.LeftHand : XRNode.RightHand);
            device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary);
            device.TryGetFeatureValue(CommonUsages.menuButton, out bool menu);
            device.TryGetFeatureValue(CommonUsages.grip, out float grip);
            device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick);
            float[] values = _values;
            values[0] = primary ? 1 : 0; values[1] = secondary ? 1 : 0; values[2] = menu ? 1 : 0;
            values[3] = grip; values[4] = trigger; values[5] = stick.x; values[6] = stick.y;
            for (int i = 0; i < Parameters.Length; i++) { if (_hasParameter[i]) { _animator.SetFloat(Parameters[i], values[i]); } }
            if (_cue < 0) { return; }
            bool performed = _cue >= 5 ? stick.magnitude > 0.45f : values[_cue] > 0.55f;
            if (!performed) { _cueArmed = true; }
            CuePerformed |= _cueArmed && performed;
            if (_markerMaterial != null) { _markerMaterial.color = performed ? Color.green : new Color(1f, 0.75f, 0.15f); }
        }

        private void OnDestroy()
        {
            if (_materials != null) { foreach (Material material in _materials) { Destroy(material); } }
            if (_markerMaterial != null) { Destroy(_markerMaterial); }
        }
    }
}
