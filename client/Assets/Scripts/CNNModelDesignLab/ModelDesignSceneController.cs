using System.Collections.Generic;
using ModelDesign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
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
        [SerializeField] private Button _trainingStartButton;
        [SerializeField] private Button _trainingStopButton;
        [SerializeField] private Text _trainingStatus;

        private readonly Dictionary<LayerDefinition, ModelBlockView> _blocks = new Dictionary<LayerDefinition, ModelBlockView>();
        private readonly Dictionary<ModelBlockView, Pose> _paletteSlots = new Dictionary<ModelBlockView, Pose>();
        private CNNModelDesignLabManager _manager;
        private ModelWorkspace _workspace;
        private BlockSettingsPopup _popup;
        private ModelBlockView _grabbedBlock;
        private ModelBlockView _settingsBlock;
        private XRRayInteractor _rightRay;
        private bool _previousUIInteraction;
        private bool _previousHoveredActivate;
        private bool _previousHitClosestOnly;
        private XRInputButtonReader _previousUIPress;
        private XRInputButtonReader _uiPress;
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedUIInputModule;
        private bool _isTraining;

        public ModelBlockView InputBlock => _inputBlock;
        public ModelBlockView OutputBlock => _outputBlock;

        private void Awake()
        {
            _popup = Instantiate(_popupPrefab, transform);
            _popup.Hide();
            _popup.OnDeleteRequested += DeleteSelectedBlock;
            _trainingStartButton.onClick.AddListener(StartTraining);
            _trainingStopButton.onClick.AddListener(StopTraining);
            SetTrainingState(false);
        }

        private void OnEnable()
        {
            PrepareUIInput();
            BindRightHand();
        }

        private void StartTraining()
        {
            if (_isTraining || _workspace == null || _grabbedBlock != null)
            {
                return;
            }
            if (!ModelJsonSerializer.TrySerialize(_workspace, out string json,
                out IReadOnlyList<ValidationIssue> issues))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                foreach (ValidationIssue issue in issues)
                {
                    Debug.LogWarning($"[ModelDesign] 모델 JSON 변환 실패: {issue.Path}: {issue.Message}", this);
                }
#endif
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Temporary preview only: replace this output with the training request when server integration is added.
            Debug.Log($"[ModelDesign] 서버에 보낼 모델 JSON:\n{json}", this);
#endif
            SetTrainingState(true);
        }

        private void StopTraining()
        {
            SetTrainingState(false);
        }

        public void FinishTraining()
        {
            // A future training completion callback restores the start button through this method.
            SetTrainingState(false);
        }

        private void SetTrainingState(bool isTraining)
        {
            _isTraining = isTraining;
            _trainingStartButton.gameObject.SetActive(!isTraining);
            _trainingStopButton.gameObject.SetActive(isTraining);
            _trainingStatus.gameObject.SetActive(isTraining);
        }

        private void LateUpdate()
        {
            if (_workspace != null)
            {
                _manager.PreviewBlockPlacement(_grabbedBlock);
            }
        }

        public void Initialize(CNNModelDesignLabManager manager, ModelWorkspace workspace)
        {
            _manager = manager;
            _workspace = workspace;
            InitializeBlock(_inputBlock, workspace.Input, false);
            InitializeBlock(_outputBlock, workspace.Output, false);
            foreach (PaletteBlock entry in _paletteBlocks)
            {
                InitializeBlock(entry.Block, CreateDefaultLayer(entry.Type), true);
            }
        }

        internal void OpenSlotSettings(SlotSettingsController control, IXRActivateInteractor interactor)
        {
            ModelBlockView block = control.Block;
            if (_grabbedBlock != null || !IsWorkspaceBlock(block) ||
                _rightRay == null || !ReferenceEquals(interactor, _rightRay) || _rightRay.hasSelection ||
                !TryGetRayCollider(out Collider collider) || collider.GetComponentInParent<SlotSettingsController>() != control)
            {
                return;
            }
            _settingsBlock = block;
            Transform anchor = control.PopupAnchor;
            // The device owns the popup pose. Head position and viewing direction are irrelevant.
            _popup.transform.SetParent(anchor, false);
            _popup.Show(block.Layer, anchor.position, anchor.rotation);
        }

        private void DeleteSelectedBlock()
        {
            ModelBlockView block = _settingsBlock;
            if (!IsWorkspaceBlock(block) || block.IsFixed)
            {
                return;
            }
            _settingsBlock = null;
            _popup.Hide();
            _manager.DeleteBlock(block);
            _blocks.Remove(block.Layer);
            // Remove interaction immediately; Unity destroys the object at the end of the frame.
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }

        internal void BeginGrab(ModelBlockView block)
        {
            _popup.Hide();
            _settingsBlock = null;
            if (block.IsPalette)
            {
                Pose slot = _paletteSlots[block];
                _paletteSlots.Remove(block);
                block.TakeFromPalette();
                _blocks.Add(block.Layer, block);
                _manager.AddBlock(block);
                CreatePaletteBlock(block.Layer.Type, slot);
            }

            _manager.BeginBlockMove(block);
            _grabbedBlock = block;
        }

        internal void EndGrab(ModelBlockView block, bool cancelled)
        {
            if (_grabbedBlock != block)
            {
                return;
            }
            // Taken blocks belong to the workspace, not the parts supply rack.
            block.transform.SetParent(transform, true);
            _manager.FinishBlockMove(block, cancelled);
            _grabbedBlock = null;
        }

        private void InitializeBlock(ModelBlockView block, LayerDefinition layer, bool palette)
        {
            block.name = palette ? layer.Type + " Supply" : layer.Type + " Block";
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
            // Refill the fixed supply position with a fresh block of the same type.
            ModelBlockView block = Instantiate(_blockPrefab, _paletteRoot);
            block.transform.SetLocalPositionAndRotation(slot.position, slot.rotation);
            InitializeBlock(block, CreateDefaultLayer(type), true);
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
            _manager?.PreviewBlockPlacement(null);
            if (_popup != null)
            {
                _popup.Hide();
            }
            if (_grabbedBlock != null)
            {
                ModelBlockView grabbedBlock = _grabbedBlock;
                grabbedBlock.CancelGrab();
                grabbedBlock.transform.SetParent(transform, true);
            }
            _grabbedBlock = null;
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
            if (_trainingStartButton != null)
            {
                _trainingStartButton.onClick.RemoveListener(StartTraining);
            }
            if (_trainingStopButton != null)
            {
                _trainingStopButton.onClick.RemoveListener(StopTraining);
            }
            if (_popup != null)
            {
                _popup.OnDeleteRequested -= DeleteSelectedBlock;
            }
        }

        [System.Serializable]
        private sealed class PaletteBlock
        {
            [SerializeField] private LayerType _type;
            [SerializeField] private ModelBlockView _block;

            public LayerType Type => _type;
            public ModelBlockView Block => _block;
        }
    }
}
