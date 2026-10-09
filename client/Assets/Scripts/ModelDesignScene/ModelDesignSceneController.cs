using System.Collections.Generic;
using ModelDesign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ModelDesignScene
{
    public sealed class ModelDesignSceneController : MonoBehaviour
    {
        [SerializeField] private ModelBlockView _blockPrefab;
        [SerializeField] private BlockSettingsPopup _popupPrefab;
        [SerializeField] private ModelBlockView _inputBlock;
        [SerializeField] private ModelBlockView _outputBlock;
        [SerializeField] private Transform _paletteRoot;
        [SerializeField] private PaletteBlock[] _paletteBlocks;
        [SerializeField, Min(0.01f)] private float _snapDistance = 0.14f;

        private readonly Dictionary<LayerDefinition, ModelBlockView> _blocks = new Dictionary<LayerDefinition, ModelBlockView>();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly List<ModelBlockView> _movingBlocks = new List<ModelBlockView>();
        private readonly Dictionary<ModelBlockView, Pose> _paletteSlots = new Dictionary<ModelBlockView, Pose>();
        private ModelWorkspace _workspace;
        private BlockSettingsPopup _popup;
        private ModelBlockView _grabbedBlock;
        private ModelBlockView _settingsBlock;
        private ModelBlockView _hoveredPortBlock;
        private Connection _candidate;
        private XRRayInteractor _rightRay;
        private bool _previousUIInteraction;
        private bool _previousHoveredActivate;
        private bool _previousHitClosestOnly;
        private XRInputButtonReader _previousUIPress;
        private XRInputButtonReader _uiPress;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedUIInputModule;

        private void Awake()
        {
            _workspace = new ModelWorkspace();
            _popup = Instantiate(_popupPrefab, transform);
            _popup.Hide();
            _popup.OnDisconnectRequested += DisconnectSelectedBlock;
        }

        private void OnEnable()
        {
            PrepareUIInput();
            BindRightHand();
        }

        private void Start()
        {
            // Scene blocks must finish Awake before binding their model data and appearance.
            InitializeBlock(_inputBlock, _workspace.Input, false);
            InitializeBlock(_outputBlock, _workspace.Output, false);
            foreach (PaletteBlock entry in _paletteBlocks)
            {
                InitializeBlock(entry.Block, CreateDefaultLayer(entry.Type), true);
            }
        }

        private void LateUpdate()
        {
            UpdatePortHover();
            if (_grabbedBlock == null)
            {
                return;
            }
            SetCandidate(FindConnectionCandidate());
        }

        internal void BeginGrab(ModelBlockView block)
        {
            _popup.Hide();
            _settingsBlock = null;
            SetCandidate(null);
            if (block.IsPalette)
            {
                Pose slot = _paletteSlots[block];
                _paletteSlots.Remove(block);
                block.TakeFromPalette();
                _blocks.Add(block.Layer, block);
                _workspace.AddLayer(block.Layer);
                CreatePaletteBlock(block.Layer.Type, slot);
            }

            _grabbedBlock = block;
            _movingBlocks.Clear();
            foreach (LayerDefinition layer in _workspace.GetModel(block.Layer).Layers)
            {
                ModelBlockView moving = _blocks[layer];
                _movingBlocks.Add(moving);
                if (moving != block)
                {
                    // Parenting also follows XRI's pose updates immediately before VR rendering.
                    moving.transform.SetParent(block.transform, true);
                }
            }
        }

        internal void EndGrab(ModelBlockView block, bool cancelled)
        {
            if (_grabbedBlock != block)
            {
                return;
            }
            Connection candidate = cancelled ? null : FindConnectionCandidate();
            SetCandidate(null);
            RestoreBlockParents();
            // Taken blocks belong to the scene, even when the palette moves later.
            block.transform.SetParent(transform, true);
            if (candidate != null)
            {
                // Ports describe presentation only; the existing workspace owns model connectivity.
                _workspace.Connect(candidate.First.Layer, candidate.Second.Layer);
                Vector3 offset = candidate.Second.GetPortPosition(candidate.SecondPort) -
                    candidate.First.GetPortPosition(candidate.FirstPort);
                foreach (ModelBlockView moving in _movingBlocks)
                {
                    moving.Translate(offset);
                }
                _connections.Add(candidate);
                RefreshPorts(candidate);
            }
            _grabbedBlock = null;
            _movingBlocks.Clear();
        }

        internal void ShowSettings(ModelBlockView block)
        {
            if (_grabbedBlock != null || _rightRay == null || _rightRay.hasSelection)
            {
                return;
            }
            _settingsBlock = block;
            Camera camera = Camera.main;
            Vector3 position = block.transform.position + Vector3.up * 0.42f;
            Quaternion rotation = block.transform.rotation;
            if (camera != null)
            {
                position += camera.transform.right * 0.32f - camera.transform.forward * 0.15f;
                rotation = Quaternion.LookRotation(position - camera.transform.position, Vector3.up);
            }
            _popup.Show(block.Layer, position, rotation, _workspace.GetModel(block.Layer).Layers.Count > 1);
        }

        private void InitializeBlock(ModelBlockView block, LayerDefinition layer, bool palette)
        {
            block.name = palette ? layer.Type + " Palette" : layer.Type + " Block";
            block.Initialize(this, layer, palette);
            if (palette)
            {
                _paletteSlots.Add(block, new Pose(block.transform.localPosition, block.transform.localRotation));
            }
            else
            {
                _blocks.Add(layer, block);
            }
        }

        private void CreatePaletteBlock(LayerType type, Pose slot)
        {
            // Store slots relative to the movable palette, not its original world position.
            ModelBlockView block = Instantiate(_blockPrefab, _paletteRoot);
            block.transform.SetLocalPositionAndRotation(slot.position, slot.rotation);
            InitializeBlock(block, CreateDefaultLayer(type), true);
        }

        private void UpdatePortHover()
        {
            ModelBlockView hoveredBlock = null;
            int hoveredPort = -1;
            if (_rightRay != null && _rightRay.isActiveAndEnabled &&
                _rightRay.TryGetCurrentRaycast(out RaycastHit? hit, out _, out _, out _, out bool isUIHitClosest) &&
                !isUIHitClosest && hit.HasValue)
            {
                Collider hitCollider = hit.Value.collider;
                ModelBlockView block = hitCollider != null ? hitCollider.GetComponentInParent<ModelBlockView>() : null;
                if (block != null && block.TryGetPort(hitCollider, out hoveredPort) &&
                    (_paletteSlots.ContainsKey(block) || (block.Layer != null && _blocks.ContainsKey(block.Layer))))
                {
                    hoveredBlock = block;
                }
            }

            if (_hoveredPortBlock != null && _hoveredPortBlock != hoveredBlock)
            {
                _hoveredPortBlock.SetHoveredPort(-1);
            }
            _hoveredPortBlock = hoveredBlock;
            if (_hoveredPortBlock != null)
            {
                _hoveredPortBlock.SetHoveredPort(hoveredPort);
            }
        }

        private static LayerDefinition CreateDefaultLayer(LayerType type)
        {
            switch (type)
            {
                case LayerType.Conv2D: return LayerDefinition.Conv2D(8, 3, 3, ActivationType.ReLU);
                case LayerType.MaxPooling2D: return LayerDefinition.MaxPooling2D(2, 2);
                case LayerType.Flatten: return LayerDefinition.Flatten();
                case LayerType.Dense: return LayerDefinition.Dense(32, ActivationType.ReLU);
                default: throw new System.ArgumentOutOfRangeException(nameof(type));
            }
        }

        private void RestoreBlockParents()
        {
            foreach (ModelBlockView block in _movingBlocks)
            {
                if (block != null && block != _grabbedBlock)
                {
                    block.transform.SetParent(transform, true);
                }
            }
        }

        private Connection FindConnectionCandidate()
        {
            float closestSquared = _snapDistance * _snapDistance;
            ModelBlockView closestMoving = null;
            ModelBlockView closestTarget = null;
            int closestPort = 0;
            foreach (ModelBlockView moving in _movingBlocks)
            {
                for (int port = 0; port < 2; port++)
                {
                    if (!IsPortAvailable(moving, port))
                    {
                        continue;
                    }
                    foreach (ModelBlockView target in _blocks.Values)
                    {
                        int targetPort = 1 - port;
                        if (_movingBlocks.Contains(target) || !IsPortAvailable(target, targetPort))
                        {
                            continue;
                        }
                        float squaredDistance = (moving.GetPortPosition(port) - target.GetPortPosition(targetPort)).sqrMagnitude;
                        if (squaredDistance < closestSquared)
                        {
                            closestSquared = squaredDistance;
                            closestMoving = moving;
                            closestTarget = target;
                            closestPort = port;
                        }
                    }
                }
            }
            if (closestMoving == null)
            {
                return null;
            }
            if (_candidate != null && _candidate.First == closestMoving &&
                _candidate.Second == closestTarget && _candidate.FirstPort == closestPort)
            {
                return _candidate;
            }
            return new Connection(closestMoving, closestPort, closestTarget, 1 - closestPort);
        }

        private bool IsPortAvailable(ModelBlockView block, int port)
        {
            if (!block.HasPort(port))
            {
                return false;
            }
            foreach (Connection connection in _connections)
            {
                if (connection.Uses(block, port))
                {
                    return false;
                }
            }
            return true;
        }

        private void SetCandidate(Connection candidate)
        {
            if (ReferenceEquals(_candidate, candidate))
            {
                return;
            }
            if (_candidate != null)
            {
                RefreshPorts(_candidate);
            }
            _candidate = candidate;
            if (candidate != null)
            {
                candidate.First.SetPortState(candidate.FirstPort, false, true);
                candidate.Second.SetPortState(candidate.SecondPort, false, true);
            }
        }

        private void RefreshPorts(Connection connection)
        {
            connection.First.SetPortState(connection.FirstPort, !IsPortAvailable(connection.First, connection.FirstPort), false);
            connection.Second.SetPortState(connection.SecondPort, !IsPortAvailable(connection.Second, connection.SecondPort), false);
        }

        private void DisconnectSelectedBlock()
        {
            if (_settingsBlock == null)
            {
                return;
            }
            for (int index = _connections.Count - 1; index >= 0; index--)
            {
                Connection connection = _connections[index];
                if (connection.First == _settingsBlock || connection.Second == _settingsBlock)
                {
                    _workspace.Disconnect(connection.First.Layer, connection.Second.Layer);
                    _connections.RemoveAt(index);
                    RefreshPorts(connection);
                }
            }
            // A small separation makes the result visible and avoids reconnecting an unmoved block on release.
            _settingsBlock.Translate(-_settingsBlock.transform.forward * (_snapDistance + 0.04f));
            _settingsBlock = null;
            _popup.Hide();
        }

        private void BindRightHand()
        {
            foreach (XRRayInteractor ray in FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None))
            {
                if (ray.handedness == InteractorHandedness.Right && ray.isActiveAndEnabled)
                {
                    _rightRay = ray;
                    break;
                }
            }
            if (_rightRay == null)
            {
                Debug.LogWarning("[ModelDesign] Enter this content scene from QuestMain to use its XR player rig.", this);
                return;
            }
            _previousUIInteraction = _rightRay.enableUIInteraction;
            _previousHoveredActivate = _rightRay.allowHoveredActivate;
            _previousHitClosestOnly = _rightRay.hitClosestOnly;
            _previousUIPress = _rightRay.uiPressInput;
            _uiPress = new XRInputButtonReader("Model Design UI", inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = new InputAction("Model Design UI Press", InputActionType.Button,
                    "<XRController>{RightHand}/triggerPressed"),
                inputActionValue = new InputAction("Model Design UI Value", InputActionType.Value,
                    "<XRController>{RightHand}/trigger")
            };
            _rightRay.uiPressInput = _uiPress;
            _rightRay.enableUIInteraction = true;
            _rightRay.allowHoveredActivate = true;
            _rightRay.hitClosestOnly = true;
        }

        private void PrepareUIInput()
        {
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("Model Design UI Input", typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(transform, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedUIInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void OnDisable()
        {
            if (_hoveredPortBlock != null)
            {
                _hoveredPortBlock.SetHoveredPort(-1);
                _hoveredPortBlock = null;
            }
            if (_popup != null)
            {
                _popup.Hide();
            }
            SetCandidate(null);
            RestoreBlockParents();
            if (_grabbedBlock != null)
            {
                _grabbedBlock.CancelGrab();
                _grabbedBlock.transform.SetParent(transform, true);
            }
            _grabbedBlock = null;
            _movingBlocks.Clear();
            if (_paletteRoot != null)
            {
                XRGrabInteractable paletteGrab = _paletteRoot.GetComponent<XRGrabInteractable>();
                if (paletteGrab != null && paletteGrab.isSelected && paletteGrab.interactionManager != null)
                {
                    paletteGrab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)paletteGrab);
                }
                // XRI unparents the palette while grabbed; restore scene ownership on shutdown.
                _paletteRoot.SetParent(transform, true);
            }
            if (_rightRay != null)
            {
                _rightRay.uiPressInput = _previousUIPress;
                _rightRay.enableUIInteraction = _previousUIInteraction;
                _rightRay.allowHoveredActivate = _previousHoveredActivate;
                _rightRay.hitClosestOnly = _previousHitClosestOnly;
                _rightRay = null;
            }
            if (_uiPress != null)
            {
                _uiPress.inputActionPerformed.Dispose();
                _uiPress.inputActionValue.Dispose();
                _uiPress = null;
            }
            if (_ownedEventSystem != null)
            {
                Destroy(_ownedEventSystem);
            }
            if (_addedUIInputModule != null)
            {
                Destroy(_addedUIInputModule);
            }
        }

        private void OnDestroy()
        {
            if (_popup != null)
            {
                _popup.OnDisconnectRequested -= DisconnectSelectedBlock;
            }
            _workspace?.Dispose();
        }

        [System.Serializable]
        private sealed class PaletteBlock
        {
            [SerializeField] private LayerType _type;
            [SerializeField] private ModelBlockView _block;

            public LayerType Type => _type;
            public ModelBlockView Block => _block;
        }

        private sealed class Connection
        {
            public ModelBlockView First { get; }
            public int FirstPort { get; }
            public ModelBlockView Second { get; }
            public int SecondPort { get; }

            public Connection(ModelBlockView first, int firstPort, ModelBlockView second, int secondPort)
            {
                First = first;
                FirstPort = firstPort;
                Second = second;
                SecondPort = secondPort;
            }

            public bool Uses(ModelBlockView block, int port)
            {
                return (First == block && FirstPort == port) || (Second == block && SecondPort == port);
            }
        }
    }
}
