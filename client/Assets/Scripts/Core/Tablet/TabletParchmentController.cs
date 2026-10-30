using System;
using Parchment;
using UnityEngine;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletParchmentController
    {
        private Func<int, bool> _hasParchment = index => false;
        private int _selectedItemIndex;
        private readonly TabletUIBuilder.ParchmentView _view;
        private readonly TabletVisualResources _resources;
        public event Action<int, Pose> OpenRequested;
        public int SelectedIndex => _selectedItemIndex;
        public TabletParchmentController(TabletUIBuilder ui, TabletVisualResources resources)
        {
            _view = ui.Parchment;
            _resources = resources;
        }
        public void BindInventory(Func<int, bool> hasParchment)
        {
            _hasParchment = hasParchment;
            RefreshInventory();
        }
        public void RestoreSelection(int index) { _selectedItemIndex = Mathf.Clamp(index, 0, ParchmentCatalog.All.Count - 1); }
        public static string ItemName(int index) => "양피지 " + (index + 1).ToString("00");
        public bool HasParchment(int index) => _hasParchment(index);
        public void RefreshInventory()
        {
            for (int index = 0; index < _view.InventoryGlows.Count; index++)
            {
                bool owned = HasParchment(index);
                bool selected = owned && index == _selectedItemIndex;
                _view.InventoryGlows[index].gameObject.SetActive(selected);
                _view.InventorySurfaces[index].sprite = selected ? _resources.SelectedSprite : _resources.PanelSprite;
                _view.InventorySurfaces[index].color = owned ? CardColor : new Color(0.9f, 0.92f, 0.94f);
                _view.InventoryButtons[index].interactable = owned;
                _view.InventoryLabels[index].text = ItemName(index) + (owned ? "" : " · 미발견");
                _view.InventoryLabels[index].fontSize = owned ? 21 : 18;
                _view.InventoryLabels[index].color = owned ? TextColor : MutedTextColor;
                _view.InventoryDescriptions[index].text = owned ? ParchmentCatalog.Get(index).Title : ParchmentCatalog.Get(index).LocationHint;
                _view.InventoryDescriptions[index].fontSize = owned ? 18 : 16;
            }
        }


        public void SelectItem(int index)
        {
            if (!HasParchment(index)) { return; }
            _selectedItemIndex = index;
            RefreshInventory();
            Transform origin = _view.InventoryButtons[index].transform;
            OpenRequested?.Invoke(index, new Pose(origin.position, origin.rotation));
        }
    }
}
