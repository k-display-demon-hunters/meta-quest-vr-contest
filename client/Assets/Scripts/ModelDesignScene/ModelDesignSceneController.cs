using System.Collections.Generic;
using ModelDesign;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ModelDesignScene
{
    public sealed class ModelDesignSceneController : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color ConnectedColor = new Color(0.2f, 1f, 0.7f);
        private static readonly Color CandidateColor = new Color(1f, 0.8f, 0.2f);

        [SerializeField] private ModelBlockView _blockPrefab;
        [SerializeField] private BlockSettingsPopup _popupPrefab;
        [SerializeField] private ModelBlockView _inputBlock;
        [SerializeField] private ModelBlockView _outputBlock;
        [SerializeField] private Transform _paletteRoot;
        [SerializeField] private PaletteBlock[] _paletteBlocks;
        [SerializeField] private Material _connectionMaterial;
        [SerializeField, Min(0.001f)] private float _connectionWidth = 0.008f;

        private readonly Dictionary<LayerDefinition, ModelBlockView> _blocks = new Dictionary<LayerDefinition, ModelBlockView>();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly Dictionary<ModelBlockView, Pose> _paletteSlots = new Dictionary<ModelBlockView, Pose>();
        private ModelWorkspace _workspace;
        private BlockSettingsPopup _popup;
        private ModelBlockView _grabbedBlock;
        private ModelBlockView _settingsBlock;
        private ModelBlockView _hoveredPortBlock;
        private Connection _hoveredConnection;
        private ModelBlockView _connectionSource;
        private int _connectionSourcePort = -1;
        private float _connectionRayDistance;
        private LineRenderer _previewLine;
        private MaterialPropertyBlock _lineProperties;
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
            _lineProperties = new MaterialPropertyBlock();
            _previewLine = CreateConnectionLine("Connection Preview");
            _previewLine.enabled = false;
            _popup = Instantiate(_popupPrefab, transform);
            _popup.Hide();
            _popup.OnDisconnectRequested += DisconnectSelectedBlock;
            _popup.OnDeleteRequested += DeleteSelectedBlock;
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
            if (_connectionSource != null)
            {
                if (_rightRay == null || !_rightRay.isActiveAndEnabled)
                {
                    CancelConnection();
                }
                else
                {
                    UpdateConnectionPreview();
                }
            }
            foreach (Connection connection in _connections)
            {
                UpdateConnectionLine(connection);
            }
            UpdateConnectionHover();
        }

        internal void BeginGrab(ModelBlockView block)
        {
            _popup.Hide();
            _settingsBlock = null;
            CancelConnection();
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
        }

        internal void EndGrab(ModelBlockView block, bool cancelled)
        {
            if (_grabbedBlock != block)
            {
                return;
            }
            // Taken blocks belong to the scene, even when the palette moves later.
            block.transform.SetParent(transform, true);
            _grabbedBlock = null;
        }

        internal void BeginConnection(ModelBlockView block, int port, IXRSelectInteractor interactor)
        {
            if (!ReferenceEquals(interactor, _rightRay) || _grabbedBlock != null || _connectionSource != null ||
                !IsWorkspaceBlock(block) || !IsPortAvailable(block, port))
            {
                return;
            }

            _popup.Hide();
            _settingsBlock = null;
            _connectionSource = block;
            _connectionSourcePort = port;
            _connectionRayDistance = Vector3.Distance(_rightRay.rayOriginTransform.position, block.GetPortPosition(port));
            _previewLine.enabled = true;
            UpdateConnectionPreview();
        }

        internal void EndConnection(ModelBlockView block, int port, bool cancelled)
        {
            if (_connectionSource != block || _connectionSourcePort != port)
            {
                return;
            }

            Connection connection = cancelled ? null : FindConnectionCandidate();
            ClearConnectionPreview();
            if (connection != null)
            {
                // Only a released port selection connects models; moving blocks never auto-connects them.
                _workspace.Connect(connection.First.Layer, connection.Second.Layer);
                connection.Line = CreateConnectionLine("Block Connection");
                SetLineColor(connection.Line, ConnectedColor);
                PrepareConnectionInteraction(connection);
                _connections.Add(connection);
                UpdateConnectionLine(connection);
                RefreshPorts(connection);
            }
        }

        private void CancelConnection()
        {
            ModelBlockView source = _connectionSource;
            ClearConnectionPreview();
            if (source != null)
            {
                source.CancelPortSelection();
            }
        }

        private void ClearConnectionPreview()
        {
            SetCandidate(null);
            if (_connectionSource != null)
            {
                _connectionSource.SetPortState(_connectionSourcePort,
                    !IsPortAvailable(_connectionSource, _connectionSourcePort), false);
            }
            _connectionSource = null;
            _connectionSourcePort = -1;
            if (_previewLine != null)
            {
                _previewLine.enabled = false;
            }
        }

        private void UpdateConnectionPreview()
        {
            SetCandidate(FindConnectionCandidate());
            _connectionSource.SetPortState(_connectionSourcePort, false, true);
            Vector3 end = _candidate != null
                ? _candidate.Second.GetPortPosition(_candidate.SecondPort)
                : GetConnectionPointerPosition();
            _previewLine.SetPosition(0, _connectionSource.GetPortPosition(_connectionSourcePort));
            _previewLine.SetPosition(1, end);
            SetLineColor(_previewLine, _candidate != null ? CandidateColor : Color.white);
        }

        private Vector3 GetConnectionPointerPosition()
        {
            if (_rightRay.TryGetCurrentRaycast(out RaycastHit? hit, out _,
                out RaycastResult? uiHit, out _, out bool isUIHitClosest))
            {
                if (isUIHitClosest && uiHit.HasValue)
                {
                    return uiHit.Value.worldPosition;
                }
                if (hit.HasValue)
                {
                    return hit.Value.point;
                }
            }
            Transform origin = _rightRay.rayOriginTransform;
            return origin.position + origin.forward * _connectionRayDistance;
        }

        private LineRenderer CreateConnectionLine(string lineName)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = _connectionMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = _connectionWidth;
            line.endWidth = _connectionWidth;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void SetLineColor(LineRenderer line, Color color)
        {
            _lineProperties.SetColor(BaseColor, color);
            line.SetPropertyBlock(_lineProperties);
        }

        private void UpdateConnectionLine(Connection connection)
        {
            Vector3 start = connection.First.GetPortPosition(connection.FirstPort);
            Vector3 end = connection.Second.GetPortPosition(connection.SecondPort);
            connection.Line.SetPosition(0, start);
            connection.Line.SetPosition(1, end);
            Vector3 direction = end - start;
            float length = direction.magnitude;
            // Leave the port spheres outside the wire's hit area so they remain easy to point at.
            float portClearance = Mathf.Min(0.03f, length * 0.25f);
            connection.Collider.enabled = length > 0.001f;
            if (connection.Collider.enabled)
            {
                connection.Line.transform.SetPositionAndRotation((start + end) * 0.5f, Quaternion.LookRotation(direction));
                float hitLength = length - portClearance * 2f;
                connection.Collider.radius = Mathf.Min(Mathf.Max(_connectionWidth * 0.5f, 0.015f),
                    hitLength * 0.5f);
                connection.Collider.height = hitLength;
            }
        }

        private void PrepareConnectionInteraction(Connection connection)
        {
            CapsuleCollider collider = connection.Line.gameObject.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.radius = Mathf.Max(_connectionWidth * 0.5f, 0.015f);
            connection.Collider = collider;
            XRSimpleInteractable interactable = connection.Line.gameObject.AddComponent<XRSimpleInteractable>();
            connection.Interactable = interactable;
            connection.OnActivated = args => HandleConnectionTapped(connection, args);
            interactable.activated.AddListener(connection.OnActivated);
        }

        private void UpdateConnectionHover()
        {
            Connection hovered = null;
            if (TryGetRayCollider(out Collider collider))
            {
                foreach (Connection connection in _connections)
                {
                    if (connection.Collider == collider)
                    {
                        hovered = connection;
                        break;
                    }
                }
            }
            if (ReferenceEquals(_hoveredConnection, hovered))
            {
                return;
            }
            ClearConnectionHover();
            _hoveredConnection = hovered;
            if (hovered != null)
            {
                SetLineColor(hovered.Line, Color.yellow);
            }
        }

        private void ClearConnectionHover()
        {
            if (_hoveredConnection != null)
            {
                SetLineColor(_hoveredConnection.Line, ConnectedColor);
                _hoveredConnection = null;
            }
        }

        private void HandleConnectionTapped(Connection connection, ActivateEventArgs args)
        {
            if (!isActiveAndEnabled || !ReferenceEquals(args.interactorObject, _rightRay) ||
                _grabbedBlock != null || _connectionSource != null || _rightRay.hasSelection ||
                !TryGetRayCollider(out Collider collider) || collider != connection.Collider)
            {
                return;
            }
            DisconnectConnection(connection);
        }

        private void DisconnectConnection(Connection connection)
        {
            int index = _connections.IndexOf(connection);
            if (index < 0)
            {
                return;
            }
            _workspace.Disconnect(connection.First.Layer, connection.Second.Layer);
            RemoveConnection(index);
            _settingsBlock = null;
            _popup.Hide();
        }

        internal void HandleBlockTapped(ModelBlockView block)
        {
            if (_grabbedBlock != null || _connectionSource != null || block.IsPalette ||
                _rightRay == null || _rightRay.hasSelection)
            {
                return;
            }
            if (TryGetRayPort(out ModelBlockView hitBlock, out int port) && hitBlock == block)
            {
                foreach (Connection connection in _connections)
                {
                    if (connection.Uses(block, port))
                    {
                        // A connected port click removes only its own wire, not the block's other connection.
                        DisconnectConnection(connection);
                        return;
                    }
                }
            }
            ShowSettings(block);
        }

        private void ShowSettings(ModelBlockView block)
        {
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
            TryGetRayPort(out ModelBlockView hoveredBlock, out int hoveredPort);

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

        private bool TryGetRayPort(out ModelBlockView block, out int port)
        {
            block = null;
            port = -1;
            if (TryGetRayCollider(out Collider collider))
            {
                ModelBlockView hitBlock = collider.GetComponentInParent<ModelBlockView>();
                if (hitBlock != null && hitBlock.Layer != null && hitBlock.TryGetPort(collider, out port) &&
                    (_paletteSlots.ContainsKey(hitBlock) || IsWorkspaceBlock(hitBlock)))
                {
                    block = hitBlock;
                    return true;
                }
            }
            return false;
        }

        private bool TryGetRayCollider(out Collider collider)
        {
            collider = null;
            if (_rightRay != null && _rightRay.isActiveAndEnabled &&
                _rightRay.TryGetCurrentRaycast(out RaycastHit? hit, out _, out _, out _, out bool isUIHitClosest) &&
                !isUIHitClosest && hit.HasValue)
            {
                collider = hit.Value.collider;
                return collider != null;
            }
            return false;
        }

        private bool IsWorkspaceBlock(ModelBlockView block)
        {
            return block != null && !block.IsPalette && block.Layer != null && _blocks.ContainsKey(block.Layer);
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

        private Connection FindConnectionCandidate()
        {
            if (!IsWorkspaceBlock(_connectionSource) || !IsPortAvailable(_connectionSource, _connectionSourcePort) ||
                !TryGetRayPort(out ModelBlockView target, out int targetPort) || !IsWorkspaceBlock(target) ||
                targetPort == _connectionSourcePort || !IsPortAvailable(target, targetPort) ||
                ReferenceEquals(_workspace.GetModel(_connectionSource.Layer), _workspace.GetModel(target.Layer)))
            {
                return null;
            }
            if (_candidate != null && _candidate.First == _connectionSource &&
                _candidate.Second == target && _candidate.FirstPort == _connectionSourcePort)
            {
                return _candidate;
            }
            return new Connection(_connectionSource, _connectionSourcePort, target, targetPort);
        }

        private bool IsPortAvailable(ModelBlockView block, int port)
        {
            if (port < 0 || port > 1 || !block.HasPort(port))
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
                    RemoveConnection(index);
                }
            }
            _settingsBlock = null;
            _popup.Hide();
        }

        private void DeleteSelectedBlock()
        {
            ModelBlockView block = _settingsBlock;
            if (!IsWorkspaceBlock(block) || block.Layer.Type == LayerType.Input || block.Layer.Type == LayerType.Output)
            {
                return;
            }

            _settingsBlock = null;
            _popup.Hide();
            CancelConnection();
            _workspace.RemoveLayer(block.Layer);
            for (int index = _connections.Count - 1; index >= 0; index--)
            {
                Connection connection = _connections[index];
                if (connection.First == block || connection.Second == block)
                {
                    RemoveConnection(index);
                }
            }
            if (_hoveredPortBlock == block)
            {
                _hoveredPortBlock.SetHoveredPort(-1);
                _hoveredPortBlock = null;
            }
            _blocks.Remove(block.Layer);
            // Remove interaction immediately; Unity destroys the object at the end of the frame.
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }

        private void RemoveConnection(int index)
        {
            Connection connection = _connections[index];
            if (ReferenceEquals(_hoveredConnection, connection))
            {
                ClearConnectionHover();
            }
            _connections.RemoveAt(index);
            connection.Interactable.activated.RemoveListener(connection.OnActivated);
            connection.Line.gameObject.SetActive(false);
            Destroy(connection.Line.gameObject);
            RefreshPorts(connection);
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
            ClearConnectionHover();
            if (_hoveredPortBlock != null)
            {
                _hoveredPortBlock.SetHoveredPort(-1);
                _hoveredPortBlock = null;
            }
            if (_popup != null)
            {
                _popup.Hide();
            }
            CancelConnection();
            if (_grabbedBlock != null)
            {
                ModelBlockView grabbedBlock = _grabbedBlock;
                grabbedBlock.CancelGrab();
                grabbedBlock.transform.SetParent(transform, true);
            }
            _grabbedBlock = null;
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
                _popup.OnDeleteRequested -= DeleteSelectedBlock;
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
            public LineRenderer Line { get; set; }
            public CapsuleCollider Collider { get; set; }
            public XRSimpleInteractable Interactable { get; set; }
            public UnityAction<ActivateEventArgs> OnActivated { get; set; }

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
