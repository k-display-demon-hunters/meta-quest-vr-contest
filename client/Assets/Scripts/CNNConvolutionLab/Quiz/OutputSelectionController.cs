using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CNNConvolution
{
    internal sealed class OutputSelectionController
    {
        private readonly ResultGridController _grid;
        private readonly Button[] _buttons = new Button[9];
        private readonly Image[] _targets = new Image[9];
        private int _highlightedIndex;
        private bool _enabled;
        private bool _paused;

        public event Action<int> Selected;

        public OutputSelectionController(ResultGridController grid)
        {
            _grid = grid;
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
                if (button.interactable) { Selected?.Invoke(index); }
            });
        }

        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled) { return; }
            _enabled = enabled;
            RefreshButtons();
            _grid.SetHighlightGlow(enabled);
            if (enabled)
            {
                // Start at the bottom-right and cycle through every unrecorded cell.
                _highlightedIndex = 0;
                AdvanceHighlight();
            }
            else { _grid.HideHighlight(); }
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (_enabled) { RefreshButtons(); }
        }

        public void AdvanceHighlight()
        {
            int index = _highlightedIndex;
            for (int remaining = _buttons.Length; remaining > 0; remaining--)
            {
                index = (index + _buttons.Length - 1) % _buttons.Length;
                if (_grid.HasRecordedValue(index)) { continue; }
                _highlightedIndex = index;
                _grid.Highlight(index);
                return;
            }
        }

        private void RefreshButtons()
        {
            for (int index = 0; index < _buttons.Length; index++)
            {
                bool selectable = _enabled && !_paused && !_grid.HasRecordedValue(index);
                _buttons[index].interactable = selectable;
                _targets[index].raycastTarget = selectable;
                _targets[index].color = selectable ? new Color(0.15f, 0.24f, 0.29f, 0.22f) : Color.clear;
            }
        }
    }
}
