using System.Collections.Generic;
using UnityEngine;

namespace ModelDesignScene.Assembly
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
        private readonly List<SlotSettingsController> _settingsControls = new List<SlotSettingsController>();
        private ConveyorSlots _slots;
        private MaterialPropertyBlock _properties;
        private int _previewSlot = -1;

        internal int SlotCount => _blockAnchors.Length;
        internal IReadOnlyList<SlotSettingsController> SettingsControls => _settingsControls;

        internal void Initialize(ConveyorSlots slots)
        {
            _slots = slots;
            _properties = new MaterialPropertyBlock();
            _previewSlot = -1;
            _slotRenderers.Clear();
            _settingsControls.Clear();
            // Sibling order matches the saved block anchors; scene transforms remain editable.
            for (int index = 0; index < _blockAnchors.Length; index++)
            {
                _slotRenderers.Add(_slotVisualsRoot.GetChild(index).GetComponent<Renderer>());
                SlotSettingsController control = _settingsControlsRoot.GetChild(index).GetComponent<SlotSettingsController>();
                control.Initialize(index);
                _settingsControls.Add(control);
                RefreshSlot(index);
            }
        }

        internal void PreviewPlacement(ModelBlockController block)
        {
            int slot = block != null ? FindSlot(block.transform.position) : -1;
            if (_previewSlot == slot)
            {
                return;
            }
            ClearPreview();
            _previewSlot = slot;
            if (slot >= 0)
            {
                RefreshSlot(slot);
            }
        }

        internal void ClearPreview()
        {
            int previousSlot = _previewSlot;
            _previewSlot = -1;
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
    }
}
