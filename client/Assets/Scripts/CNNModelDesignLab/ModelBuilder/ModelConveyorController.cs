using System.Collections.Generic;
using ModelDesignScene.Model;
using UnityEngine;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class ModelConveyorController : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Header("Saved slot layout")]
        [SerializeField] private Transform[] _blockAnchors;
        [SerializeField] private Vector3 _placementHalfExtents = new Vector3(0.25f, 0.2f, 0.24f);

        [Header("Assembly line visuals")]
        [SerializeField] private Transform _slotVisualsRoot;
        [SerializeField] private Color _emptyColor = new Color(0.24f, 0.3f, 0.36f);
        [SerializeField] private Color _occupiedColor = new Color(0.15f, 0.48f, 0.38f);
        [SerializeField] private Color _candidateColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _blockedColor = new Color(0.85f, 0.22f, 0.18f);

        [Header("Fixed controls")]
        [SerializeField] private Transform _settingsControlsRoot;

        private readonly List<Renderer> _slotRenderers = new List<Renderer>();
        private readonly List<MeshFilter> _slotMeshes = new List<MeshFilter>();
        private readonly List<Mesh> _originalSlotMeshes = new List<Mesh>();
        private readonly List<SlotSettingsController> _settingsControls = new List<SlotSettingsController>();
        private readonly List<Vector3> _settingsPositions = new List<Vector3>();
        private readonly List<Vector3> _popupPositions = new List<Vector3>();
        private ConveyorSlots _slots;
        private MaterialPropertyBlock _properties;
        private int _previewSlot = -1;
        private bool _previewIsConv;
        private Mesh _convSlotMesh;

        internal IReadOnlyList<SlotSettingsController> SettingsControls => _settingsControls;

        internal void Initialize(ConveyorSlots slots)
        {
            _slots = slots;
            _properties = new MaterialPropertyBlock();
            _previewSlot = -1;
            _slotRenderers.Clear();
            _slotMeshes.Clear();
            _originalSlotMeshes.Clear();
            _convSlotMesh = ConvBlockVisualController.CreateSlotMesh();
            _settingsControls.Clear();
            _settingsPositions.Clear();
            _popupPositions.Clear();
            // Sibling order matches the saved block anchors; scene transforms remain editable.
            for (int index = 0; index < _blockAnchors.Length; index++)
            {
                _slotRenderers.Add(_slotVisualsRoot.GetChild(index).GetComponent<Renderer>());
                MeshFilter slotMesh = _slotVisualsRoot.GetChild(index).GetComponent<MeshFilter>();
                _slotMeshes.Add(slotMesh);
                _originalSlotMeshes.Add(slotMesh.sharedMesh);
                SlotSettingsController control = _settingsControlsRoot.GetChild(index).GetComponent<SlotSettingsController>();
                control.Initialize(index);
                _settingsControls.Add(control);
                _settingsPositions.Add(control.transform.localPosition);
                _popupPositions.Add(control.PopupAnchor.localPosition);
                RefreshSlot(index);
            }
        }

        internal void PreviewPlacement(ModelBlockController block)
        {
            int slot = block != null ? FindSlot(block.transform.position) : -1;
            bool isConv = block != null && block.Layer.Type == LayerType.Conv2D;
            if (_previewSlot == slot && _previewIsConv == isConv)
            {
                return;
            }
            ClearPreview();
            _previewSlot = slot;
            _previewIsConv = isConv;
            if (slot >= 0)
            {
                RefreshSlot(slot);
            }
        }

        internal void ClearPreview()
        {
            int previousSlot = _previewSlot;
            _previewSlot = -1;
            _previewIsConv = false;
            if (previousSlot >= 0)
            {
                RefreshSlot(previousSlot);
            }
        }

        internal void AlignBlock(int slot, ModelBlockController block)
        {
            Transform anchor = _blockAnchors[slot];
            block.SetPose(anchor.position, anchor.rotation);
            RefreshSlot(slot);
        }

        internal int FindSlot(Vector3 worldPosition)
        {
            int nearestSlot = -1;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < _blockAnchors.Length; index++)
            {
                Transform anchor = _blockAnchors[index];
                Vector3 offset = anchor.InverseTransformPoint(worldPosition);
                if (Mathf.Abs(offset.x) > _placementHalfExtents.x ||
                    Mathf.Abs(offset.y) > _placementHalfExtents.y ||
                    Mathf.Abs(offset.z) > _placementHalfExtents.z)
                {
                    continue;
                }
                float distance = (worldPosition - anchor.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestSlot = index;
                    nearestDistance = distance;
                }
            }
            return nearestSlot;
        }

        internal void RefreshSlot(int index)
        {
            ModelBlockController block = _slots[index];
            _settingsControls[index].SetBlock(block);
            bool occupied = block != null;
            bool showConvSlot = occupied ? block.Layer.Type == LayerType.Conv2D
                : index == _previewSlot && _previewIsConv;
            _slotMeshes[index].sharedMesh = showConvSlot ? _convSlotMesh : _originalSlotMeshes[index];
            bool hasConv = occupied && block.Layer.Type == LayerType.Conv2D;
            _settingsControls[index].transform.localPosition = _settingsPositions[index]
                + (hasConv ? new Vector3(0f, 0f, -0.3f) : Vector3.zero);
            _settingsControls[index].PopupAnchor.localPosition = _popupPositions[index]
                + (hasConv ? new Vector3(0f, 0.5f, 0f) : Vector3.zero);
            Color color = index == _previewSlot
                ? (occupied ? _blockedColor : _candidateColor)
                : (occupied ? _occupiedColor : _emptyColor);
            _slotRenderers[index].GetPropertyBlock(_properties);
            _properties.SetColor(BaseColor, color);
            _slotRenderers[index].SetPropertyBlock(_properties);
        }

        private void OnDisable()
        {
            ClearPreview();
        }

        private void OnDestroy()
        {
            Destroy(_convSlotMesh);
        }
    }
}
