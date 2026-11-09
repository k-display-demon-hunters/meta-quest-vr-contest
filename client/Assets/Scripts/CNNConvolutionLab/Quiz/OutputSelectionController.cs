using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CNNConvolution
{
    internal sealed class OutputSelectionController
    {
        private readonly Button[] _buttons = new Button[9];
        private readonly Image[] _targets = new Image[9];
        private bool _enabled;

        public event Action<int> Selected;

        public OutputSelectionController(ResultGridController grid)
        {
            for (int index = 0; index < _buttons.Length; index++)
            {
                int selected = index;
                grid.AttachSelectionSurface(index, rect => Bind(selected, rect));
            }
        }

        private void Bind(int index, RectTransform hitRect)
        {
            GameObject hitSurface = hitRect.gameObject;
            hitRect.sizeDelta = new Vector2(170, 150);
            Image target = hitSurface.AddComponent<Image>();
            target.color = Color.clear;
            target.raycastTarget = false;
            hitSurface.AddComponent<TrackedDeviceGraphicRaycaster>();
            Button button = hitSurface.AddComponent<Button>();
            button.targetGraphic = target;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.10f, 0.21f, 0.25f, 0.2f);
            colors.highlightedColor = new Color(0.95f, 0.75f, 0.40f, 0.65f);
            colors.pressedColor = new Color(1f, 0.85f, 0.57f, 0.8f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.clear;
            button.colors = colors;
            button.interactable = false;
            _targets[index] = target;
            _buttons[index] = button;
            button.onClick.AddListener(() =>
            {
                if (_enabled) { Selected?.Invoke(index); }
            });
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            for (int index = 0; index < _buttons.Length; index++)
            {
                _buttons[index].interactable = enabled;
                _targets[index].raycastTarget = enabled;
                _targets[index].color = enabled ? new Color(0.15f, 0.24f, 0.29f, 0.22f) : Color.clear;
            }
        }
    }
}
