using System.Collections.Generic;
using UnityEngine;

namespace ModelDesignScene
{
    public sealed class ModelConveyorController : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Header("Layout (applied when the scene starts)")]
        [SerializeField, Min(1)] private int _slotCount = 8;
        [SerializeField, Min(0.1f)] private float _slotSpacing = 0.52f;
        [SerializeField] private float _slotSurfaceHeight = 0.036f;
        [SerializeField] private float _blockCenterHeight = 0.162f;
        [SerializeField] private Vector3 _placementHalfExtents = new Vector3(0.25f, 0.2f, 0.24f);

        [Header("Replaceable visuals")]
        [SerializeField] private Transform _beltVisual;
        [SerializeField] private Transform _slotVisualsRoot;
        [SerializeField] private Renderer _slotTemplate;
        [SerializeField] private Color _emptyColor = new Color(0.24f, 0.3f, 0.36f);
        [SerializeField] private Color _occupiedColor = new Color(0.15f, 0.48f, 0.38f);
        [SerializeField] private Color _candidateColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _blockedColor = new Color(0.85f, 0.22f, 0.18f);

        [Header("Fixed controls")]
        [SerializeField] private Transform _settingsControlsRoot;
        [SerializeField] private SlotSettingsController _settingsControlPrefab;
        [SerializeField] private Vector3 _settingsControlOffset = new Vector3(0f, 0.08f, -0.39f);
        [SerializeField] private Transform _trainingAnchor;

        private readonly List<Renderer> _slotRenderers = new List<Renderer>();
        private readonly List<SlotSettingsController> _settingsControls = new List<SlotSettingsController>();
        private ModelBlockController[] _blocks;
        private MaterialPropertyBlock _properties;
        private int _previewSlot = -1;

        public int SlotCount => _blocks.Length;
        public IReadOnlyList<SlotSettingsController> SettingsControls => _settingsControls;

        public void Initialize(ModelBlockController input, ModelBlockController output)
        {
            _slotCount = Mathf.Max(1, _slotCount);
            _blocks = new ModelBlockController[_slotCount];
            _properties = new MaterialPropertyBlock();
            ConfigureVisuals();
            ConfigureSettingsControls();
            float endpointX = (_slotCount + 1) * _slotSpacing * 0.5f;
            input.SetPose(transform.TransformPoint(new Vector3(-endpointX, _blockCenterHeight, 0f)), transform.rotation);
            output.SetPose(transform.TransformPoint(new Vector3(endpointX, _blockCenterHeight, 0f)), transform.rotation);
            Vector3 trainingPosition = _trainingAnchor.localPosition;
            trainingPosition.x = -endpointX;
            _trainingAnchor.localPosition = trainingPosition;
        }

        public ModelBlockController GetBlock(int index)
        {
            return _blocks[index];
        }

        public bool RemoveBlock(ModelBlockController block)
        {
            for (int index = 0; index < _blocks.Length; index++)
            {
                if (_blocks[index] == block)
                {
                    _blocks[index] = null;
                    _settingsControls[index].SetBlock(null);
                    RefreshSlot(index);
                    return true;
                }
            }
            return false;
        }

        public void PreviewPlacement(ModelBlockController block)
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

        public void ClearPreview()
        {
            int previousSlot = _previewSlot;
            _previewSlot = -1;
            if (previousSlot >= 0)
            {
                RefreshSlot(previousSlot);
            }
        }

        public bool TryPlaceBlock(ModelBlockController block)
        {
            if (block == null || block.IsFixed || block.IsPalette)
            {
                return false;
            }
            int slot = FindSlot(block.transform.position);
            if (slot < 0 || _blocks[slot] != null)
            {
                return false;
            }
            _blocks[slot] = block;
            _settingsControls[slot].SetBlock(block);
            block.SetPose(transform.TransformPoint(GetBlockPosition(slot)), transform.rotation);
            RefreshSlot(slot);
            return true;
        }

        private void ConfigureVisuals()
        {
            Vector3 beltScale = _beltVisual.localScale;
            beltScale.x = (_slotCount + 2) * _slotSpacing;
            _beltVisual.localScale = beltScale;

            int existingCount = _slotVisualsRoot.childCount;
            for (int index = 0; index < Mathf.Max(existingCount, _slotCount); index++)
            {
                Renderer slot = index < existingCount
                    ? _slotVisualsRoot.GetChild(index).GetComponent<Renderer>()
                    : Instantiate(_slotTemplate, _slotVisualsRoot);
                slot.gameObject.SetActive(index < _slotCount);
                if (index >= _slotCount)
                {
                    continue;
                }
                slot.name = "Slot " + (index + 1);
                Vector3 position = GetBlockPosition(index);
                position.y = _slotSurfaceHeight;
                slot.transform.position = transform.TransformPoint(position);
                _slotRenderers.Add(slot);
                RefreshSlot(index);
            }
        }

        private Vector3 GetBlockPosition(int index)
        {
            return new Vector3((index - (_slotCount - 1) * 0.5f) * _slotSpacing, _blockCenterHeight, 0f);
        }

        private void ConfigureSettingsControls()
        {
            int existingCount = _settingsControlsRoot.childCount;
            for (int index = 0; index < Mathf.Max(existingCount, _slotCount); index++)
            {
                SlotSettingsController control = index < existingCount
                    ? _settingsControlsRoot.GetChild(index).GetComponent<SlotSettingsController>()
                    : Instantiate(_settingsControlPrefab, _settingsControlsRoot);
                control.gameObject.SetActive(index < _slotCount);
                if (index >= _slotCount)
                {
                    continue;
                }
                Vector3 position = _settingsControlOffset;
                position.x += GetBlockPosition(index).x;
                control.transform.SetPositionAndRotation(transform.TransformPoint(position), transform.rotation);
                control.Initialize(index);
                _settingsControls.Add(control);
            }
        }

        private int FindSlot(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            int index = Mathf.RoundToInt(localPosition.x / _slotSpacing + (_slotCount - 1) * 0.5f);
            if (index < 0 || index >= _slotCount)
            {
                return -1;
            }
            Vector3 offset = localPosition - GetBlockPosition(index);
            if (Mathf.Abs(offset.x) > _placementHalfExtents.x ||
                Mathf.Abs(offset.y) > _placementHalfExtents.y ||
                Mathf.Abs(offset.z) > _placementHalfExtents.z)
            {
                return -1;
            }
            return index;
        }

        private void RefreshSlot(int index)
        {
            bool occupied = _blocks[index] != null;
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
