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

        private static readonly Color TextBright = new Color32(234, 246, 255, 255);
        private static readonly Color TextMuted = new Color32(159, 179, 200, 255);
        private static readonly Color TextInactive = new Color32(130, 151, 172, 255);
        private static readonly Color SlotText = new Color32(8, 12, 18, 255);

        internal int PlacedBlockCount { get; private set; }

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
                    LayerType.Flatten => ("FLAT", new Color32(224, 64, 200, 255)),
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
