using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CNNFeatureMaps
{
    public sealed class CNNFeatureMapScene : MonoBehaviour
    {
        [SerializeField] private Texture2D _inputImage;
        [SerializeField] private CNNFeatureMapNode[] _nodes;
        [SerializeField] private Renderer[] _outputs;
        private readonly List<CNNFeatureMapGrabTarget> _heldTargets = new List<CNNFeatureMapGrabTarget>();
        private CNNFeatureMapGrabTarget[] _targets;
        private CNNFeatureMapLayer[] _layers;
        private CNNFeatureMapExampleData _data;
        private CNNFeatureMapGrabTarget _hoverTarget;
        private InputAction _zoomIn;
        private InputAction _zoomOut;
        private InputAction _pixelClick;
        private InputAction _reset;
        private Text[] _tabletTexts;
        private bool[] _tabletTextStates;
        private bool _ready;
        private CNNFeatureMapGuide _guide;
        public bool HasHeldTargets => _heldTargets.Count > 0;
        public CNNFeatureMapExampleData Data => _data;

        private void Awake()
        {
            _zoomIn = new InputAction("Enlarge held CNN item", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
            _zoomOut = new InputAction("Shrink held CNN item", InputActionType.Button, "<XRController>{RightHand}/secondaryButton");
            _pixelClick = new InputAction("Choose input pixel", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            _reset = new InputAction("Restore CNN layout", InputActionType.Button, "<XRController>{RightHand}/primary2DAxisClick");
        }

        private void OnEnable()
        {
            _zoomIn.Enable();
            _zoomOut.Enable();
            _pixelClick.Enable();
            _reset.Enable();
        }

        private void Start()
        {
            _targets = GetComponentsInChildren<CNNFeatureMapGrabTarget>();
            _layers = GetComponentsInChildren<CNNFeatureMapLayer>();
            if (_inputImage == null || !_inputImage.isReadable || _inputImage.width != 8 || _inputImage.height != 8)
            {
                Debug.LogError("CNNFeatureMapScene requires the readable 8 x 8 sample image.", this);
                return;
            }
            _data = new CNNFeatureMapExampleData(_inputImage);
            _ready = true;
            _guide = GetComponent<CNNFeatureMapGuide>();
            RefreshAppearance(false);
            // Hide existing tablet words only while this content scene is active.
            XR.LeftHandTablet tablet = Object.FindFirstObjectByType<XR.LeftHandTablet>();
            if (tablet != null)
            {
                _tabletTexts = tablet.GetComponentsInChildren<Text>(true);
                _tabletTextStates = new bool[_tabletTexts.Length];
                for (int index = 0; index < _tabletTexts.Length; index++)
                {
                    _tabletTextStates[index] = _tabletTexts[index].enabled;
                    _tabletTexts[index].enabled = false;
                }
            }
            if (_guide != null)
            {
                _guide.Initialize(this, _nodes);
            }
        }

        public void RefreshLearningAppearance()
        {
            RefreshAppearance(false);
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }
            if (_heldTargets.Count > 0)
            {
                CNNFeatureMapGrabTarget target = _heldTargets[_heldTargets.Count - 1];
                float direction = (_zoomIn.IsPressed() ? 1f : 0f) - (_zoomOut.IsPressed() ? 1f : 0f);
                if (direction != 0f)
                {
                    target.ScaleBy(Mathf.Exp(direction * Time.deltaTime));
                }
            }
            if (_pixelClick.WasPressedThisFrame() && _hoverTarget != null)
            {
                CNNFeatureMapNode node = _hoverTarget.GetComponent<CNNFeatureMapNode>();
                if (node != null)
                {
                    node.SelectPixel();
                }
            }
            if (_reset.WasPressedThisFrame())
            {
                ResetLayout();
            }
        }

        public void SelectPixel(int row, int column)
        {
            if (!_ready)
            {
                return;
            }
            if (_guide != null && _guide.HandleInputSelection(row, column))
            {
                return;
            }
            _data.SelectPixel(row, column);
            RefreshAppearance(true);
        }

        public bool CanGrab(CNNFeatureMapGrabTarget candidate)
        {
            foreach (CNNFeatureMapGrabTarget held in _heldTargets)
            {
                if (held != candidate && (held.transform.IsChildOf(candidate.transform) || candidate.transform.IsChildOf(held.transform)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasHeldDescendant(Transform ancestor)
        {
            foreach (CNNFeatureMapGrabTarget held in _heldTargets)
            {
                if (held.transform.IsChildOf(ancestor))
                {
                    return true;
                }
            }
            return false;
        }

        public void HoldTarget(CNNFeatureMapGrabTarget target)
        {
            if (!_heldTargets.Contains(target))
            {
                _heldTargets.Add(target);
            }
        }

        public void ReleaseTarget(CNNFeatureMapGrabTarget target) => _heldTargets.Remove(target);

        public void SetHoverTarget(CNNFeatureMapGrabTarget target, bool hovering)
        {
            if (hovering)
            {
                _hoverTarget = target;
            }
            else if (_hoverTarget == target)
            {
                _hoverTarget = null;
            }
        }

        public void ResetLayout()
        {
            if (!_ready || _heldTargets.Count > 0)
            {
                return;
            }
            foreach (CNNFeatureMapLayer layer in _layers)
            {
                layer.Collapse();
            }
            foreach (CNNFeatureMapGrabTarget target in _targets)
            {
                target.ResetPose();
            }
            _data.SelectPixel(-1, -1);
            RefreshAppearance(false);
            if (_guide != null)
            {
                _guide.RestoreCurrentLessonLayout();
            }
        }

        public void RefreshMaps()
        {
            if (!_ready)
            {
                return;
            }
            foreach (CNNFeatureMapNode node in _nodes)
            {
                node.RefreshAppearance(_data);
            }
        }

        private void RefreshAppearance(bool hasSelection)
        {
            RefreshMaps();
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            for (int index = 0; index < _outputs.Length; index++)
            {
                float value = _data.ReadOutput(index);
                _outputs[index].transform.localScale = new Vector3(.12f, .1f + value * .5f, .06f);
                properties.SetColor("_BaseColor", hasSelection ? new Color(1f, .83f, .22f) : new Color(.15f, .35f + value * .5f, .55f));
                _outputs[index].SetPropertyBlock(properties);
            }
        }

        private void OnDisable()
        {
            _zoomIn.Disable();
            _zoomOut.Disable();
            _pixelClick.Disable();
            _reset.Disable();
        }

        private void OnDestroy()
        {
            if (_tabletTexts != null)
            {
                for (int index = 0; index < _tabletTexts.Length; index++)
                {
                    if (_tabletTexts[index] != null)
                    {
                        _tabletTexts[index].enabled = _tabletTextStates[index];
                    }
                }
            }
            _zoomIn.Dispose();
            _zoomOut.Dispose();
            _pixelClick.Dispose();
            _reset.Dispose();
        }
    }
}

