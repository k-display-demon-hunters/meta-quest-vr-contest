using System;
using Parchment;
using UnityEngine;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletParchmentController
    {
        private readonly Func<int, bool> _hasParchment;
        private readonly Func<int, ParchmentInventoryItem> _getItem;
        private int _selectedItemIndex;
        private readonly TabletUIBuilder.ParchmentView _view;
        private readonly TabletVisualResources _resources;
        public event Action<int, Pose> OpenRequested;
        public int SelectedIndex => _selectedItemIndex;
        public TabletParchmentController(TabletUIBuilder ui, TabletVisualResources resources,
            Func<int, bool> hasParchment, Func<int, ParchmentInventoryItem> getItem)
        {
            _view = ui.Parchment;
            _resources = resources;
            _hasParchment = hasParchment;
            _getItem = getItem;
        }
        public void RestoreSelection(int index) { _selectedItemIndex = Mathf.Clamp(index, 0, ParchmentCatalog.All.Count - 1); }
        public static string ItemName(int index) => "양피지 " + (index + 1).ToString("00");
        public void RefreshInventory()
        {
            for (int index = 0; index < _view.InventoryGlows.Count; index++)
            {
                ParchmentInventoryItem item = _getItem(index);
                bool owned = item.Collected;
                bool selected = owned && index == _selectedItemIndex;
                _view.InventoryGlows[index].gameObject.SetActive(selected);
                _view.InventorySurfaces[index].sprite = selected ? _resources.SelectedSprite : _resources.PanelSprite;
                _view.InventorySurfaces[index].color = owned ? CardColor : new Color(0.9f, 0.92f, 0.94f);
                _view.InventoryButtons[index].interactable = owned;
                _view.InventoryLabels[index].text = ItemName(index) + (owned ? "" : " · 미발견");
                _view.InventoryLabels[index].fontSize = owned ? 21 : 18;
                _view.InventoryLabels[index].color = owned ? TextColor : MutedTextColor;
                _view.InventoryDescriptions[index].text = owned
                    ? item.Title
                    : item.LocationText;
                _view.InventoryDescriptions[index].fontSize = owned ? 18 : 16;
            }
        }

        public void SelectItem(int index)
        {
            if (!_hasParchment(index)) { return; }
            _selectedItemIndex = index;
            RefreshInventory();
            Transform origin = _view.InventoryButtons[index].transform;
            OpenRequested?.Invoke(index, new Pose(origin.position, origin.rotation));
        }
    }
}
