using System;
using System.Collections.Generic;
using System.Text;
using ModelDesignScene.Model;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    public sealed class ModelAssemblyController : MonoBehaviour
    {
        [SerializeField] private Text[] _assemblySteps;
        [SerializeField] private Image[] _assemblyStepUnderlines;
        [SerializeField] private Text[] _assemblySlotLabels;
        [SerializeField] private Image[] _assemblySlotFills;
        [SerializeField] private Graphic[] _assemblyEmptySlots;
        [SerializeField] private Text _assemblyOutputSizes;
        [SerializeField] private GameObject _placementGuide;
        [SerializeField] private Text _placementGuideText;

        private PlacementGuideState _guideState;
        private bool _guideAvailable;

        private static readonly Color TextBright = new Color32(234, 246, 255, 255);
        private static readonly Color TextMuted = new Color32(170, 185, 192, 255);
        private static readonly Color TextInactive = new Color32(130, 146, 154, 255);
        private static readonly Color SlotText = new Color32(8, 12, 18, 255);

        internal bool IsPlacementGuideVisible => _placementGuide.activeInHierarchy;

        internal int PlacedBlockCount { get; private set; }

        internal void ShowPlacementGuide(PlacementGuideState state, int slotIndex)
        {
            _guideState = state;
            string destination = slotIndex >= 0 ? $"{slotIndex + 1}번 칸" : "빈 칸";
            _placementGuideText.text = state switch
            {
                PlacementGuideState.GrabBlock => "1. 블록 집기\n책상 위 블록을 가리키세요.\n옆면 Grip 버튼을 중지로 누르고 누른 채 유지하세요.",
                PlacementGuideState.MoveToSlot => $"2. 빈 칸으로 옮기기\nGrip을 누른 채 블록을\n강조된 {destination} 위로 옮기세요.",
                PlacementGuideState.ReleaseBlock => $"3. 손을 떼어 놓기\n{destination}에 놓을 수 있어요.\nGrip 버튼에서 손을 떼세요.",
                PlacementGuideState.RetryPlacement => "다시 놓아볼까요?\n빈 칸에 배치되지 않았어요.\n책상에서 다시 집어 옮긴 뒤,\n칸이 노랗게 되면 Grip을 놓으세요.",
                PlacementGuideState.Completed => "첫 블록을 놓았어요!\n같은 방법으로 다른 블록도\n이어서 배치하세요.",
                _ => ""
            };
            RefreshPlacementGuide();
        }

        internal void SetPlacementGuideAvailable(bool available)
        {
            _guideAvailable = available;
            RefreshPlacementGuide();
        }

        private void RefreshPlacementGuide()
        {
            _placementGuide.SetActive(_guideAvailable && _guideState != PlacementGuideState.Hidden);
        }

        internal void ShowAssembly(IReadOnlyList<LayerDefinition> slotLayers, bool isDragging)
        {
            PlacedBlockCount = 0;
            for (int index = 0; index < _assemblySlotLabels.Length; index++)
            {
                LayerDefinition layer = index < slotLayers.Count ? slotLayers[index] : null;
                bool occupied = layer != null;
                _assemblySlotFills[index].enabled = occupied;
                _assemblyEmptySlots[index].enabled = !occupied;
                Text label = _assemblySlotLabels[index];
                if (!occupied)
                {
                    label.text = (index + 1).ToString();
                    label.color = TextInactive;
                    continue;
                }

                PlacedBlockCount++;
                (string name, Color32 fill) = layer.Type switch
                {
                    LayerType.Conv2D => ("CONV", new Color32(76, 141, 255, 255)),
                    LayerType.MaxPooling2D => ("POOL", new Color32(155, 107, 255, 255)),
                    LayerType.Flatten => ("FLAT", new Color32(193, 119, 154, 255)),
                    LayerType.Dense => ("DENSE", new Color32(61, 220, 132, 255)),
                    _ => throw new ArgumentOutOfRangeException(nameof(layer))
                };
                _assemblySlotFills[index].color = fill;
                label.text = name;
                label.color = SlotText;
            }

            int current = isDragging ? 1 : PlacedBlockCount > 0 ? 2 : 0;
            for (int index = 0; index < _assemblySteps.Length; index++)
            {
                Text step = _assemblySteps[index];
                step.color = index == current ? TextBright : index < current ? TextMuted : TextInactive;
                step.fontStyle = index == current ? FontStyle.Bold : FontStyle.Normal;
                _assemblyStepUnderlines[index].enabled = index == current;
            }
        }

        internal void ShowOutputSizes(IReadOnlyList<string> sizes)
        {
            int split = sizes.Count > 6 ? (sizes.Count + 1) / 2 : sizes.Count;
            StringBuilder output = new StringBuilder();
            for (int index = 0; index < sizes.Count; index++)
            {
                if (index == split) { output.Append(" →\n"); }
                else if (index > 0) { output.Append(" → "); }
                output.Append(sizes[index]);
            }
            _assemblyOutputSizes.text = output.ToString();
        }
    }
}
